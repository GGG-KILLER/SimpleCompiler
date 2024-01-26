using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Lokad.ILPack;
using Loretta.CodeAnalysis.Lua;
using Loretta.CodeAnalysis.Lua.Experimental;
using Sigil;
using Sigil.NonGeneric;
using SimpleCompiler.LIR;
using SimpleCompiler.MIR;
using SimpleCompiler.Runtime;

namespace SimpleCompiler.Compiler;

public sealed class Compiler
{
    private readonly ScopeStack _scopeStack;
    private readonly ScopeStack.Scope _rootScope;
    private readonly AssemblyBuilder _assemblyBuilder;
    private readonly ModuleBuilder _moduleBuilder;
    private readonly TextWriter? _cilDebugWriter;

    public ScopeInfo GlobalScope { get; }
    public KnownGlobalsSet KnownGlobals { get; }

    public Compiler(AssemblyBuilder assemblyBuilder, ModuleBuilder moduleBuilder, TextWriter? cilDebugWriter)
    {
        _scopeStack = new(moduleBuilder);
        _rootScope = _scopeStack.NewScope();
        GlobalScope = new ScopeInfo(MIR.ScopeKind.Global, null);
        KnownGlobals = new KnownGlobalsSet(GlobalScope);
        _assemblyBuilder = assemblyBuilder;
        _moduleBuilder = moduleBuilder;
        _cilDebugWriter = cilDebugWriter;
    }

    // TODO: Use public API when it's available: https://github.com/dotnet/runtime/issues/15704
    private static readonly Type _builderType = Type.GetType("System.Reflection.Emit.AssemblyBuilderImpl, System.Reflection.Emit", throwOnError: true)!;

