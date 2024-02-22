using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using Sigil;
using SimpleCompiler.Helpers;
using SimpleCompiler.IR;
using SimpleCompiler.Runtime;

namespace SimpleCompiler.Backends.Cil;

internal sealed class MethodCompiler(IrGraph ir, SymbolTable symbolTable, Emit<Func<LuaValue, LuaValue>> method)
{
    private readonly SymbolTable _symbolTable = symbolTable;
    private readonly SlotPool _slots = new(method);
    public Emit<Func<LuaValue, LuaValue>> Method => method;

    public static MethodCompiler Create(TypeBuilder typeBuilder, IrGraph ir, SymbolTable symbolTable, string name)
    {
        return new MethodCompiler(
            ir,
            symbolTable,
            Emit<Func<LuaValue, LuaValue>>.BuildStaticMethod(
                typeBuilder,
                name,
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                allowUnverifiableCode: true)
        );
    }

    public void AddNilReturn()
    {
        method.NewObject<LuaValue>();
        method.Return();
    }

    public void Compile()
    {
        foreach (var block in ir.BasicBlocks)
            method.DefineLabel($"BB{block.Ordinal}");

        foreach (var block in ir.BasicBlocks)
        {
            method.MarkLabel($"BB{block.Ordinal}");
            foreach (var instruction in block.Instructions)
                EmitInstruction(instruction);
        }

        // Return nil by default.
        method.NewObject<LuaValue>();
        method.Return();
    }

    private void EmitInstruction(Instruction instruction)
    {
        switch (instruction.Kind)
        {
            case InstructionKind.DebugLocation:
                // TODO: Find out how to add debug data
                break;
            case InstructionKind.Assignment:
            {
                var assignment = CastHelper.FastCast<Assignment>(instruction);
                var assigneeData = _symbolTable[assignment.Name];
                var local = GetLocal(assignment.Name);

                if (assignment.Value is Constant { Kind: ConstantKind.Nil })
                {
                    // Fast method of initializing a nil local.
                    method.LoadLocalAddress(local);
                    method.InitializeObject<LuaValue>();
                }
                else
                {
                    EmitAssignment(assignment.Name, assigneeData.LocalType, () =>
                    {
                        var valueType = InformationCollector.GetOperandType(assignment.Value, _symbolTable).ToLocalType();
                        EmitOperand(assignment.Value);
                        ConvertTo(valueType, assigneeData.LocalType, "");
                    });
                }
                break;
            }
            case InstructionKind.UnaryAssignment:
            {
                var assignment = CastHelper.FastCast<UnaryAssignment>(instruction);
                EmitUnaryAssignment(assignment);
                break;
            }
            case InstructionKind.BinaryAssignment:
            {
                var assignment = CastHelper.FastCast<BinaryAssignment>(instruction);
                EmitBinaryAssignment(assignment);
                break;
            }
            case InstructionKind.FunctionAssignment:
            {
                var assignment = CastHelper.FastCast<FunctionAssignment>(instruction);
                EmitFunctionAssignment(assignment);
                break;
            }
            case InstructionKind.PhiAssignment:
                throw new InvalidOperationException("There should be no phis when MethodCompiler is invoked.");
            case InstructionKind.Branch:
            {
                var branch = CastHelper.FastCast<Branch>(instruction);
                method.Branch($"BB{branch.Target.BlockOrdinal}");
                break;
            }
            case InstructionKind.ConditionalBranch:
            {
                var branch = CastHelper.FastCast<ConditionalBranch>(instruction);
                EmitOperand(branch.Condition);
                method.BranchIfTrue($"BB{branch.TargetIfTrue.BlockOrdinal}");
                method.Branch($"BB{branch.TargetIfFalse.BlockOrdinal}");
                break;
            }
        }
    }

