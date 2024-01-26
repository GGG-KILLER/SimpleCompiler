using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Lokad.ILPack;
using Loretta.CodeAnalysis.Lua;
using Loretta.CodeAnalysis.Lua.Experimental;
using SimpleCompiler.LIR;
using SimpleCompiler.MIR;
using SimpleCompiler.Runtime;
using static SimpleCompiler.Helpers.ExpressionHelper;

namespace SimpleCompiler.Compiler;

public sealed class Compiler
{
    private static readonly MethodInfo s_miLuaFunctionInvoke =
        typeof(LuaFunction).GetMethod(nameof(LuaFunction.Invoke))
        ?? throw new InvalidOperationException("Cannot get LuaFunction.Invoke(...) method.");

    private readonly ScopeStack _scopeStack;
    private readonly ScopeStack.Scope _rootScope;
    private readonly AssemblyBuilder _assemblyBuilder;
    private readonly ModuleBuilder _moduleBuilder;

    public ScopeInfo GlobalScope { get; }
    public KnownGlobalsSet KnownGlobals { get; }

    public Compiler(AssemblyBuilder assemblyBuilder, ModuleBuilder moduleBuilder)
    {
        _scopeStack = new(moduleBuilder);
        _rootScope = _scopeStack.NewScope();
        GlobalScope = new ScopeInfo(MIR.ScopeKind.Global, null);
        KnownGlobals = new KnownGlobalsSet(GlobalScope);
        _assemblyBuilder = assemblyBuilder;
        _moduleBuilder = moduleBuilder;
    }

    // TODO: Use public API when it's available: https://github.com/dotnet/runtime/issues/15704
    private static readonly Type _builderType = Type.GetType("System.Reflection.Emit.AssemblyBuilderImpl, System.Reflection.Emit", throwOnError: true)!;