    public static Compiler Create(AssemblyName assemblyName, TextWriter? cilDebugWriter = null)
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect);
        var module = assembly.DefineDynamicModule(assemblyName.Name + ".dll");
        return new Compiler(assembly, module, cilDebugWriter);
    }

    public async Task SaveAsync(Stream stream)
    {
        var gen = new AssemblyGenerator();
        var bytes = gen.GenerateAssemblyBytes(_assemblyBuilder, [typeof(string).Assembly, typeof(LuaValue).Assembly]);
        await stream.WriteAsync(bytes.AsMemory()).ConfigureAwait(false);
    }

    public MirNode LowerSyntax(LuaSyntaxTree syntaxTree)
    {
        var folded = syntaxTree.GetRoot().ConstantFold(ConstantFoldingOptions.All);
        var lowerer = new SyntaxLowerer(GlobalScope);
        return lowerer.Visit(folded)!;
    }

    [SuppressMessage("Code Quality", "CA1822", Justification = "For consistency with the rest of the API when using an instance of Compiler.")]
    public IEnumerable<Instruction> LowerMir(MirNode node)
    {
        return MirLowerer.Lower(node);
    }

    public (Type, MethodInfo) CompileProgram(IEnumerable<Instruction> instructions)
    {
        var type = _moduleBuilder.DefineType("Program", TypeAttributes.Public | TypeAttributes.AutoClass | TypeAttributes.AnsiClass | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit);
        var method = Emit.BuildStaticMethod(typeof(void), [], type, "Main", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static, strictBranchVerification: true);

        using var scope = _scopeStack.NewScope();

        var fnLocal = new Lazy<Local>(() => method.DeclareLocal<LuaValue>("fn"));

        foreach (var instruction in instructions)
        {
            switch (instruction.Kind)
            {
                case LirInstrKind.PushCons:
                    {
                        var pushCons = Unsafe.As<PushCons>(instruction);
                        PushConstant(method, pushCons.ConstantKind, pushCons.Value, true);
                    }
                    break;
                case LirInstrKind.PushVar: PushVar(method, Unsafe.As<PushVar>(instruction)); break;
                case LirInstrKind.StoreVar: StoreVar(method, Unsafe.As<StoreVar>(instruction)); break;
                case LirInstrKind.Pop: method.Pop(); break;

                case LirInstrKind.Ret: method.Return(); break;
                case LirInstrKind.MultiRet: throw new NotImplementedException();

                case LirInstrKind.Neg: PushCall(method, LuaOperations.Negate); break;
                case LirInstrKind.Add: PushCall(method, LuaOperations.Add); break;
                case LirInstrKind.Sub: PushCall(method, LuaOperations.Subtract); break;
                case LirInstrKind.Mul: PushCall(method, LuaOperations.Multiply); break;
                case LirInstrKind.Div: PushCall(method, LuaOperations.Divide); break;
                case LirInstrKind.IntDiv: PushCall(method, LuaOperations.IntegerDivide); break;
                case LirInstrKind.Pow: PushCall(method, LuaOperations.Exponentiate); break;
                case LirInstrKind.Mod: PushCall(method, LuaOperations.Modulo); break;
                case LirInstrKind.Concat: PushCall(method, LuaOperations.Concatenate); break;

                case LirInstrKind.Not: throw new NotImplementedException();

                case LirInstrKind.BNot: PushCall(method, LuaOperations.BitwiseNot); break;
                case LirInstrKind.BAnd: PushCall(method, LuaOperations.BitwiseAnd); break;
                case LirInstrKind.BOr: PushCall(method, LuaOperations.BitwiseOr); break;
                case LirInstrKind.Xor: PushCall(method, LuaOperations.BitwiseXor); break;
                case LirInstrKind.LShift: PushCall(method, LuaOperations.ShiftLeft); break;
                case LirInstrKind.RShift: PushCall(method, LuaOperations.ShiftRight); break;

                case LirInstrKind.Eq: PushCall(method, LuaOperations.Equals); break;
                case LirInstrKind.Neq: PushCall(method, LuaOperations.NotEquals); break;
                case LirInstrKind.Lt: PushCall(method, LuaOperations.LessThan); break;
                case LirInstrKind.Lte: PushCall(method, LuaOperations.LessThanOrEqual); break;
                case LirInstrKind.Gt: PushCall(method, LuaOperations.GreaterThan); break;
                case LirInstrKind.Gte: PushCall(method, LuaOperations.GreaterThanOrEqual); break;

                case LirInstrKind.Len: throw new NotImplementedException();

                case LirInstrKind.MkArgs:
                    {
                        // Convert the previous lua value to a function
                        var f = fnLocal.Value;
                        method.StoreLocal(f);
                        method.LoadLocalAddress(f);
                        method.CallVirtual(ReflectionData.LuaValue_AsFunction);

                        var mkArgs = Unsafe.As<MkArgs>(instruction);
                        method.LoadConstant(mkArgs.Size);
                        method.NewArray<LuaValue>();
                    }
                    break;
                case LirInstrKind.BeginArg:
                    {
                        var beginArg = Unsafe.As<BeginArg>(instruction);
                        method.Duplicate();
                        method.LoadConstant(beginArg.Pos);
                    }
                    break;
                case LirInstrKind.StoreArg:
                    method.StoreElement<LuaValue>();
                    break;
                case LirInstrKind.FCall:
                    method.NewObject(typeof(ReadOnlySpan<LuaValue>), [typeof(LuaValue[])]);
                    method.Call(ReflectionData.LuaFunction_Invoke);
                    break;

                case LirInstrKind.Loc:
                    {
                        var loc = Unsafe.As<Loc>(instruction);
                        _scopeStack.Current.AssignLabel(loc.Location, method.DefineLabel());
                    }
                    break;
                case LirInstrKind.Br:
                    {
                        var br = Unsafe.As<Br>(instruction);
                        var label = _scopeStack.Current.GetOrCreateLabel(method, br.Location);
                        method.Branch(label);
                    }
                    break;
                case LirInstrKind.BrFalse:
                    {
                        var br = Unsafe.As<BrFalse>(instruction);
                        var label = _scopeStack.Current.GetOrCreateLabel(method, br.Location);
                        method.BranchIfFalse(label);
                    }
                    break;
                case LirInstrKind.BrTrue:
                    {
                        var br = Unsafe.As<BrTrue>(instruction);
                        var label = _scopeStack.Current.GetOrCreateLabel(method, br.Location);
                        method.BranchIfTrue(label);
                    }
                    break;

                case LirInstrKind.Debug:
                    method.Box<LuaValue>();
                    method.Call(ReflectionData.Console_WriteLine_object);
                    break;

                default:
                    throw new UnreachableException();
            }
        }
        method.Return();

        _cilDebugWriter?.WriteLine(method.Instructions());

        method.CreateMethod(OptimizationOptions.All);
        var t = type.CreateType();

        return (t, t.GetMethod("Main")!);
    }

    private void PushVar(Emit method, PushVar pushVar)
    {
        if (pushVar.Variable == KnownGlobals.Print)
        {
            method.LoadField(ReflectionData.StockGlobal_Print);
        }
        else if (pushVar.Variable == KnownGlobals.ToString)
        {
            method.LoadField(ReflectionData.StockGlobal_ToString);
        }
        else
        {
            var local = _scopeStack.Current.GetOrCreateLocal(method, pushVar.Variable);
            method.LoadLocal(local);
        }
    }

    private void StoreVar(Emit method, StoreVar storeVar)
    {
        if (storeVar.Variable == KnownGlobals.Print)
        {
            method.StoreField(ReflectionData.StockGlobal_Print);
        }
        else if (storeVar.Variable == KnownGlobals.ToString)
        {
            method.StoreField(ReflectionData.StockGlobal_ToString);
        }
        else
        {
            var local = _scopeStack.Current.GetOrCreateLocal(method, storeVar.Variable);
            method.StoreLocal(local);
        }
    }

    private static void PushConstant(Emit method, ConstantKind kind, object value, bool wrapInLuaValue = true)
    {
        switch (kind)
        {
            case ConstantKind.Nil:
                if (wrapInLuaValue)
                    method.NewObject<LuaValue>();
                else
                    method.LoadNull();
                return;
            case ConstantKind.Boolean:
                method.LoadConstant(Unsafe.Unbox<bool>(value));
                if (wrapInLuaValue)
                    method.NewObject<LuaValue, bool>();
                break;
            case ConstantKind.Number:
                if (value is int i32)
                {
                    method.LoadConstant(i32);
                    if (wrapInLuaValue) throw new NotSupportedException();
                    return;
                }
                else if (value is long i64)
                {
                    method.LoadConstant(i64);
                    if (wrapInLuaValue)
                        method.NewObject<LuaValue, long>();
                }
                else
                {
                    method.LoadConstant(Unsafe.Unbox<double>(value));
                    if (wrapInLuaValue)
                        method.NewObject<LuaValue, double>();
                }
                break;
            case ConstantKind.String:
                method.LoadConstant(Unsafe.As<string>(value));
                if (wrapInLuaValue)
                    method.NewObject<LuaValue, string>();
                break;
        }
    }

    private static void PushCall(Emit method, Func<LuaValue, LuaValue, LuaValue> func)
    {
        method.Call(func.Method);
    }

    private static void PushCall(Emit method, Delegate func)
    {
        method.Call(func.Method);
    }

    public sealed class KnownGlobalsSet
    {
        internal KnownGlobalsSet(ScopeInfo globalScope)
        {
            Print = new VariableInfo(globalScope, MIR.VariableKind.Global, "print");
            ToString = new VariableInfo(globalScope, MIR.VariableKind.Global, "tostring");
        }

        public VariableInfo Print { get; }
        public new VariableInfo ToString { get; }
    }
}