    private void EmitUnaryAssignment(UnaryAssignment assignment)
    {
        var assigneeData = _symbolTable[assignment.Name];
        var operandSymbolType = InformationCollector.GetOperandType(assignment.Operand, _symbolTable);
        var operandType = operandSymbolType.ToLocalType();
        var valueType = OperationFacts.GetOperationOutput(assignment.OperationKind, operandSymbolType).ToLocalType();

        EmitAssignment(assignment.Name, valueType, () =>
        {
            switch (assignment.OperationKind)
            {
                case UnaryOperationKind.LogicalNegation:
                    EmitOperand(assignment.Operand, true);
                    switch (operandType)
                    {
                        case LocalType.Bool:
                            method.Not();
                            break;
                        case LocalType.Long:
                            method.Pop();
                            EmitOperand(Constant.False);
                            break;
                        case LocalType.Double:
                            method.Pop();
                            EmitOperand(Constant.False);
                            break;
                        case LocalType.String:
                            method.Pop();
                            EmitOperand(Constant.False);
                            break;
                        case LocalType.LuaFunction:
                            method.Pop();
                            EmitOperand(Constant.False);
                            break;
                        case LocalType.LuaValue:
                            method.Call(ReflectionData.LuaValue_IsTruthy);
                            method.Negate();
                            break;
                    }
                    break;
                case UnaryOperationKind.BitwiseNegation:
                    EmitOperand(assignment.Operand, true);
                    if (!ConvertTo(operandType, LocalType.Long, "Value does not have an integer representation.", true))
                    {
                        method.Pop();
                        method.LoadConstant((int) operandType.ToValueKind());
                        method.Call(ReflectionData.LuaOperations_ThrowBitwiseError);
                    }
                    else
                    {
                        method.Not();
                    }
                    break;
                case UnaryOperationKind.NumericalNegation:
                    EmitOperand(assignment.Operand, true);
                    switch (operandType)
                    {
                        case LocalType.Bool:
                        case LocalType.String:
                        case LocalType.LuaFunction:
                            method.Pop();
                            method.LoadConstant((int) operandType.ToValueKind());
                            method.Call(ReflectionData.LuaOperations_ThrowArithmeticError);
                            break;
                        case LocalType.Long:
                            method.Negate();
                            break;
                        case LocalType.Double:
                            method.Negate();
                            break;
                        case LocalType.LuaValue:
                            var ifTrue = method.DefineLabel();
                            var end = method.DefineLabel();

                            // stack = [*, LuaValue]
                            method.Duplicate();
                            // stack = [*.Kind, LuaValue]
                            method.LoadField(ReflectionData.LuaValue_Kind);
                            // stack = [ValueKind.Long, *.Kind, LuaValue]
                            method.LoadConstant((int) ValueKind.Long);
                            // stack = [LuaValue]
                            // if (x.Kind != ValueKind.Long)
                            method.BranchIfEqual(ifTrue);
                            //   stack = [LuaValue.AsLong()]
                            method.Call(ReflectionData.LuaValue_AsLong);
                            method.Branch(end);
                            // else
                            method.MarkLabel(ifTrue);
                            //   stack = [x.AsDouble()]
                            method.Call(ReflectionData.LuaValue_AsDouble);
                            method.MarkLabel(end);
                            // stack = [-top]
                            method.Negate();
                            break;
                    }
                    break;
                case UnaryOperationKind.LengthOf:
                    EmitOperand(assignment.Operand, true);
                    if (!ConvertTo(operandType, LocalType.String, "", true))
                    {
                        method.Pop();
                        method.LoadConstant((int) operandType.ToValueKind());
                        method.Call(ReflectionData.LuaOperations_ThrowLengthError);
                    }
                    else
                    {
                        method.CallVirtual(ReflectionData.string_Length);
                    }
                    break;
            }
        });
    }

