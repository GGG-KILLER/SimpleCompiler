using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Microsoft.CSharp.RuntimeBinder;

namespace SimpleCompiler.Helpers;

public class OperationHelper(ScopeStack scopeStack)
{
    private static readonly MethodInfo s_stackMethodInfo = ExpressionHelper.MethodInfo(() => Stack<string>()).GetGenericMethodDefinition();
    public static T Stack<T>() => throw new InvalidOperationException();

    public static void EmitConstant(ILGenerator ilGen, object? value)
    {
        switch (value)
        {
            case null: ilGen.Emit(OpCodes.Ldnull); break;
            case 0: ilGen.Emit(OpCodes.Ldc_I4_0); break;
            case 1: ilGen.Emit(OpCodes.Ldc_I4_1); break;
            case 2: ilGen.Emit(OpCodes.Ldc_I4_2); break;
            case 3: ilGen.Emit(OpCodes.Ldc_I4_3); break;
            case 4: ilGen.Emit(OpCodes.Ldc_I4_4); break;
            case 5: ilGen.Emit(OpCodes.Ldc_I4_5); break;
            case 6: ilGen.Emit(OpCodes.Ldc_I4_6); break;
            case 7: ilGen.Emit(OpCodes.Ldc_I4_7); break;
            case 8: ilGen.Emit(OpCodes.Ldc_I4_8); break;
            case -1: ilGen.Emit(OpCodes.Ldc_I4_M1); break;
            case sbyte i8:
                ilGen.Emit(OpCodes.Ldc_I4, i8);
                ilGen.Emit(OpCodes.Conv_I1);
                break;
            case byte u8:
                ilGen.Emit(OpCodes.Ldc_I4, u8);
                ilGen.Emit(OpCodes.Conv_U1);
                break;
            case short i16:
                ilGen.Emit(OpCodes.Ldc_I4, i16);
                ilGen.Emit(OpCodes.Conv_I2);
                break;
            case ushort u16:
                ilGen.Emit(OpCodes.Ldc_I4, u16);
                ilGen.Emit(OpCodes.Conv_U2);
                break;
            case int i32: ilGen.Emit(OpCodes.Ldc_I4, i32); break;
            case uint u32:
                ilGen.Emit(OpCodes.Ldc_I8, u32);
                ilGen.Emit(OpCodes.Conv_U4);
                break;
            case LocalBuilder local:
                ilGen.Emit(OpCodes.Ldloca_S, local);
                break;
            default:
                {
                    var t = value.GetType();
                    if (t.IsEnum)
                    {
                        t = Enum.GetUnderlyingType(t);
                        if (t == typeof(byte))
                            EmitConstant(ilGen, (byte)value);
                        else if (t == typeof(sbyte))
                            EmitConstant(ilGen, (sbyte)value);
                        else if (t == typeof(short))
                            EmitConstant(ilGen, (short)value);
                        else if (t == typeof(ushort))
                            EmitConstant(ilGen, (ushort)value);
                        else if (t == typeof(int))
                            EmitConstant(ilGen, (int)value);
                        else if (t == typeof(uint))
                            EmitConstant(ilGen, (uint)value);
                        else if (t == typeof(long))
                            EmitConstant(ilGen, (long)value);
                        else if (t == typeof(ulong))
                            EmitConstant(ilGen, (ulong)value);
                        else
                            throw new NotSupportedException();
                        break;
                    }
                }
                throw new NotSupportedException();
        }
    }

    public static void EmitStaticCall<T>(ILGenerator ilGen, Expression<Func<T>> call)
    {
        if (call.Body is not MethodCallExpression methodCall)
            throw new ArgumentException("");

        var pushedToStack = false;
        for (var idx = methodCall.Arguments.Count - 1; idx >= 0; idx--)
        {
            var arg = methodCall.Arguments[idx];
            if (arg is MethodCallExpression methodCallExpression
                && methodCallExpression.Method.IsGenericMethod
                && methodCallExpression.Method.GetGenericMethodDefinition() == s_stackMethodInfo)
            {
                if (pushedToStack)
                    throw new InvalidOperationException("Value was pushed to stack so an unexpected value will be there.");
                continue;
            }
            if (arg is not ConstantExpression constantExpression)
                throw new ArgumentException("");

            pushedToStack = true;
            EmitConstant(ilGen, constantExpression.Value);
        }
        ilGen.Emit(OpCodes.Call, methodCall.Method);
    }

