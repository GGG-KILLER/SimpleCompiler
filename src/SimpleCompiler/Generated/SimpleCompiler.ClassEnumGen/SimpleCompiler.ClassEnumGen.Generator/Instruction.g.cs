﻿// <auto-generated />

#nullable enable

namespace SimpleCompiler.LIR;
public enum LirInstrKind
{
    PushVar,
    StoreVar,
    PushCons,
    Pop,
    Loc,
    Br,
    BrTrue,
    BrFalse,
    Ret,
    MultiRet,
    Neg,
    Add,
    Sub,
    Mul,
    Div,
    IntDiv,
    Pow,
    Mod,
    Concat,
    Not,
    Len,
    BAnd,
    BOr,
    Xor,
    LShift,
    RShift,
    Eq,
    Neq,
    Lt,
    Lte,
    Gt,
    Gte,
    FCall,
}

partial record Instruction
{
    public static partial PushVar PushVar(global::SimpleCompiler.VariableInfo variable) => new(variable);
    public static partial StoreVar StoreVar(global::SimpleCompiler.VariableInfo variable) => new(variable);
    public static partial PushCons PushCons(global::SimpleCompiler.MIR.ConstantKind constantKind, object value) => new(constantKind, value);
    public static partial Pop Pop() => global::SimpleCompiler.LIR.Pop.Instance;
    public static partial Loc Loc(global::SimpleCompiler.LIR.Location location) => new(location);
    public static partial Br Br(global::SimpleCompiler.LIR.Location location) => new(location);
    public static partial BrTrue BrTrue(global::SimpleCompiler.LIR.Location location) => new(location);
    public static partial BrFalse BrFalse(global::SimpleCompiler.LIR.Location location) => new(location);
    public static partial Ret Ret() => global::SimpleCompiler.LIR.Ret.Instance;
    public static partial MultiRet MultiRet(int count) => new(count);
    public static partial Neg Neg() => global::SimpleCompiler.LIR.Neg.Instance;
    public static partial Add Add() => global::SimpleCompiler.LIR.Add.Instance;
    public static partial Sub Sub() => global::SimpleCompiler.LIR.Sub.Instance;
    public static partial Mul Mul() => global::SimpleCompiler.LIR.Mul.Instance;
    public static partial Div Div() => global::SimpleCompiler.LIR.Div.Instance;
    public static partial IntDiv IntDiv() => global::SimpleCompiler.LIR.IntDiv.Instance;
    public static partial Pow Pow() => global::SimpleCompiler.LIR.Pow.Instance;
    public static partial Mod Mod() => global::SimpleCompiler.LIR.Mod.Instance;
    public static partial Concat Concat() => global::SimpleCompiler.LIR.Concat.Instance;
    public static partial Not Not() => global::SimpleCompiler.LIR.Not.Instance;
    public static partial Len Len() => global::SimpleCompiler.LIR.Len.Instance;
    public static partial BAnd BAnd() => global::SimpleCompiler.LIR.BAnd.Instance;
    public static partial BOr BOr() => global::SimpleCompiler.LIR.BOr.Instance;
    public static partial Xor Xor() => global::SimpleCompiler.LIR.Xor.Instance;
    public static partial LShift LShift() => global::SimpleCompiler.LIR.LShift.Instance;
    public static partial RShift RShift() => global::SimpleCompiler.LIR.RShift.Instance;
    public static partial Eq Eq() => global::SimpleCompiler.LIR.Eq.Instance;
    public static partial Neq Neq() => global::SimpleCompiler.LIR.Neq.Instance;
    public static partial Lt Lt() => global::SimpleCompiler.LIR.Lt.Instance;
    public static partial Lte Lte() => global::SimpleCompiler.LIR.Lte.Instance;
    public static partial Gt Gt() => global::SimpleCompiler.LIR.Gt.Instance;
    public static partial Gte Gte() => global::SimpleCompiler.LIR.Gte.Instance;
    public static partial FCall FCall(int argCount) => new(argCount);
}

public sealed partial record PushVar(global::SimpleCompiler.VariableInfo Variable) : global::SimpleCompiler.LIR.Instruction(global::SimpleCompiler.LIR.LirInstrKind.PushVar);

public sealed partial record StoreVar(global::SimpleCompiler.VariableInfo Variable) : global::SimpleCompiler.LIR.Instruction(global::SimpleCompiler.LIR.LirInstrKind.StoreVar);

public sealed partial record PushCons(global::SimpleCompiler.MIR.ConstantKind ConstantKind, object Value) : global::SimpleCompiler.LIR.Instruction(global::SimpleCompiler.LIR.LirInstrKind.PushCons);

public sealed partial record Pop() : global::SimpleCompiler.LIR.Instruction(global::SimpleCompiler.LIR.LirInstrKind.Pop)
{
    public static readonly Pop Instance = new();
}

public sealed partial record Loc(global::SimpleCompiler.LIR.Location Location) : global::SimpleCompiler.LIR.Instruction(global::SimpleCompiler.LIR.LirInstrKind.Loc);

public sealed partial record Br(global::SimpleCompiler.LIR.Location Location) : global::SimpleCompiler.LIR.Instruction(global::SimpleCompiler.LIR.LirInstrKind.Br);