    private void EmitBinaryAssignment(BinaryAssignment assignment)
    {
        var assigneeData = _symbolTable[assignment.Name];
        var leftSymbolType = InformationCollector.GetOperandType(assignment.Left, _symbolTable);
        var leftType = leftSymbolType.ToLocalType();
        var rightSymbolType = InformationCollector.GetOperandType(assignment.Right, _symbolTable);
        var rightType = rightSymbolType.ToLocalType();
        var localType = OperationFacts.GetOperationOutput(assignment.OperationKind, leftSymbolType, rightSymbolType).ToLocalType();

        EmitAssignment(assignment.Name, localType, () =>
        {
            switch (assignment.OperationKind)
            {
                case BinaryOperationKind.Addition:
                {
                    var conversionSuccessful = true;
                    EmitOperand(assignment.Left, true);
                    conversionSuccessful &= ConvertTo(leftType, localType, "", true);
                    EmitOperand(assignment.Right, true);
                    conversionSuccessful &= ConvertTo(rightType, localType, "", true);
                    if (!conversionSuccessful)
                    {
                        method.Pop();
                        method.Pop();
                        method.LoadConstant(leftType != LocalType.LuaValue ? (int) leftType.ToValueKind() : (int) rightType.ToValueKind());
                        method.Call(ReflectionData.LuaOperations_ThrowArithmeticError);
                        if (localType == LocalType.Double)
                            method.Convert<double>();
                    }
                    else
                    {
                        method.Add();
                    }
                    break;
                }
                case BinaryOperationKind.Subtraction:
                {
                    var conversionSuccessful = true;
                    EmitOperand(assignment.Left, true);
                    conversionSuccessful &= ConvertTo(leftType, localType, "", true);
                    EmitOperand(assignment.Right, true);
                    conversionSuccessful &= ConvertTo(rightType, localType, "", true);
                    if (!conversionSuccessful)
                    {
                        method.Pop();
                        method.Pop();
                        method.LoadConstant(leftType != LocalType.LuaValue ? (int) leftType.ToValueKind() : (int) rightType.ToValueKind());
                        method.Call(ReflectionData.LuaOperations_ThrowArithmeticError);
                        if (localType == LocalType.Double)
                            method.Convert<double>();
                    }
                    else
                    {
                        method.Subtract();
                    }
                    break;
                }
                case BinaryOperationKind.Multiplication:
                {
                    var conversionSuccessful = true;
                    EmitOperand(assignment.Left, true);
                    conversionSuccessful &= ConvertTo(leftType, localType, "", true);
                    EmitOperand(assignment.Right, true);
                    conversionSuccessful &= ConvertTo(rightType, localType, "", true);
                    if (!conversionSuccessful)
                    {
                        method.Pop();
                        method.Pop();
                        method.LoadConstant(leftType != LocalType.LuaValue ? (int) leftType.ToValueKind() : (int) rightType.ToValueKind());
                        method.Call(ReflectionData.LuaOperations_ThrowArithmeticError);
                        if (localType == LocalType.Double)
                            method.Convert<double>();
                    }
                    else
                    {
                        method.Multiply();
                    }
                    break;
                }
                case BinaryOperationKind.Division:
                {
                    var conversionSuccessful = true;
                    EmitOperand(assignment.Left, true);
                    conversionSuccessful &= ConvertTo(leftType, LocalType.Double, "", true);
                    EmitOperand(assignment.Right, true);
                    conversionSuccessful &= ConvertTo(rightType, LocalType.Double, "", true);
                    if (!conversionSuccessful)
                    {
                        method.Pop();
                        method.Pop();
                        method.LoadConstant(leftType != LocalType.LuaValue ? (int) leftType.ToValueKind() : (int) rightType.ToValueKind());
                        method.Call(ReflectionData.LuaOperations_ThrowArithmeticError);
                        method.Convert<double>();
                    }
                    else
                    {
                        method.Divide();
                    }
                    break;
                }
                case BinaryOperationKind.IntegerDivision:
                {
                    var conversionSuccessful = true;
                    EmitOperand(assignment.Left, true);
                    conversionSuccessful &= ConvertTo(leftType, LocalType.Double, "", true);
                    EmitOperand(assignment.Right, true);
                    conversionSuccessful &= ConvertTo(rightType, LocalType.Double, "", true);
                    if (!conversionSuccessful)
                    {
                        method.Pop();
                        method.Pop();
                        method.LoadConstant(leftType != LocalType.LuaValue ? (int) leftType.ToValueKind() : (int) rightType.ToValueKind());
                        method.Call(ReflectionData.LuaOperations_ThrowArithmeticError);
                    }
                    else
                    {
                        method.Divide();
                        method.Convert<long>();
                    }
                    break;
                }
                case BinaryOperationKind.Exponentiation:
                {
                    var conversionSuccessful = true;
                    EmitOperand(assignment.Left, true);
                    conversionSuccessful &= ConvertTo(leftType, LocalType.Double, "", true);
                    EmitOperand(assignment.Right, true);
                    conversionSuccessful &= ConvertTo(rightType, LocalType.Double, "", true);
                    if (!conversionSuccessful)
                    {
                        method.Pop();
                        method.Pop();
                        method.LoadConstant(leftType != LocalType.LuaValue ? (int) leftType.ToValueKind() : (int) rightType.ToValueKind());
                        method.Call(ReflectionData.LuaOperations_ThrowArithmeticError);
                        method.Convert<double>();
                    }
                    else
                    {
                        method.Call(ReflectionData.Math_Pow);
                    }
                    break;
                }
                case BinaryOperationKind.Modulo:
                {
                    var conversionSuccessful = true;
                    EmitOperand(assignment.Left, true);
                    conversionSuccessful &= ConvertTo(leftType, localType, "", true);
                    EmitOperand(assignment.Right, true);
                    conversionSuccessful &= ConvertTo(rightType, localType, "", true);
                    if (!conversionSuccessful)
                    {
                        method.Pop();
                        method.Pop();
                        method.LoadConstant(leftType != LocalType.LuaValue ? (int) leftType.ToValueKind() : (int) rightType.ToValueKind());
                        method.Call(ReflectionData.LuaOperations_ThrowArithmeticError);
                        if (localType == LocalType.Double)
                            method.Convert<double>();
                    }
                    else
                    {
                        method.Remainder();
                    }
                    break;
                }
                case BinaryOperationKind.Concatenation:
                {
                    var conversionSuccessful = true;
                    EmitOperand(assignment.Left, true);
                    conversionSuccessful &= ConvertTo(leftType, localType, "", true);
                    EmitOperand(assignment.Right, true);
                    conversionSuccessful &= ConvertTo(rightType, localType, "", true);
                    if (!conversionSuccessful)
                    {
                        method.Pop();
                        method.Pop();
                        method.LoadConstant(leftType != LocalType.LuaValue ? (int) leftType.ToValueKind() : (int) rightType.ToValueKind());
                        method.Call(ReflectionData.LuaOperations_ThrowConcatError);
                    }
                    else
                    {
                        method.Call(ReflectionData.string_Concat2);
                    }
                    break;
                }
                case BinaryOperationKind.BitwiseAnd:
                {
                    var conversionSuccessful = true;
                    EmitOperand(assignment.Left, true);
                    conversionSuccessful &= ConvertTo(leftType, LocalType.Long, "", true);
                    EmitOperand(assignment.Right, true);
                    conversionSuccessful &= ConvertTo(rightType, LocalType.Long, "", true);
                    if (!conversionSuccessful)
                    {
                        method.Pop();
                        method.Pop();
                        method.LoadConstant(leftType != LocalType.LuaValue ? (int) leftType.ToValueKind() : (int) rightType.ToValueKind());
                        method.Call(ReflectionData.LuaOperations_ThrowBitwiseError);
                    }
                    else
                    {
                        method.And();
                    }
                    break;
                }
                case BinaryOperationKind.BitwiseOr:
                {
                    var conversionSuccessful = true;
                    EmitOperand(assignment.Left, true);
                    conversionSuccessful &= ConvertTo(leftType, LocalType.Long, "", true);
                    EmitOperand(assignment.Right, true);
                    conversionSuccessful &= ConvertTo(rightType, LocalType.Long, "", true);
                    if (!conversionSuccessful)
                    {
                        method.Pop();
                        method.Pop();
                        method.LoadConstant(leftType != LocalType.LuaValue ? (int) leftType.ToValueKind() : (int) rightType.ToValueKind());
                        method.Call(ReflectionData.LuaOperations_ThrowBitwiseError);
                    }
                    else
                    {
                        method.Or();
                    }
                    break;
                }
                case BinaryOperationKind.BitwiseXor:
                {
                    var conversionSuccessful = true;
                    EmitOperand(assignment.Left, true);
                    conversionSuccessful &= ConvertTo(leftType, LocalType.Long, "", true);
                    EmitOperand(assignment.Right, true);
                    conversionSuccessful &= ConvertTo(rightType, LocalType.Long, "", true);
                    if (!conversionSuccessful)
                    {
                        method.Pop();
                        method.Pop();
                        method.LoadConstant(leftType != LocalType.LuaValue ? (int) leftType.ToValueKind() : (int) rightType.ToValueKind());
                        method.Call(ReflectionData.LuaOperations_ThrowBitwiseError);
                    }
                    else
                    {
                        method.Xor();
                    }
                    break;
                }
                case BinaryOperationKind.LeftShift:
                {
                    var conversionSuccessful = true;
                    EmitOperand(assignment.Left, true);
                    conversionSuccessful &= ConvertTo(leftType, LocalType.Long, "", true);
                    EmitOperand(assignment.Right, true);
                    conversionSuccessful &= ConvertTo(rightType, LocalType.Long, "", true);
                    if (!conversionSuccessful)
                    {
                        method.Pop();
                        method.Pop();
                        method.LoadConstant(leftType != LocalType.LuaValue ? (int) leftType.ToValueKind() : (int) rightType.ToValueKind());
                        method.Call(ReflectionData.LuaOperations_ThrowBitwiseError);
                    }
                    else
                    {
                        method.ShiftLeft();
                    }
                    break;
                }
                case BinaryOperationKind.RightShift:
                {
                    var conversionSuccessful = true;
                    EmitOperand(assignment.Left, true);
                    conversionSuccessful &= ConvertTo(leftType, LocalType.Long, "", true);
                    EmitOperand(assignment.Right, true);
                    conversionSuccessful &= ConvertTo(rightType, LocalType.Long, "", true);
                    if (!conversionSuccessful)
                    {
                        method.Pop();
                        method.Pop();
                        method.LoadConstant(leftType != LocalType.LuaValue ? (int) leftType.ToValueKind() : (int) rightType.ToValueKind());
                        method.Call(ReflectionData.LuaOperations_ThrowBitwiseError);
                    }
                    else
                    {
                        method.ShiftRight();
                    }
                    break;
                }
                case BinaryOperationKind.Equals:
                    switch (leftType, rightType)
                    {
                        case (LocalType.Bool, LocalType.Bool):
                        case (LocalType.Long, LocalType.Long):
                        case (LocalType.Double, LocalType.Double):
                            EmitOperand(assignment.Left);
                            EmitOperand(assignment.Right);
                            method.CompareEqual();
                            break;
                        case (LocalType.String, LocalType.String):
                            EmitOperand(assignment.Left);
                            EmitOperand(assignment.Right);
                            method.Call(ReflectionData.string_Equality);
                            break;
                        case (LocalType.LuaValue, LocalType.LuaValue):
                            EmitOperand(assignment.Left);
                            EmitOperand(assignment.Right);
                            method.Call(ReflectionData.LuaValue_Equality);
                            break;
                        default:
                        {

                            EmitOperand(assignment.Left);
                            var converted = ConvertTo(leftType, LocalType.LuaValue, "");
                            EmitOperand(assignment.Right);
                            converted &= ConvertTo(rightType, LocalType.LuaValue, "");
                            if (!converted)
                            {
                                method.Pop();
                                method.Pop();
                                method.LoadConstant(false);
                            }
                            method.Call(ReflectionData.LuaValue_Equality);
                            break;
                        }
                    }
                    break;
                case BinaryOperationKind.NotEquals:
                    switch (leftType, rightType)
                    {
                        case (LocalType.Bool, LocalType.Bool):
                        case (LocalType.Long, LocalType.Long):
                        case (LocalType.Double, LocalType.Double):
                            EmitOperand(assignment.Left);
                            EmitOperand(assignment.Right);
                            method.CompareEqual();
                            method.Not();
                            break;
                        case (LocalType.String, LocalType.String):
                            EmitOperand(assignment.Left);
                            EmitOperand(assignment.Right);
                            method.Call(ReflectionData.string_Inequality);
                            break;
                        case (LocalType.LuaValue, LocalType.LuaValue):
                            EmitOperand(assignment.Left);
                            EmitOperand(assignment.Right);
                            method.Call(ReflectionData.LuaValue_Inequality);
                            break;
                        default:
                        {

                            EmitOperand(assignment.Left);
                            var converted = ConvertTo(leftType, LocalType.LuaValue, "");
                            EmitOperand(assignment.Right);
                            converted &= ConvertTo(rightType, LocalType.LuaValue, "");
                            if (!converted)
                            {
                                method.Pop();
                                method.Pop();
                                method.LoadConstant(false);
                            }
                            method.Call(ReflectionData.LuaValue_Inequality);
                            break;
                        }
                    }
                    break;
                case BinaryOperationKind.LessThan:
                {
                    EmitOperand(assignment.Left);
                    var converted = ConvertTo(leftType, LocalType.LuaValue, "");
                    EmitOperand(assignment.Right);
                    converted &= ConvertTo(rightType, LocalType.LuaValue, "");
                    if (!converted)
                        throw new UnreachableException("Couldn't convert inputs.");
                    method.Call(ReflectionData.LuaOperations_LessThan);
                    break;
                }
                case BinaryOperationKind.LessThanOrEquals:
                {
                    EmitOperand(assignment.Left);
                    var converted = ConvertTo(leftType, LocalType.LuaValue, "");
                    EmitOperand(assignment.Right);
                    converted &= ConvertTo(rightType, LocalType.LuaValue, "");
                    if (!converted)
                        throw new UnreachableException("Couldn't convert inputs.");
                    method.Call(ReflectionData.LuaOperations_LessThanOrEqual);
                    break;
                }
                case BinaryOperationKind.GreaterThan:
                {
                    EmitOperand(assignment.Left);
                    var converted = ConvertTo(leftType, LocalType.LuaValue, "");
                    EmitOperand(assignment.Right);
                    converted &= ConvertTo(rightType, LocalType.LuaValue, "");
                    if (!converted)
                        throw new UnreachableException("Couldn't convert inputs.");
                    method.Call(ReflectionData.LuaOperations_GreaterThan);
                    break;
                }
                case BinaryOperationKind.GreaterThanOrEquals:
                {
                    EmitOperand(assignment.Left);
                    var converted = ConvertTo(leftType, LocalType.LuaValue, "");
                    EmitOperand(assignment.Right);
                    converted &= ConvertTo(rightType, LocalType.LuaValue, "");
                    if (!converted)
                        throw new UnreachableException("Couldn't convert inputs.");
                    method.Call(ReflectionData.LuaOperations_GreaterThanOrEqual);
                    break;
                }
            }
        });
    }