    public void CreateBinOp(TypeBuilder currentType, ILGenerator ilGen, ExpressionType expressionType, LocalBuilder left, LocalBuilder right)
    {
        var callsiteField = scopeStack.Current.CreateCallsiteCache(currentType, [typeof(object), typeof(object)], typeof(object));
        var callsiteT = callsiteField.FieldType;

        var ifend = ilGen.DefineLabel();
        // if (field != null)
        ilGen.Emit(OpCodes.Ldsfld, callsiteField);
        ilGen.Emit(OpCodes.Brtrue_S, ifend);
        // {
        {
            // stack = [CSharpBinderFlags.None]
            EmitConstant(ilGen, CSharpBinderFlags.None);
            // stack = [expressionType, CSharpBinderFlags.None]
            EmitConstant(ilGen, expressionType);
            // stack = [typeof(Program), expressionType, CSharpBinderFlags.None]
            ilGen.Emit(OpCodes.Ldtoken, currentType);
            EmitStaticCall(ilGen, () => Type.GetTypeFromHandle(Stack<RuntimeTypeHandle>()));
            // stack = [2, typeof(Program), expressionType, CSharpBinderFlags.None]
            ilGen.Emit(OpCodes.Ldc_I4_2);
            // stack = [CSharpArgumentInfo[null, null], typeof(Program), expressionType, CSharpBinderFlags.None]
            ilGen.Emit(OpCodes.Newarr, typeof(CSharpArgumentInfo));
            // stack = [*, CSharpArgumentInfo[null, null], typeof(Program), expressionType, CSharpBinderFlags.None]
            ilGen.Emit(OpCodes.Dup);
            // stack = [0, *, CSharpArgumentInfo[null, null], typeof(Program), expressionType, CSharpBinderFlags.None]
            ilGen.Emit(OpCodes.Ldc_I4_0);
            // stack = [new CSharpArgumentInfo(0, null), 0, *, CSharpArgumentInfo[null, null], typeof(Program), expressionType, CSharpBinderFlags.None]
            EmitStaticCall(ilGen, () => CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null));
            // stack = [CSharpArgumentInfo[new CSharpArgumentInfo(0, null), null], typeof(Program), expressionType, CSharpBinderFlags.None]
            ilGen.Emit(OpCodes.Stind_Ref);

            // stack = [CSharpArgumentInfo[new CSharpArgumentInfo(0, null), null], CSharpArgumentInfo[new CSharpArgumentInfo(0, null), null], typeof(Program), expressionType, CSharpBinderFlags.None]
            ilGen.Emit(OpCodes.Dup);

            // stack = [1, CSharpArgumentInfo[new CSharpArgumentInfo(0, null), null], CSharpArgumentInfo[new CSharpArgumentInfo(0, null), null], typeof(Program), expressionType, CSharpBinderFlags.None]
            ilGen.Emit(OpCodes.Ldc_I4_1);
            // stack = [new CSharpArgumentInfo(0, null), 1, CSharpArgumentInfo[new CSharpArgumentInfo(0, null), null], CSharpArgumentInfo[new CSharpArgumentInfo(0, null), null], typeof(Program), expressionType, CSharpBinderFlags.None]
            EmitStaticCall(ilGen, () => CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null));
            // stack = [CSharpArgumentInfo[new CSharpArgumentInfo(0, null), new CSharpArgumentInfo(0, null)], typeof(Program), expressionType, CSharpBinderFlags.None]
            ilGen.Emit(OpCodes.Stelem_Ref);

            // stack = [CallSiteBinder]
            EmitStaticCall(ilGen, () => Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(
                Stack<CSharpBinderFlags>(),
                Stack<ExpressionType>(),
                Stack<Type>(),
                Stack<IEnumerable<CSharpArgumentInfo>>()));

            // stack = [CallSite<...>]
            ilGen.Emit(OpCodes.Call, callsiteT.GetMethod(nameof(CallSite<object>.Create), [typeof(CallSiteBinder)])!);

            // stack = []
            // <>o__N.<>p__C = CallSite<...>
            ilGen.Emit(OpCodes.Stfld, callsiteField);
        }
        ilGen.MarkLabel(ifend);
        // }

        // stack = [<>o__N.<>p__C]
        ilGen.Emit(OpCodes.Ldsfld, callsiteField);
        // stack = [<>o__N.<>p__C.Target]
        ilGen.Emit(OpCodes.Ldfld, callsiteT.GetField("Target", BindingFlags.Public | BindingFlags.Instance)!);
        // stack = [<>o__N.<>p__C, <>o__N.<>p__C.Target]
        ilGen.Emit(OpCodes.Ldsfld, callsiteField);
        // stack = [left, <>o__N.<>p__C, <>o__N.<>p__C.Target]
        ilGen.Emit(OpCodes.Ldloc, left);
        // stack = [right, left, <>o__N.<>p__C, <>o__N.<>p__C.Target]
        ilGen.Emit(OpCodes.Ldloc, right);
        // stack = [<>o__N.<>p__C.Target(<>o__N.<>p__C, left, right)]
        ilGen.Emit(OpCodes.Callvirt, callsiteT.GetGenericArguments()[0].GetMethod("Invoke", BindingFlags.Public | BindingFlags.Instance)!);
    }

    public void CreateAddOp(TypeBuilder currentType, ILGenerator ilGen, LocalBuilder left, LocalBuilder right) =>
        CreateBinOp(currentType, ilGen, ExpressionType.Add, left, right);

    public void CreateSubOp(TypeBuilder currentType, ILGenerator ilGen, LocalBuilder left, LocalBuilder right) =>
        CreateBinOp(currentType, ilGen, ExpressionType.Subtract, left, right);

    public void CreateMulOp(TypeBuilder currentType, ILGenerator ilGen, LocalBuilder left, LocalBuilder right) =>
        CreateBinOp(currentType, ilGen, ExpressionType.Multiply, left, right);

    public void CreateDivOp(TypeBuilder currentType, ILGenerator ilGen, LocalBuilder left, LocalBuilder right) =>
        CreateBinOp(currentType, ilGen, ExpressionType.Divide, left, right);

    public void CreateModOp(TypeBuilder currentType, ILGenerator ilGen, LocalBuilder left, LocalBuilder right) =>
        CreateBinOp(currentType, ilGen, ExpressionType.Modulo, left, right);
}
