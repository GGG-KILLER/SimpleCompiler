namespace SimpleCompiler.Emit;

using System.Reflection;
using System.Reflection.Emit;
using SimpleCompiler.LIR;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Sigil;
using Sigil.NonGeneric;
using SimpleCompiler.Compiler;
using SimpleCompiler.Runtime;
using SimpleCompiler.MIR;
using Lokad.ILPack;
using System.Threading.Tasks;

internal sealed class Emitter
{
    private readonly ScopeStack _scopeStack;
    private readonly ModuleBuilder _moduleBuilder;
    private readonly TextWriter? _cilDebugWriter;
    private readonly KnownGlobalsSet _knownGlobals;

    private Emitter(ModuleBuilder moduleBuilder, KnownGlobalsSet knownGlobals, TextWriter? cilDebugWriter)
    {
        _scopeStack = new(moduleBuilder);
        _scopeStack.NewScope();

        _knownGlobals = knownGlobals;
        _moduleBuilder = moduleBuilder;
        _cilDebugWriter = cilDebugWriter;
    }

    private (Type, MethodInfo) CompileProgram(IEnumerable<Instruction> instructions)
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
                    PushCall(method, LuaOperations.Call);
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
        var entryPoint = t.GetMethod("Main")!;

        return (t, entryPoint);
    }

    private void PushVar(Emit method, PushVar pushVar)
    {
        if (pushVar.Variable == _knownGlobals.Print)
        {
            method.LoadField(ReflectionData.StockGlobal_Print);
        }
        else if (pushVar.Variable == _knownGlobals.ToString)
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
        if (storeVar.Variable == _knownGlobals.Print)
        {
            method.StoreField(ReflectionData.StockGlobal_Print);
        }
        else if (storeVar.Variable == _knownGlobals.ToString)
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

    private static void PushCall(Emit method, Func<LuaValue, LuaValue, LuaValue> func) => method.Call(func.Method);
    private static void PushCall(Emit method, Delegate func) => method.Call(func.Method);

    public static async Task EmitAsync(string name, KnownGlobalsSet knownGlobals, Stream stream, IEnumerable<Instruction> instructions, TextWriter? cilDebugWriter = null)
    {
        var assemblyName = new AssemblyName(name);
        var assembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect);
        var module = assembly.DefineDynamicModule(assemblyName.Name + ".dll");

        var emitter = new Emitter(module, knownGlobals, cilDebugWriter);
        var (_, entryPoint) = emitter.CompileProgram(instructions);

        var gen = new AssemblyGenerator();
        var bytes = gen.GenerateAssemblyBytes(
            assembly,
            [typeof(string).Assembly, typeof(LuaValue).Assembly],
            entryPoint);
        await stream.WriteAsync(bytes.AsMemory())
                    .ConfigureAwait(false);
    }
}