    private void EmitFunctionAssignment(FunctionAssignment assignment)
    {
        EmitAssignment(assignment.Name, LocalType.LuaValue, () =>
        {
            EmitOperand(assignment.Callee);

            method.LoadConstant(assignment.Arguments.Count);
            method.NewArray<LuaValue>();
            for (var i = 0; i < assignment.Arguments.Count; i++)
            {
                var argument = assignment.Arguments[i];
                var argumentType = InformationCollector.GetOperandType(argument, _symbolTable).ToLocalType();

                method.Duplicate();
                method.LoadConstant(i);
                // TODO: Uncomment once kevin-montrose/Sigil#67 gets fixed.
                // if (argumentType != LocalType.LuaValue)
                //     method.LoadElementAddress<LuaValue>();
                EmitOperand(argument, false);
                // if (argumentType != LocalType.LuaValue)
                // {
                //     switch (argumentType)
                //     {
                //         case LocalType.Bool:
                //             method.Call(ReflectionData.LuaValue_BoolCtor);
                //             break;
                //         case LocalType.Long:
                //             method.Call(ReflectionData.LuaValue_LongCtor);
                //             break;
                //         case LocalType.Double:
                //             method.Call(ReflectionData.LuaValue_DoubleCtor);
                //             break;
                //         case LocalType.String:
                //             method.Call(ReflectionData.LuaValue_StringCtor);
                //             break;
                //         case LocalType.LuaFunction:
                //             method.Call(ReflectionData.LuaValue_FunctionCtor);
                //             break;
                //         case LocalType.None:
                //             throw new UnreachableException("None shouldn't exist here.");
                //         case LocalType.LuaValue:
                //             throw new UnreachableException("Shouldn't have entered this if.");
                //         default:
                //             throw new UnreachableException("Unknown symbol type");
                //     }
                // }
                // else
                {
                    Debug.Assert(ConvertTo(argumentType, LocalType.LuaValue, "", true), $"Should be able to convert from {argumentType} to LuaValue.");
                    method.StoreElement<LuaValue>();
                }
            }
            method.NewObject(ReflectionData.ArgumentSpan_ctor);
            method.CallVirtual(ReflectionData.LuaFunction_Invoke);
        });
    }