    public static Compiler Create(AssemblyName assemblyName)
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect);
        var module = assembly.DefineDynamicModule(assemblyName.Name + ".dll");
        return new Compiler(assembly, module);
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
        var method = type.DefineMethod("Main", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static, typeof(void), null);

        using var scope = _scopeStack.NewScope();
        var ilGen = method.GetILGenerator();

        var argLocal = new Lazy<LocalBuilder>(() => ilGen.DeclareLocal(typeof(LuaValue)));

        foreach (var instruction in instructions)
        {
            switch (instruction.Kind)
            {
                case LirInstrKind.PushCons:
                    {
                        var pushCons = Unsafe.As<PushCons>(instruction);
                        PushConstant(ilGen, pushCons.ConstantKind, pushCons.Value, true);
                    }
                    break;
                case LirInstrKind.PushVar: PushVar(ilGen, Unsafe.As<PushVar>(instruction)); break;
                case LirInstrKind.StoreVar: StoreVar(ilGen, Unsafe.As<StoreVar>(instruction)); break;
                case LirInstrKind.Pop: Emit(ilGen, OpCodes.Pop); break;

                case LirInstrKind.Ret: Emit(ilGen, OpCodes.Ret); break;
                case LirInstrKind.MultiRet: throw new NotImplementedException();

                case LirInstrKind.Neg: PushCall(ilGen, LuaOperations.Negate); break;
                case LirInstrKind.Add: PushCall(ilGen, LuaOperations.Add); break;
                case LirInstrKind.Sub: PushCall(ilGen, LuaOperations.Subtract); break;
                case LirInstrKind.Mul: PushCall(ilGen, LuaOperations.Multiply); break;
                case LirInstrKind.Div: PushCall(ilGen, LuaOperations.Divide); break;
                case LirInstrKind.IntDiv: PushCall(ilGen, LuaOperations.IntegerDivide); break;
                case LirInstrKind.Pow: PushCall(ilGen, LuaOperations.Exponentiate); break;
                case LirInstrKind.Mod: PushCall(ilGen, LuaOperations.Modulo); break;
                case LirInstrKind.Concat: PushCall(ilGen, LuaOperations.Concatenate); break;

                case LirInstrKind.Not: throw new NotImplementedException();

                case LirInstrKind.BNot: PushCall(ilGen, LuaOperations.BitwiseNot); break;
                case LirInstrKind.BAnd: PushCall(ilGen, LuaOperations.BitwiseAnd); break;
                case LirInstrKind.BOr: PushCall(ilGen, LuaOperations.BitwiseOr); break;
                case LirInstrKind.Xor: PushCall(ilGen, LuaOperations.BitwiseXor); break;
                case LirInstrKind.LShift: PushCall(ilGen, LuaOperations.ShiftLeft); break;
                case LirInstrKind.RShift: PushCall(ilGen, LuaOperations.ShiftRight); break;

                case LirInstrKind.Eq: PushCall(ilGen, LuaOperations.Equals); break;
                case LirInstrKind.Neq: PushCall(ilGen, LuaOperations.NotEquals); break;
                case LirInstrKind.Lt: PushCall(ilGen, LuaOperations.LessThan); break;
                case LirInstrKind.Lte: PushCall(ilGen, LuaOperations.LessThanOrEqual); break;
                case LirInstrKind.Gt: PushCall(ilGen, LuaOperations.GreaterThan); break;
                case LirInstrKind.Gte: PushCall(ilGen, LuaOperations.GreaterThanOrEqual); break;

                case LirInstrKind.Len: throw new NotImplementedException();

                case LirInstrKind.MkArgs:
                    {
                        var mkArgs = Unsafe.As<MkArgs>(instruction);
                        PushConstant(ilGen, ConstantKind.Number, mkArgs.Size, false);
                        Emit(ilGen, OpCodes.Newarr, typeof(LuaValue));
                    }
                    break;
                    }
                    break;
                case LirInstrKind.StoreArg:
                    {
                        var storeArg = Unsafe.As<StoreArg>(instruction);
                        var local = argLocal.Value;

                        StoreLocal(ilGen, local);
                        ilGen.Emit(OpCodes.Dup);
                        PushConstant(ilGen, ConstantKind.Number, (long)storeArg.Pos, false);
                        PushLocal(ilGen, local);
                        ilGen.Emit(OpCodes.Stelem_Ref);
                    }
                    break;

                case LirInstrKind.Loc:
                    {
                        var loc = Unsafe.As<Loc>(instruction);
                        _scopeStack.Current.AssignLabel(loc.Location, ilGen.DefineLabel());
                    }
                    break;
                case LirInstrKind.Br:
                    {
                        var br = Unsafe.As<Br>(instruction);
                        var label = _scopeStack.Current.GetOrCreateLabel(ilGen, br.Location);
                        Emit(ilGen, OpCodes.Br, label);
                    }
                    break;
                case LirInstrKind.BrFalse:
                    {
                        var br = Unsafe.As<BrFalse>(instruction);
                        var label = _scopeStack.Current.GetOrCreateLabel(ilGen, br.Location);
                        Emit(ilGen, OpCodes.Brfalse, label);
                    }
                    break;
                case LirInstrKind.BrTrue:
                    {
                        var br = Unsafe.As<BrTrue>(instruction);
                        var label = _scopeStack.Current.GetOrCreateLabel(ilGen, br.Location);
                        Emit(ilGen, OpCodes.Brtrue, label);
                    }
                    break;

                default:
                    throw new UnreachableException();
            }
        }
        Emit(ilGen, OpCodes.Ret);

        var t = type.CreateType();
        return (t, t.GetMethod("Main")!);
    }

    private void PushVar(ILGenerator ilGen, PushVar pushVar)
    {
        if (pushVar.Variable == KnownGlobals.Print)
        {
            Emit(ilGen, OpCodes.Ldsfld, FieldInfo(() => StockGlobals.Print));
        }
        else if (pushVar.Variable == KnownGlobals.ToString)
        {
            Emit(ilGen, OpCodes.Ldsfld, FieldInfo(() => StockGlobals.ToString));
        }
        else
        {
            var variable = _scopeStack.Current.GetOrCreateLocal(ilGen, pushVar.Variable);
            PushLocal(ilGen, variable);
        }
    }

    private void StoreVar(ILGenerator ilGen, StoreVar storeVar)
    {
        if (storeVar.Variable == KnownGlobals.Print)
        {
            Emit(ilGen, OpCodes.Stsfld, FieldInfo(() => StockGlobals.Print));
        }
        else if (storeVar.Variable == KnownGlobals.ToString)
        {
            Emit(ilGen, OpCodes.Stsfld, FieldInfo(() => StockGlobals.ToString));
        }
        else
        {
            var variable = _scopeStack.Current.GetOrCreateLocal(ilGen, storeVar.Variable);
            StoreLocal(ilGen, variable);
        }
    }

    private static void PushLocal(ILGenerator ilGen, LocalBuilder local)
    {
        switch (local.LocalIndex)
        {
            case 0: Emit(ilGen, OpCodes.Ldloc_0); break;
            case 1: Emit(ilGen, OpCodes.Ldloc_1); break;
            case 2: Emit(ilGen, OpCodes.Ldloc_2); break;
            case 3: Emit(ilGen, OpCodes.Ldloc_3); break;
            case 4: Emit(ilGen, OpCodes.Ldloc_S); break;
            case < 256: Emit(ilGen, OpCodes.Ldloc_S, local); break;
            default: Emit(ilGen, OpCodes.Ldloc, local); break;
        }
    }

    private static void StoreLocal(ILGenerator ilGen, LocalBuilder local)
    {
        switch (local.LocalIndex)
        {
            case 0: Emit(ilGen, OpCodes.Stloc_0); break;
            case 1: Emit(ilGen, OpCodes.Stloc_1); break;
            case 2: Emit(ilGen, OpCodes.Stloc_2); break;
            case 3: Emit(ilGen, OpCodes.Stloc_3); break;
            case 4: Emit(ilGen, OpCodes.Stloc_S); break;
            case < 256: Emit(ilGen, OpCodes.Stloc_S, local); break;
            default: Emit(ilGen, OpCodes.Stloc, local); break;
        }
    }

    private static void PushConstant(ILGenerator ilGen, ConstantKind kind, object value, bool wrapInLuaValue = true)
    {
        switch (kind)
        {
            case ConstantKind.Nil:
                if (wrapInLuaValue)
                    Emit(ilGen, OpCodes.Ldsfld, FieldInfo(() => LuaValue.Nil));
                else
                    Emit(ilGen, OpCodes.Ldnull);
                break;
            case ConstantKind.Boolean:
                if (wrapInLuaValue)
                    Emit(ilGen, OpCodes.Ldsfld, Unsafe.Unbox<bool>(value)
                        ? FieldInfo(() => LuaValue.True)
                        : FieldInfo(() => LuaValue.False));
                else
                    Emit(ilGen, Unsafe.Unbox<bool>(value) ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                break;
            case ConstantKind.Number:
                if (value is int i32)
                {
                    switch (i32)
                    {
                        case 0: Emit(ilGen, OpCodes.Ldc_I4_0); break;
                        case 1: Emit(ilGen, OpCodes.Ldc_I4_1); break;
                        case 2: Emit(ilGen, OpCodes.Ldc_I4_2); break;
                        case 3: Emit(ilGen, OpCodes.Ldc_I4_3); break;
                        case 4: Emit(ilGen, OpCodes.Ldc_I4_4); break;
                        case 5: Emit(ilGen, OpCodes.Ldc_I4_5); break;
                        case 6: Emit(ilGen, OpCodes.Ldc_I4_6); break;
                        case 7: Emit(ilGen, OpCodes.Ldc_I4_7); break;
                        case 8: Emit(ilGen, OpCodes.Ldc_I4_8); break;
                        case -1: Emit(ilGen, OpCodes.Ldc_I4_M1); break;
                        case <= sbyte.MaxValue and >= sbyte.MinValue: Emit(ilGen, OpCodes.Ldc_I4_S, i32); break;
                        default: Emit(ilGen, OpCodes.Ldc_I4, i32); break;
                    }
                    if (wrapInLuaValue) throw new NotSupportedException();
                }
                else if (value is long i64)
                {
                    switch (i64)
                    {
                        case 0: Emit(ilGen, OpCodes.Ldc_I4_0); break;
                        case 1: Emit(ilGen, OpCodes.Ldc_I4_1); break;
                        case 2: Emit(ilGen, OpCodes.Ldc_I4_2); break;
                        case 3: Emit(ilGen, OpCodes.Ldc_I4_3); break;
                        case 4: Emit(ilGen, OpCodes.Ldc_I4_4); break;
                        case 5: Emit(ilGen, OpCodes.Ldc_I4_5); break;
                        case 6: Emit(ilGen, OpCodes.Ldc_I4_6); break;
                        case 7: Emit(ilGen, OpCodes.Ldc_I4_7); break;
                        case 8: Emit(ilGen, OpCodes.Ldc_I4_8); break;
                        case -1: Emit(ilGen, OpCodes.Ldc_I4_M1); break;
                        case <= sbyte.MaxValue and >= sbyte.MinValue: Emit(ilGen, OpCodes.Ldc_I4_S, i64); break;
                        case <= int.MaxValue and >= int.MinValue: Emit(ilGen, OpCodes.Ldc_I4, i64); break;
                        default: Emit(ilGen, OpCodes.Ldc_I8, i64); goto skipConv;
                    }
                    Emit(ilGen, OpCodes.Conv_I8);
                skipConv:;
                    if (wrapInLuaValue)
                        Emit(ilGen, OpCodes.Newobj, ConstructorInfo(() => new LuaValue(24L)));
                }
                else
                {
                    Emit(ilGen, OpCodes.Ldc_R8, Unsafe.Unbox<double>(value));
                    if (wrapInLuaValue)
                        Emit(ilGen, OpCodes.Newobj, ConstructorInfo(() => new LuaValue(2.0)));
                }
                break;
            case ConstantKind.String:
                Emit(ilGen, OpCodes.Ldstr, Unsafe.As<string>(value));
                if (wrapInLuaValue)
                    Emit(ilGen, OpCodes.Newobj, ConstructorInfo(() => new LuaValue("")));
                break;
        }
    }

    private static void PushCall(ILGenerator ilGen, Func<LuaValue, LuaValue> func)
    {
        Emit(ilGen, OpCodes.Call, func.Method);
    }

    private static void PushCall(ILGenerator ilGen, Func<LuaValue, LuaValue, LuaValue> func)
    {
        Emit(ilGen, OpCodes.Call, func.Method);
    }

    private static void Emit(ILGenerator gen, OpCode opCode)
    {
#if PRINT_CIL
        Console.WriteLine($"    {opCode.Name}");
#endif // PRINT_CIL
        gen.Emit(opCode);
    }

    private static void Emit(ILGenerator gen, OpCode opCode, dynamic value)
    {
#if PRINT_CIL
        if (value is ConstructorInfo ctor)
        {
            Console.WriteLine($"    {opCode.Name} {ctor.DeclaringType} {ctor}");
        }
        else
        {
            Console.WriteLine($"    {opCode.Name} {value}");
        }
#endif // PRINT_CIL
        gen.Emit(opCode, value);
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
