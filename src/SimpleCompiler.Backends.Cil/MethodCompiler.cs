using System.Diagnostics;
using System.Reflection;
using SimpleCompiler.Backend.Cil.Emit;
using SimpleCompiler.Helpers;
using SimpleCompiler.IR;
using SimpleCompiler.Runtime;

namespace SimpleCompiler.Backends.Cil;

internal sealed class MethodCompiler(IrGraph ir, SymbolTable symbolTable, MethodBuilder method)
{
    private readonly SymbolTable _symbolTable = symbolTable;
    private readonly SlotPool _slots = new(method);
    private readonly ILGenerator _il = method.GetILGenerator();
    private readonly Dictionary<int, Label> _blockLabels = [];
    private readonly Dictionary<NameValue, LocalBuilder> _locals = [];
    public MethodBuilder Method => method;

    public static MethodCompiler Create(TypeBuilder typeBuilder, IrGraph ir, SymbolTable symbolTable, string name)
    {
        return new MethodCompiler(
            ir,
            symbolTable,
            typeBuilder.DefineMethod(
                name,
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                typeof(LuaValue), [typeof(ReadOnlySpan<LuaValue>)])
        );
    }

    public void Compile()
    {
        foreach (var block in ir.BasicBlocks)
            _blockLabels[block.Ordinal] = _il.DefineLabel();

        foreach (var block in ir.BasicBlocks)
        {
            _il.MarkLabel(_blockLabels[block.Ordinal]);
            foreach (var instruction in block.Instructions)
                EmitInstruction(instruction);
        }

        // Return nil by default.
        _il.Emit(OpCodes.Newobj, ReflectionData.LuaValue_NilCtor);
        _il.Emit(OpCodes.Ret);
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
                    _il.LoadLocalAddress(local);
                    _il.Emit(OpCodes.Initobj, typeof(LuaValue));
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
                _il.Emit(OpCodes.Br, _blockLabels[branch.Target.BlockOrdinal]);
                break;
            }
            case InstructionKind.ConditionalBranch:
            {
                var branch = CastHelper.FastCast<ConditionalBranch>(instruction);
                EmitOperand(branch.Condition);
                _il.Emit(OpCodes.Brtrue, _blockLabels[branch.TargetIfTrue.BlockOrdinal]);
                _il.Emit(OpCodes.Br, _blockLabels[branch.TargetIfFalse.BlockOrdinal]);
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
                            _il.Emit(OpCodes.Not);
                            break;
                        case LocalType.Long:
                            _il.Emit(OpCodes.Pop);
                            EmitOperand(Constant.False);
                            break;
                        case LocalType.Double:
                            _il.Emit(OpCodes.Pop);
                            EmitOperand(Constant.False);
                            break;
                        case LocalType.String:
                            _il.Emit(OpCodes.Pop);
                            EmitOperand(Constant.False);
                            break;
                        case LocalType.LuaFunction:
                            _il.Emit(OpCodes.Pop);
                            EmitOperand(Constant.False);
                            break;
                        case LocalType.LuaValue:
                            _il.Emit(OpCodes.Call, ReflectionData.LuaValue_IsTruthy);
                            _il.Emit(OpCodes.Not);
                            break;
                    }
                    break;
                case UnaryOperationKind.BitwiseNegation:
                    EmitOperand(assignment.Operand, true);
                    if (!ConvertTo(operandType, LocalType.Long, "Value does not have an integer representation.", true))
                    {
                        _il.Emit(OpCodes.Pop);
                        _il.LoadConstant((int) operandType.ToValueKind());
                        _il.EmitCall(OpCodes.Call, ReflectionData.LuaOperations_ThrowBitwiseError, null);
                    }
                    else
                    {
                        _il.Emit(OpCodes.Not);
                    }
                    break;
                case UnaryOperationKind.NumericalNegation:
                    EmitOperand(assignment.Operand, true);
                    switch (operandType)
                    {
                        case LocalType.Bool:
                        case LocalType.String:
                        case LocalType.LuaFunction:
                            _il.Emit(OpCodes.Pop);
                            _il.LoadConstant((int) operandType.ToValueKind());
                            _il.EmitCall(OpCodes.Call, ReflectionData.LuaOperations_ThrowBitwiseError, null);
                            break;
                        case LocalType.Long:
                        case LocalType.Double:
                            _il.Emit(OpCodes.Neg);
                            break;
                        case LocalType.LuaValue:
                            var ifTrue = _il.DefineLabel();
                            var end = _il.DefineLabel();

                            // stack = [*, LuaValue*]
                            _il.Emit(OpCodes.Dup);
                            // stack = [*.Kind, LuaValue*]
                            _il.Emit(OpCodes.Ldfld, ReflectionData.LuaValue_Kind);
                            // stack = [ValueKind.Long, *.Kind, LuaValue*]
                            _il.LoadConstant((int) ValueKind.Long);
                            // stack = [LuaValue*]
                            // if (x.Kind != ValueKind.Long)
                            _il.Emit(OpCodes.Brtrue, ifTrue);
                            //   stack = [LuaValue.AsLong()]
                            _il.Emit(OpCodes.Call, ReflectionData.LuaValue_AsLong);
                            _il.Emit(OpCodes.Br, end);
                            // else
                            _il.MarkLabel(ifTrue);
                            //   stack = [x.AsDouble()]
                            _il.Emit(OpCodes.Call, ReflectionData.LuaValue_AsDouble);
                            _il.MarkLabel(end);
                            // stack = [-top]
                            _il.Emit(OpCodes.Neg);
                            break;
                    }
                    break;
                case UnaryOperationKind.LengthOf:
                    EmitOperand(assignment.Operand, true);
                    if (!ConvertTo(operandType, LocalType.String, "", true))
                    {
                        _il.Emit(OpCodes.Pop);
                        _il.LoadConstant((int) operandType.ToValueKind());
                        _il.Emit(OpCodes.Call, ReflectionData.LuaOperations_ThrowLengthError);
                    }
                    else
                    {
                        _il.EmitCall(OpCodes.Callvirt, ReflectionData.string_Length, null);
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
                    var gotAddress = EmitOperand(assignment.Left, true);
                    conversionSuccessful &= ConvertTo(leftType, localType, "", gotAddress);
                    gotAddress = EmitOperand(assignment.Right, true);
                    conversionSuccessful &= ConvertTo(rightType, localType, "", gotAddress);
                    if (!conversionSuccessful)
                    {
                        _il.Emit(OpCodes.Pop);
                        _il.Emit(OpCodes.Pop);
                        _il.LoadConstant(leftType != LocalType.LuaValue ? (int) leftType.ToValueKind() : (int) rightType.ToValueKind());
                        _il.EmitCall(OpCodes.Call, ReflectionData.LuaOperations_ThrowArithmeticError, null);
                        if (localType == LocalType.Double)
                            _il.Emit(OpCodes.Conv_R8);
                    }
                    else
                    {
                        _il.Emit(OpCodes.Add);
                    }
                    break;
                }
                case BinaryOperationKind.Subtraction:
                {
                    var conversionSuccessful = true;
                    var gotAddress = EmitOperand(assignment.Left, true);
                    conversionSuccessful &= ConvertTo(leftType, localType, "", gotAddress);
                    gotAddress = EmitOperand(assignment.Right, true);
                    conversionSuccessful &= ConvertTo(rightType, localType, "", gotAddress);
                    if (!conversionSuccessful)
                    {
                        _il.Emit(OpCodes.Pop);
                        _il.Emit(OpCodes.Pop);
                        _il.LoadConstant(leftType != LocalType.LuaValue ? (int) leftType.ToValueKind() : (int) rightType.ToValueKind());
                        _il.EmitCall(OpCodes.Call, ReflectionData.LuaOperations_ThrowArithmeticError, null);
                        if (localType == LocalType.Double)
                            _il.Emit(OpCodes.Conv_R8);
                    }
                    else
                    {
                        _il.Emit(OpCodes.Sub);
                    }
                    break;
                }
                case BinaryOperationKind.Multiplication:
                {
                    var conversionSuccessful = true;
                    var gotAddress = EmitOperand(assignment.Left, true);
                    conversionSuccessful &= ConvertTo(leftType, localType, "", gotAddress);
                    gotAddress = EmitOperand(assignment.Right, true);
                    conversionSuccessful &= ConvertTo(rightType, localType, "", gotAddress);
                    if (!conversionSuccessful)
                    {
                        _il.Emit(OpCodes.Pop);
                        _il.Emit(OpCodes.Pop);
                        _il.LoadConstant(leftType != LocalType.LuaValue ? (int) leftType.ToValueKind() : (int) rightType.ToValueKind());
                        _il.EmitCall(OpCodes.Call, ReflectionData.LuaOperations_ThrowArithmeticError, null);
                        if (localType == LocalType.Double)
                            _il.Emit(OpCodes.Conv_R8);
                    }
                    else
                    {
                        _il.Emit(OpCodes.Mul);
                    }
                    break;
                }
                case BinaryOperationKind.Division:
                {
                    var conversionSuccessful = true;
                    var gotAddress = EmitOperand(assignment.Left, true);
                    conversionSuccessful &= ConvertTo(leftType, LocalType.Double, "", gotAddress);
                    gotAddress = EmitOperand(assignment.Right, true);
                    conversionSuccessful &= ConvertTo(rightType, LocalType.Double, "", gotAddress);
                    if (!conversionSuccessful)
                    {
                        _il.Emit(OpCodes.Pop);
                        _il.Emit(OpCodes.Pop);
                        _il.LoadConstant(leftType != LocalType.LuaValue ? (int) leftType.ToValueKind() : (int) rightType.ToValueKind());
                        _il.EmitCall(OpCodes.Call, ReflectionData.LuaOperations_ThrowArithmeticError, null);
                        _il.Emit(OpCodes.Conv_R8);
                    }
                    else
                    {
                        _il.Emit(OpCodes.Div);
                    }
                    break;
                }
                case BinaryOperationKind.IntegerDivision:
                {
                    var conversionSuccessful = true;
                    var gotAddress = EmitOperand(assignment.Left, true);
                    conversionSuccessful &= ConvertTo(leftType, LocalType.Double, "", gotAddress);
                    gotAddress = EmitOperand(assignment.Right, true);
                    conversionSuccessful &= ConvertTo(rightType, LocalType.Double, "", gotAddress);
                    if (!conversionSuccessful)
                    {
                        _il.Emit(OpCodes.Pop);
                        _il.Emit(OpCodes.Pop);
                        _il.LoadConstant(leftType != LocalType.LuaValue ? (int) leftType.ToValueKind() : (int) rightType.ToValueKind());
                        _il.EmitCall(OpCodes.Call, ReflectionData.LuaOperations_ThrowArithmeticError, null);
                    }
                    else
                    {
                        _il.Emit(OpCodes.Div);
                        _il.Emit(OpCodes.Conv_I8);
                    }
                    break;
                }
                case BinaryOperationKind.Exponentiation:
                {
                    var conversionSuccessful = true;
                    var gotAddress = EmitOperand(assignment.Left, true);
                    conversionSuccessful &= ConvertTo(leftType, LocalType.Double, "", gotAddress);
                    gotAddress = EmitOperand(assignment.Right, true);
                    conversionSuccessful &= ConvertTo(rightType, LocalType.Double, "", gotAddress);
                    if (!conversionSuccessful)
                    {
                        _il.Emit(OpCodes.Pop);
                        _il.Emit(OpCodes.Pop);
                        _il.LoadConstant(leftType != LocalType.LuaValue ? (int) leftType.ToValueKind() : (int) rightType.ToValueKind());
                        _il.EmitCall(OpCodes.Call, ReflectionData.LuaOperations_ThrowArithmeticError, null);
                        _il.Emit(OpCodes.Conv_R8);
                    }
                    else
                    {
                        _il.EmitCall(OpCodes.Call, ReflectionData.Math_Pow, null);
                    }
                    break;
                }
                case BinaryOperationKind.Modulo:
                {
                    var conversionSuccessful = true;
                    var gotAddress = EmitOperand(assignment.Left, true);
                    conversionSuccessful &= ConvertTo(leftType, localType, "", gotAddress);
                    gotAddress = EmitOperand(assignment.Right, true);
                    conversionSuccessful &= ConvertTo(rightType, localType, "", gotAddress);
                    if (!conversionSuccessful)
                    {
                        _il.Emit(OpCodes.Pop);
                        _il.Emit(OpCodes.Pop);
                        _il.LoadConstant(leftType != LocalType.LuaValue ? (int) leftType.ToValueKind() : (int) rightType.ToValueKind());
                        _il.EmitCall(OpCodes.Call, ReflectionData.LuaOperations_ThrowArithmeticError, null);
                        if (localType == LocalType.Double)
                            _il.Emit(OpCodes.Conv_R8);
                    }
                    else
                    {
                        _il.Emit(OpCodes.Rem);
                    }
                    break;
                }
                case BinaryOperationKind.Concatenation:
                {
                    var conversionSuccessful = true;
                    var gotAddress = EmitOperand(assignment.Left, true);
                    conversionSuccessful &= ConvertTo(leftType, localType, "", gotAddress);
                    gotAddress = EmitOperand(assignment.Right, true);
                    conversionSuccessful &= ConvertTo(rightType, localType, "", gotAddress);
                    if (!conversionSuccessful)
                    {
                        _il.Emit(OpCodes.Pop);
                        _il.Emit(OpCodes.Pop);
                        _il.LoadConstant(leftType != LocalType.LuaValue ? (int) leftType.ToValueKind() : (int) rightType.ToValueKind());
                        _il.EmitCall(OpCodes.Call, ReflectionData.LuaOperations_ThrowConcatError, null);
                    }
                    else
                    {
                        _il.EmitCall(OpCodes.Call, ReflectionData.string_Concat2, null);
                    }
                    break;
                }
                case BinaryOperationKind.BitwiseAnd:
                {
                    var conversionSuccessful = true;
                    var gotAddress = EmitOperand(assignment.Left, true);
                    conversionSuccessful &= ConvertTo(leftType, LocalType.Long, "", gotAddress);
                    gotAddress = EmitOperand(assignment.Right, true);
                    conversionSuccessful &= ConvertTo(rightType, LocalType.Long, "", gotAddress);
                    if (!conversionSuccessful)
                    {
                        _il.Emit(OpCodes.Pop);
                        _il.Emit(OpCodes.Pop);
                        _il.LoadConstant(leftType != LocalType.LuaValue ? (int) leftType.ToValueKind() : (int) rightType.ToValueKind());
                        _il.EmitCall(OpCodes.Call, ReflectionData.LuaOperations_ThrowBitwiseError, null);
                    }
                    else
                    {
                        _il.Emit(OpCodes.And);
                    }
                    break;
                }
                case BinaryOperationKind.BitwiseOr:
                {
                    var conversionSuccessful = true;
                    var gotAddress = EmitOperand(assignment.Left, true);
                    conversionSuccessful &= ConvertTo(leftType, LocalType.Long, "", gotAddress);
                    gotAddress = EmitOperand(assignment.Right, true);
                    conversionSuccessful &= ConvertTo(rightType, LocalType.Long, "", gotAddress);
                    if (!conversionSuccessful)
                    {
                        _il.Emit(OpCodes.Pop);
                        _il.Emit(OpCodes.Pop);
                        _il.LoadConstant(leftType != LocalType.LuaValue ? (int) leftType.ToValueKind() : (int) rightType.ToValueKind());
                        _il.EmitCall(OpCodes.Call, ReflectionData.LuaOperations_ThrowBitwiseError, null);
                    }
                    else
                    {
                        _il.Emit(OpCodes.Or);
                    }
                    break;
                }
                case BinaryOperationKind.BitwiseXor:
                {
                    var conversionSuccessful = true;
                    var gotAddress = EmitOperand(assignment.Left, true);
                    conversionSuccessful &= ConvertTo(leftType, LocalType.Long, "", gotAddress);
                    gotAddress = EmitOperand(assignment.Right, true);
                    conversionSuccessful &= ConvertTo(rightType, LocalType.Long, "", gotAddress);
                    if (!conversionSuccessful)
                    {
                        _il.Emit(OpCodes.Pop);
                        _il.Emit(OpCodes.Pop);
                        _il.LoadConstant(leftType != LocalType.LuaValue ? (int) leftType.ToValueKind() : (int) rightType.ToValueKind());
                        _il.EmitCall(OpCodes.Call, ReflectionData.LuaOperations_ThrowBitwiseError, null);
                    }
                    else
                    {
                        _il.Emit(OpCodes.Xor);
                    }
                    break;
                }
                case BinaryOperationKind.LeftShift:
                {
                    var conversionSuccessful = true;
                    var gotAddress = EmitOperand(assignment.Left, true);
                    conversionSuccessful &= ConvertTo(leftType, LocalType.Long, "", gotAddress);
                    gotAddress = EmitOperand(assignment.Right, true);
                    conversionSuccessful &= ConvertTo(rightType, LocalType.Long, "", gotAddress);
                    if (!conversionSuccessful)
                    {
                        _il.Emit(OpCodes.Pop);
                        _il.Emit(OpCodes.Pop);
                        _il.LoadConstant(leftType != LocalType.LuaValue ? (int) leftType.ToValueKind() : (int) rightType.ToValueKind());
                        _il.EmitCall(OpCodes.Call, ReflectionData.LuaOperations_ThrowBitwiseError, null);
                    }
                    else
                    {
                        _il.Emit(OpCodes.Shl);
                    }
                    break;
                }
                case BinaryOperationKind.RightShift:
                {
                    var conversionSuccessful = true;
                    var gotAddress = EmitOperand(assignment.Left, true);
                    conversionSuccessful &= ConvertTo(leftType, LocalType.Long, "", gotAddress);
                    gotAddress = EmitOperand(assignment.Right, true);
                    conversionSuccessful &= ConvertTo(rightType, LocalType.Long, "", gotAddress);
                    if (!conversionSuccessful)
                    {
                        _il.Emit(OpCodes.Pop);
                        _il.Emit(OpCodes.Pop);
                        _il.LoadConstant(leftType != LocalType.LuaValue ? (int) leftType.ToValueKind() : (int) rightType.ToValueKind());
                        _il.EmitCall(OpCodes.Call, ReflectionData.LuaOperations_ThrowBitwiseError, null);
                    }
                    else
                    {
                        _il.Emit(OpCodes.Shr);
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
                            _il.Emit(OpCodes.Ceq);
                            break;
                        case (LocalType.String, LocalType.String):
                            EmitOperand(assignment.Left);
                            EmitOperand(assignment.Right);
                            _il.EmitCall(OpCodes.Call, ReflectionData.string_Equality, null);
                            break;
                        case (LocalType.LuaValue, LocalType.LuaValue):
                            EmitOperand(assignment.Left);
                            EmitOperand(assignment.Right);
                            _il.EmitCall(OpCodes.Call, ReflectionData.LuaValue_Equality, null);
                            break;
                        default:
                        {

                            EmitOperand(assignment.Left);
                            var converted = ConvertTo(leftType, LocalType.LuaValue, "");
                            EmitOperand(assignment.Right);
                            converted &= ConvertTo(rightType, LocalType.LuaValue, "");
                            if (!converted)
                            {
                                _il.Emit(OpCodes.Pop);
                                _il.Emit(OpCodes.Pop);
                                _il.LoadConstant(false);
                            }
                            _il.EmitCall(OpCodes.Call, ReflectionData.LuaValue_Equality, null);
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
                            _il.Emit(OpCodes.Ceq);
                            _il.Emit(OpCodes.Not);
                            break;
                        case (LocalType.String, LocalType.String):
                            EmitOperand(assignment.Left);
                            EmitOperand(assignment.Right);
                            _il.EmitCall(OpCodes.Call, ReflectionData.string_Inequality, null);
                            break;
                        case (LocalType.LuaValue, LocalType.LuaValue):
                            EmitOperand(assignment.Left);
                            EmitOperand(assignment.Right);
                            _il.EmitCall(OpCodes.Call, ReflectionData.LuaValue_Inequality, null);
                            break;
                        default:
                        {

                            EmitOperand(assignment.Left);
                            var converted = ConvertTo(leftType, LocalType.LuaValue, "");
                            EmitOperand(assignment.Right);
                            converted &= ConvertTo(rightType, LocalType.LuaValue, "");
                            if (!converted)
                            {
                                _il.Emit(OpCodes.Pop);
                                _il.Emit(OpCodes.Pop);
                                _il.LoadConstant(true);
                            }
                            _il.EmitCall(OpCodes.Call, ReflectionData.LuaValue_Inequality, null);
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
                    _il.EmitCall(OpCodes.Call, ReflectionData.LuaOperations_LessThan, null);
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
                    _il.EmitCall(OpCodes.Call, ReflectionData.LuaOperations_LessThanOrEqual, null);
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
                    _il.EmitCall(OpCodes.Call, ReflectionData.LuaOperations_GreaterThan, null);
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
                    _il.EmitCall(OpCodes.Call, ReflectionData.LuaOperations_GreaterThanOrEqual, null);
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

            _il.LoadConstant(assignment.Arguments.Count);
            _il.Emit(OpCodes.Newarr, typeof(LuaValue));
            for (var i = 0; i < assignment.Arguments.Count; i++)
            {
                var argument = assignment.Arguments[i];
                var argumentType = InformationCollector.GetOperandType(argument, _symbolTable).ToLocalType();

                _il.Emit(OpCodes.Dup);
                _il.LoadConstant(i);
                // if (argumentType != LocalType.LuaValue)
                //     _il.Emit(OpCodes.Ldelema, typeof(LuaValue));
                var gotAddress = EmitOperand(argument);
                // if (argumentType != LocalType.LuaValue)
                // {
                //     switch (argumentType)
                //     {
                //         case LocalType.Bool:
                //             _il.Emit(OpCodes.Call, ReflectionData.LuaValue_BoolCtor);
                //             break;
                //         case LocalType.Long:
                //             _il.Emit(OpCodes.Call, ReflectionData.LuaValue_LongCtor);
                //             break;
                //         case LocalType.Double:
                //             _il.Emit(OpCodes.Call, ReflectionData.LuaValue_DoubleCtor);
                //             break;
                //         case LocalType.String:
                //             _il.Emit(OpCodes.Call, ReflectionData.LuaValue_StringCtor);
                //             break;
                //         case LocalType.LuaFunction:
                //             _il.Emit(OpCodes.Call, ReflectionData.LuaValue_FunctionCtor);
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
                    if (!ConvertTo(argumentType, LocalType.LuaValue, ""))
                        Debug.Assert(false, $"Should be able to convert from {argumentType} to LuaValue.");
                    _il.Emit(OpCodes.Stelem, typeof(LuaValue));
                }
            }
            _il.Emit(OpCodes.Newobj, ReflectionData.ArgumentSpan_ArrayCtor);
            _il.EmitCall(OpCodes.Callvirt, ReflectionData.LuaFunction_Invoke, null);
        });
    }