    private void EmitAssignment(NameValue name, LocalType localType, Action emitValue)
    {
        var assigneeData = _symbolTable[name];
        var local = GetLocal(name);

        // TODO: Uncomment once kevin-montrose/Sigil#67 gets fixed.
        // if (assigneeData.LocalType == LocalType.LuaValue && localType != LocalType.LuaValue)
        //     method.LoadLocalAddress(local);
        emitValue();
        // if (assigneeData.LocalType == LocalType.LuaValue && localType != LocalType.LuaValue)
        // {
        //     switch (localType)
        //     {
        //         case LocalType.Bool:
        //             method.Call(ReflectionData.LuaValue_BoolCtor);
        //             break;
        //         case LocalType.Long:
        //             method.Call(ReflectionData.LuaValue_LongCtor);
        //             break;
        //         case LocalType.Double:
        //             method.Call(ReflectionData.LuaValue_DoubleCtor);
        //             break;
        //         case LocalType.String:
        //             method.Call(ReflectionData.LuaValue_StringCtor);
        //             break;
        //         case LocalType.LuaFunction:
        //             method.Call(ReflectionData.LuaValue_FunctionCtor);
        //             break;
        //         case LocalType.None:
        //             throw new UnreachableException("None shouldn't exist here.");
        //         case LocalType.LuaValue:
        //             throw new UnreachableException("Shouldn't have entered this if.");
        //         default:
        //             throw new UnreachableException("Unknown symbol type");
        //     }
        // }
        // else
        {
            method.StoreLocal(local);
        }
    }