public sealed partial record BrTrue(global::SimpleCompiler.LIR.Location Location) : global::SimpleCompiler.LIR.Instruction(global::SimpleCompiler.LIR.LirInstrKind.BrTrue);

public sealed partial record BrFalse(global::SimpleCompiler.LIR.Location Location) : global::SimpleCompiler.LIR.Instruction(global::SimpleCompiler.LIR.LirInstrKind.BrFalse);

public sealed partial record Ret() : global::SimpleCompiler.LIR.Instruction(global::SimpleCompiler.LIR.LirInstrKind.Ret)
{
    public static readonly Ret Instance = new();
}

public sealed partial record MultiRet(int Count) : global::SimpleCompiler.LIR.Instruction(global::SimpleCompiler.LIR.LirInstrKind.MultiRet);

public sealed partial record Neg() : global::SimpleCompiler.LIR.Instruction(global::SimpleCompiler.LIR.LirInstrKind.Neg)
{
    public static readonly Neg Instance = new();
}

public sealed partial record Add() : global::SimpleCompiler.LIR.Instruction(global::SimpleCompiler.LIR.LirInstrKind.Add)
{
    public static readonly Add Instance = new();
}

public sealed partial record Sub() : global::SimpleCompiler.LIR.Instruction(global::SimpleCompiler.LIR.LirInstrKind.Sub)
{
    public static readonly Sub Instance = new();
}

public sealed partial record Mul() : global::SimpleCompiler.LIR.Instruction(global::SimpleCompiler.LIR.LirInstrKind.Mul)
{
    public static readonly Mul Instance = new();
}

public sealed partial record Div() : global::SimpleCompiler.LIR.Instruction(global::SimpleCompiler.LIR.LirInstrKind.Div)
{
    public static readonly Div Instance = new();
}

public sealed partial record IntDiv() : global::SimpleCompiler.LIR.Instruction(global::SimpleCompiler.LIR.LirInstrKind.IntDiv)
{
    public static readonly IntDiv Instance = new();
}

public sealed partial record Pow() : global::SimpleCompiler.LIR.Instruction(global::SimpleCompiler.LIR.LirInstrKind.Pow)
{
    public static readonly Pow Instance = new();
}

public sealed partial record Mod() : global::SimpleCompiler.LIR.Instruction(global::SimpleCompiler.LIR.LirInstrKind.Mod)
{
    public static readonly Mod Instance = new();
}

public sealed partial record Concat() : global::SimpleCompiler.LIR.Instruction(global::SimpleCompiler.LIR.LirInstrKind.Concat)
{
    public static readonly Concat Instance = new();
}

public sealed partial record Not() : global::SimpleCompiler.LIR.Instruction(global::SimpleCompiler.LIR.LirInstrKind.Not)
{
    public static readonly Not Instance = new();
}

public sealed partial record Len() : global::SimpleCompiler.LIR.Instruction(global::SimpleCompiler.LIR.LirInstrKind.Len)
{
    public static readonly Len Instance = new();
}

public sealed partial record BAnd() : global::SimpleCompiler.LIR.Instruction(global::SimpleCompiler.LIR.LirInstrKind.BAnd)
{
    public static readonly BAnd Instance = new();
}

public sealed partial record BOr() : global::SimpleCompiler.LIR.Instruction(global::SimpleCompiler.LIR.LirInstrKind.BOr)
{
    public static readonly BOr Instance = new();
}

public sealed partial record Xor() : global::SimpleCompiler.LIR.Instruction(global::SimpleCompiler.LIR.LirInstrKind.Xor)
{
    public static readonly Xor Instance = new();
}

public sealed partial record LShift() : global::SimpleCompiler.LIR.Instruction(global::SimpleCompiler.LIR.LirInstrKind.LShift)
{
    public static readonly LShift Instance = new();
}

public sealed partial record RShift() : global::SimpleCompiler.LIR.Instruction(global::SimpleCompiler.LIR.LirInstrKind.RShift)
{
    public static readonly RShift Instance = new();
}

public sealed partial record Eq() : global::SimpleCompiler.LIR.Instruction(global::SimpleCompiler.LIR.LirInstrKind.Eq)
{
    public static readonly Eq Instance = new();
}

public sealed partial record Neq() : global::SimpleCompiler.LIR.Instruction(global::SimpleCompiler.LIR.LirInstrKind.Neq)
{
    public static readonly Neq Instance = new();
}

public sealed partial record Lt() : global::SimpleCompiler.LIR.Instruction(global::SimpleCompiler.LIR.LirInstrKind.Lt)
{
    public static readonly Lt Instance = new();
}

public sealed partial record Lte() : global::SimpleCompiler.LIR.Instruction(global::SimpleCompiler.LIR.LirInstrKind.Lte)
{
    public static readonly Lte Instance = new();
}

public sealed partial record Gt() : global::SimpleCompiler.LIR.Instruction(global::SimpleCompiler.LIR.LirInstrKind.Gt)
{
    public static readonly Gt Instance = new();
}

public sealed partial record Gte() : global::SimpleCompiler.LIR.Instruction(global::SimpleCompiler.LIR.LirInstrKind.Gte)
{
    public static readonly Gte Instance = new();
}

public sealed partial record FCall(int ArgCount) : global::SimpleCompiler.LIR.Instruction(global::SimpleCompiler.LIR.LirInstrKind.FCall);
