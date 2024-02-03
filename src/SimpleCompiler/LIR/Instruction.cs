using ClassEnumGen;
using SimpleCompiler.MIR;

namespace SimpleCompiler.LIR;

[ClassEnum(KindEnumName = "LirInstrKind")]
public abstract partial record Instruction(LirInstrKind Kind)
{
    public static partial PushVar PushVar(VariableInfo variable);
    public static PushVar PushVar(VariableExpression variable) => PushVar(variable.VariableInfo);
    public static partial StoreVar StoreVar(VariableInfo variable);
    public static StoreVar StoreVar(VariableExpression variable) => StoreVar(variable.VariableInfo);
    public static partial PushCons PushCons(ConstantKind constantKind, object value);
    public static PushCons PushCons(ConstantExpression constant) => PushCons(constant.ConstantKind, constant.Value);
    public static partial Pop Pop();

    public static partial Loc Loc(Location location);
    public static partial Br Br(Location location);
    public static partial BrTrue BrTrue(Location location);
    public static partial BrFalse BrFalse(Location location);

    public static partial Ret Ret();
    public static partial MultiRet MultiRet(int count);

    public static partial Neg Neg();
    public static partial Add Add();
    public static partial Sub Sub();
    public static partial Mul Mul();
    public static partial Div Div();
    public static partial IntDiv IntDiv();
    public static partial Pow Pow();
    public static partial Mod Mod();
    public static partial Concat Concat();

    public static partial Not Not();

    public static partial Len Len();

    public static partial BNot BNot();
    public static partial BAnd BAnd();
    public static partial BOr BOr();
    public static partial Xor Xor();
    public static partial LShift LShift();
    public static partial RShift RShift();

    public static partial Eq Eq();
    public static partial Neq Neq();
    public static partial Lt Lt();
    public static partial Lte Lte();
    public static partial Gt Gt();
    public static partial Gte Gte();

    public static partial MkArgs MkArgs(int size);
    public static partial BeginArg BeginArg(int pos);
    public static partial StoreArg StoreArg();
    public static partial FCall FCall();

    public static partial Debug Debug();

    public string ToRepr()
    {
        return this switch
        {
            PushVar p => $"PUSHVAR {p.Variable.Name} (0x{p.Variable.GetHashCode():X})",
            StoreVar s => $"STOREVAR {s.Variable.Name} (0x{s.Variable.GetHashCode():X})",
            PushCons c => $"PUSHCONS {(c.ConstantKind == ConstantKind.Nil ? "nil" : c.Value)}",
            Pop _ => "POP",

            Loc l => $"LOC {l.Location.GetHashCode()}",
            Br b => $"BR {b.Location.GetHashCode()}",
            BrTrue b => $"BR.TRUE {b.Location.GetHashCode()}",
            BrFalse b => $"BR.FALSE {b.Location.GetHashCode()}",

            Ret _ => "RET",
            MultiRet m => $"RET {m.Count}",

            Neg _ => "NEG",
            Add _ => "ADD",
            Sub _ => "SUB",
            Mul _ => "MUL",
            Div _ => "DIV",
            IntDiv _ => "INTDIV",
            Pow _ => "POW",
            Mod _ => "MOD",
            Concat _ => "CONCAT",

            Not _ => "NOT",

            Len _ => "LEN",

            BAnd _ => "BAND",
            BOr _ => "BOR",
            Xor _ => "XOR",
            LShift _ => "LSHIFT",
            RShift _ => "RSHIFT",

            Eq _ => "EQ",
            Neq _ => "NEQ",
            Lt _ => "LT",
            Lte _ => "LTE",
            Gt _ => "GT",
            Gte _ => "GTE",

            MkArgs m => $"MKARGS {m.Size}",
            BeginArg b => $"BEGINARG {b.Pos}",
            StoreArg _ => "STOREARG",
            FCall _ => "FCALL",

            Debug _ => "DEBUG",

            _ => throw new InvalidOperationException()
        };
    }
}