    private bool ConvertTo(LocalType sourceType, LocalType targetType, string failMessage, bool sourceIsAddress = false)
    {
        if (sourceType == targetType)
            return true;

        switch (sourceType, targetType)
        {
            case (LocalType.Bool, LocalType.LuaValue):
                method.NewObject<LuaValue, bool>();
                return true;

            case (LocalType.Double, LocalType.Long):
                var end = method.DefineLabel();
                method.Duplicate();
                method.Duplicate();
                method.Convert<long>();
                method.Convert<double>();
                method.BranchIfEqual(end);
                method.LoadConstant(failMessage);
                method.NewObject<LuaException, string>();
                method.Throw();
                method.MarkLabel(end);
                method.Convert<long>();
                return true;

            case (LocalType.Long, LocalType.Double):
                method.Convert<double>();
                return true;

            case (LocalType.LuaValue, LocalType.Bool):
                if (!sourceIsAddress)
                {
                    _slots.WithSlot(LocalType.LuaValue, (method, local) =>
                    {
                        method.StoreLocal(local);
                        method.LoadLocalAddress(local);
                    });
                }
                method.Call(ReflectionData.LuaValue_AsBoolean);
                return true;

            case (LocalType.LuaValue, LocalType.Long):
                if (!sourceIsAddress)
                {
                    _slots.WithSlot(LocalType.LuaValue, (method, local) =>
                    {
                        method.StoreLocal(local);
                        method.LoadLocalAddress(local);
                    });
                }
                method.Call(ReflectionData.LuaValue_ToInteger);
                return true;

            case (LocalType.LuaValue, LocalType.Double):
                if (!sourceIsAddress)
                {
                    _slots.WithSlot(LocalType.LuaValue, (method, local) =>
                    {
                        method.StoreLocal(local);
                        method.LoadLocalAddress(local);
                    });
                }
                method.Call(ReflectionData.LuaValue_ToNumber);
                return true;

            case (LocalType.LuaValue, LocalType.String):
                if (!sourceIsAddress)
                {
                    _slots.WithSlot(LocalType.LuaValue, (method, local) =>
                    {
                        method.StoreLocal(local);
                        method.LoadLocalAddress(local);
                    });
                }
                method.Call(ReflectionData.LuaValue_AsString);
                return true;

            case (LocalType.LuaValue, LocalType.LuaFunction):
                if (!sourceIsAddress)
                {
                    _slots.WithSlot(LocalType.LuaValue, (method, local) =>
                    {
                        method.StoreLocal(local);
                        method.LoadLocalAddress(local);
                    });
                }
                method.Call(ReflectionData.LuaValue_AsFunction);
                return true;

            default:
                if (targetType == LocalType.LuaValue)
                {
                    switch (sourceType)
                    {
                        case LocalType.Bool:
                            method.NewObject<LuaValue, bool>();
                            return true;
                        case LocalType.Long:
                            method.NewObject<LuaValue, long>();
                            return true;
                        case LocalType.Double:
                            method.NewObject<LuaValue, double>();
                            return true;
                        case LocalType.String:
                            method.NewObject<LuaValue, string>();
                            return true;
                        case LocalType.LuaFunction:
                            method.NewObject<LuaValue, LuaFunction>();
                            return true;
                        default:
                            throw new UnreachableException($"There should be no requests to convert from {sourceType} to {targetType}.");
                    }
                }
                return false;
        }
    }