    private void EmitAssignment(NameValue name, LocalType localType, Action emitValue)
    {
        var assigneeData = _symbolTable[name];
        var local = GetLocal(name);

        // if (assigneeData.LocalType == LocalType.LuaValue && localType != LocalType.LuaValue)
        //     _il.LoadLocalAddress(local);
        emitValue();
        // if (assigneeData.LocalType == LocalType.LuaValue && localType != LocalType.LuaValue)
        // {
        //     switch (localType)
        //     {
        //         case LocalType.Bool:
        //             _il.Emit(OpCodes.Call, ReflectionData.LuaValue_BoolCtor);
        //             break;
        //         case LocalType.Long:
        //             _il.Emit(OpCodes.Call, ReflectionData.LuaValue_LongCtor);
        //             break;
        //         case LocalType.Double:
        //             _il.Emit(OpCodes.Call, ReflectionData.LuaValue_DoubleCtor);
        //             break;
        //         case LocalType.String:
        //             _il.Emit(OpCodes.Call, ReflectionData.LuaValue_StringCtor);
        //             break;
        //         case LocalType.LuaFunction:
        //             _il.Emit(OpCodes.Call, ReflectionData.LuaValue_FunctionCtor);
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
            _il.StoreLocal(local);
        }
    }

    private bool ConvertTo(LocalType sourceType, LocalType targetType, string failMessage, bool luaValueIsAddress = false)
    {
        if (sourceType == targetType)
            return true;

        switch (sourceType, targetType)
        {
            case (LocalType.Bool, LocalType.LuaValue):
                _il.Emit(OpCodes.Newobj, ReflectionData.LuaValue_BoolCtor);
                return true;

            case (LocalType.Double, LocalType.Long):
                var end = _il.DefineLabel();
                _il.Emit(OpCodes.Dup);
                _il.Emit(OpCodes.Dup);
                _il.Emit(OpCodes.Conv_I8);
                _il.Emit(OpCodes.Conv_R8);
                _il.Emit(OpCodes.Brtrue, end);
                _il.LoadConstant(failMessage);
                _il.Emit(OpCodes.Newobj, ReflectionData.LuaException_StringCtor);
                _il.Emit(OpCodes.Throw);
                _il.MarkLabel(end);
                _il.Emit(OpCodes.Conv_I8);
                return true;

            case (LocalType.Long, LocalType.Double):
                _il.Emit(OpCodes.Conv_R8);
                return true;

            case (LocalType.LuaValue, LocalType.Bool):
                if (!luaValueIsAddress)
                {
                    _slots.WithSlot(LocalType.LuaValue, (method, local) =>
                    {
                        _il.StoreLocal(local);
                        _il.LoadLocalAddress(local);
                        _il.EmitCall(OpCodes.Call, ReflectionData.LuaValue_AsBoolean, null);
                    });
                }
                else
                {
                    _il.EmitCall(OpCodes.Call, ReflectionData.LuaValue_AsBoolean, null);
                }
                return true;

            case (LocalType.LuaValue, LocalType.Long):
                if (!luaValueIsAddress)
                {
                    _slots.WithSlot(LocalType.LuaValue, (method, local) =>
                    {
                        _il.StoreLocal(local);
                        _il.LoadLocalAddress(local);
                        _il.EmitCall(OpCodes.Call, ReflectionData.LuaValue_ToInteger, null);
                    });
                }
                else
                {
                    _il.EmitCall(OpCodes.Call, ReflectionData.LuaValue_ToInteger, null);
                }
                return true;

            case (LocalType.LuaValue, LocalType.Double):
                if (!luaValueIsAddress)
                {
                    _slots.WithSlot(LocalType.LuaValue, (method, local) =>
                    {
                        _il.StoreLocal(local);
                        _il.LoadLocalAddress(local);
                        _il.EmitCall(OpCodes.Call, ReflectionData.LuaValue_ToNumber, null);
                    });
                }
                else
                {
                    _il.EmitCall(OpCodes.Call, ReflectionData.LuaValue_ToNumber, null);
                }
                return true;

            case (LocalType.LuaValue, LocalType.String):
                if (!luaValueIsAddress)
                {
                    _slots.WithSlot(LocalType.LuaValue, (method, local) =>
                    {
                        _il.StoreLocal(local);
                        _il.LoadLocalAddress(local);
                        _il.EmitCall(OpCodes.Call, ReflectionData.LuaValue_AsString, null);
                    });
                }
                else
                {
                    _il.EmitCall(OpCodes.Call, ReflectionData.LuaValue_AsString, null);
                }
                return true;

            case (LocalType.LuaValue, LocalType.LuaFunction):
                if (!luaValueIsAddress)
                {
                    _slots.WithSlot(LocalType.LuaValue, (method, local) =>
                    {
                        _il.StoreLocal(local);
                        _il.LoadLocalAddress(local);
                        _il.EmitCall(OpCodes.Call, ReflectionData.LuaValue_AsFunction, null);
                    });
                }
                else
                {
                    _il.EmitCall(OpCodes.Call, ReflectionData.LuaValue_AsFunction, null);
                }
                return true;

            default:
                if (targetType == LocalType.LuaValue)
                {
                    switch (sourceType)
                    {
                        case LocalType.Bool:
                            _il.Emit(OpCodes.Newobj, ReflectionData.LuaValue_BoolCtor);
                            return true;
                        case LocalType.Long:
                            _il.Emit(OpCodes.Newobj, ReflectionData.LuaValue_LongCtor);
                            return true;
                        case LocalType.Double:
                            _il.Emit(OpCodes.Newobj, ReflectionData.LuaValue_DoubleCtor);
                            return true;
                        case LocalType.String:
                            _il.Emit(OpCodes.Newobj, ReflectionData.LuaValue_StringCtor);
                            return true;
                        case LocalType.LuaFunction:
                            _il.Emit(OpCodes.Newobj, ReflectionData.LuaValue_FunctionCtor);
                            return true;
                        default:
                            throw new UnreachableException($"There should be no requests to convert from {sourceType} to {targetType}.");
                    }
                }
                return false;
        }
    }

    private bool EmitOperand(Operand operand, bool luaValueAsAddress = false)
    {
        switch (operand)
        {
            case Constant constant:
            {
                switch (constant.Kind)
                {
                    case ConstantKind.Nil:
                        _il.Emit(OpCodes.Newobj, ReflectionData.LuaValue_NilCtor);
                        break;
                    case ConstantKind.Boolean:
                        _il.LoadConstant(constant.Value is true);
                        break;
                    case ConstantKind.Number:
                        if (constant.Value is long i64)
                            _il.LoadConstant(i64);
                        else
                            _il.LoadConstant(CastHelper.FastUnbox<double>(constant.Value!));
                        break;
                    case ConstantKind.String:
                        _il.LoadConstant(CastHelper.FastCast<string>(constant.Value)!);
                        break;
                }
                break;
            }
            case Builtin builtin:
            {
                switch (builtin.BuiltinId)
                {
                    case KnownBuiltins.assert:
                        _il.Emit(OpCodes.Ldsfld, ReflectionData.Stdlib_AssertFunction);
                        break;
                    case KnownBuiltins.type:
                        _il.Emit(OpCodes.Ldsfld, ReflectionData.Stdlib_TypeFunction);
                        break;
                    case KnownBuiltins.print:
                        _il.Emit(OpCodes.Ldsfld, ReflectionData.Stdlib_PrintFunction);
                        break;
                    case KnownBuiltins.error:
                        _il.Emit(OpCodes.Ldsfld, ReflectionData.Stdlib_ErrorFunction);
                        break;
                    case KnownBuiltins.tostring:
                        _il.Emit(OpCodes.Ldsfld, ReflectionData.Stdlib_ToStringFunction);
                        break;
                }
                break;
            }
            case NameValue name:
            {
                var local = GetLocal(name);
                if (luaValueAsAddress && local.LocalType == typeof(LuaValue))
                {
                    _il.LoadLocalAddress(local);
                    return true;
                }
                else
                {
                    _il.LoadLocal(local);
                    break;
                }
            }
        }
        return false;
    }

    private LocalBuilder GetLocal(NameValue name)
    {
        if (!_locals.TryGetValue(name, out var local))
            _locals[name] = local = _il.DeclareLocal(_symbolTable[name].LocalType.GetClrType());
        return local;
    }
}
