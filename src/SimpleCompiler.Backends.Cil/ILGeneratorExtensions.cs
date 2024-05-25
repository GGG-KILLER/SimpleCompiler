using System.Diagnostics;
using System.Reflection.Emit;

namespace SimpleCompiler.Backend.Cil.Emit;

internal static class ILGeneratorExtensions
{
    public static void LoadLocalAddress(this ILGenerator il, LocalBuilder local) =>
        il.Emit(local.LocalIndex is >= byte.MinValue and <= byte.MaxValue ? OpCodes.Ldloca_S : OpCodes.Ldloca, local);

    public static void LoadLocal(this ILGenerator il, LocalBuilder local) =>
        il.Emit(local.LocalIndex switch
        {
            0 => OpCodes.Ldloc_0,
            1 => OpCodes.Ldloc_1,
            2 => OpCodes.Ldloc_2,
            3 => OpCodes.Ldloc_3,
            >= byte.MinValue and <= byte.MaxValue => OpCodes.Ldloc_S,
            _ => OpCodes.Ldloc
        }, local);

    public static void StoreLocal(this ILGenerator il, LocalBuilder local) =>
        il.Emit(local.LocalIndex switch
        {
            0 => OpCodes.Stloc_0,
            1 => OpCodes.Stloc_1,
            2 => OpCodes.Stloc_2,
            3 => OpCodes.Stloc_3,
            >= byte.MinValue and <= byte.MaxValue => OpCodes.Stloc_S,
            _ => OpCodes.Stloc
        }, local);

    public static void LoadConstant(this ILGenerator il, bool b) => il.LoadConstant(b ? 1 : 0);
    public static void LoadConstant(this ILGenerator il, int val)
    {
        switch (val)
        {
            case -1: il.Emit(OpCodes.Ldc_I4_M1); break;
            case 0: il.Emit(OpCodes.Ldc_I4_0); break;
            case 1: il.Emit(OpCodes.Ldc_I4_1); break;
            case 2: il.Emit(OpCodes.Ldc_I4_2); break;
            case 3: il.Emit(OpCodes.Ldc_I4_3); break;
            case 4: il.Emit(OpCodes.Ldc_I4_4); break;
            case 5: il.Emit(OpCodes.Ldc_I4_5); break;
            case 6: il.Emit(OpCodes.Ldc_I4_6); break;
            case 7: il.Emit(OpCodes.Ldc_I4_7); break;
            case 8: il.Emit(OpCodes.Ldc_I4_8); break;
            case >= sbyte.MinValue and <= sbyte.MaxValue: il.Emit(OpCodes.Ldc_I4_S, unchecked((byte) val)); break;
            default: il.Emit(OpCodes.Ldc_I4, val); break;
        }
    }
    public static void LoadConstant(this ILGenerator il, uint val)
    {
        switch (val)
        {
            case uint.MaxValue: il.Emit(OpCodes.Ldc_I4_M1); break;
            case 0: il.Emit(OpCodes.Ldc_I4_0); break;
            case 1: il.Emit(OpCodes.Ldc_I4_1); break;
            case 2: il.Emit(OpCodes.Ldc_I4_2); break;
            case 3: il.Emit(OpCodes.Ldc_I4_3); break;
            case 4: il.Emit(OpCodes.Ldc_I4_4); break;
            case 5: il.Emit(OpCodes.Ldc_I4_5); break;
            case 6: il.Emit(OpCodes.Ldc_I4_6); break;
            case 7: il.Emit(OpCodes.Ldc_I4_7); break;
            case 8: il.Emit(OpCodes.Ldc_I4_8); break;
            case <= (uint) sbyte.MaxValue: il.Emit(OpCodes.Ldc_I4_S, unchecked((byte) val)); break;
            default: il.Emit(OpCodes.Ldc_I4, val); break;
        }
    }
    public static void LoadConstant(this ILGenerator il, long val) => il.Emit(OpCodes.Ldc_I8, val);
    public static void LoadConstant(this ILGenerator il, ulong val) => il.Emit(OpCodes.Ldc_I8, val);
    public static void LoadConstant(this ILGenerator il, float val) => il.Emit(OpCodes.Ldc_R4, val);
    public static void LoadConstant(this ILGenerator il, double val) => il.Emit(OpCodes.Ldc_R8, val);
    public static void LoadConstant(this ILGenerator il, string str) => il.Emit(OpCodes.Ldstr, str);
}