    private void EmitOperand(Operand operand, bool asAddress = false)
    {
        switch (operand)
        {
            case Constant constant:
            {
                switch (constant.Kind)
                {
                    case ConstantKind.Nil:
                        if (asAddress)
                            method.LoadFieldAddress(ReflectionData.LuaValue_Nil);
                        else
                            method.NewObject<LuaValue>();
                        break;
                    case ConstantKind.Boolean:
                        method.LoadConstant(constant.Value is true);
                        break;
                    case ConstantKind.Number:
                        if (constant.Value is long i64)
                            method.LoadConstant(i64);
                        else
                            method.LoadConstant(CastHelper.FastUnbox<double>(constant.Value!));
                        break;
                    case ConstantKind.String:
                        method.LoadConstant(CastHelper.FastCast<string>(constant.Value));
                        break;
                }
                break;
            }
            case Builtin builtin:
            {
                switch (builtin.BuiltinId)
                {
                    case KnownBuiltins.assert:
                        method.LoadField(ReflectionData.Stdlib_AssertFunction);
                        break;
                    case KnownBuiltins.type:
                        method.LoadField(ReflectionData.Stdlib_TypeFunction);
                        break;
                    case KnownBuiltins.print:
                        method.LoadField(ReflectionData.Stdlib_PrintFunction);
                        break;
                    case KnownBuiltins.error:
                        method.LoadField(ReflectionData.Stdlib_ErrorFunction);
                        break;
                    case KnownBuiltins.tostring:
                        method.LoadField(ReflectionData.Stdlib_ToStringFunction);
                        break;
                }
                break;
            }
            case NameValue name:
            {
                var local = GetLocal(name);
                if (asAddress && local.LocalType == typeof(LuaValue))
                    method.LoadLocalAddress(local);
                else
                    method.LoadLocal(local);
                break;
            }
        }
    }

    private Local GetLocal(NameValue name)
    {
        if (method.Locals.Names.Contains(name.ToString()))
            return method.Locals[name.ToString()];
        return method.DeclareLocal(_symbolTable[name].LocalType.GetClrType(), name.ToString());
    }

    public MethodBuilder CreateMethod() => method.CreateMethod(OptimizationOptions.All);
}
