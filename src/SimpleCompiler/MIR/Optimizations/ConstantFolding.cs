using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace SimpleCompiler.MIR;

public sealed class ConstantFolder : MirRewriter
{
    private static readonly ConstantFolder s_instance = new();

    [return: NotNullIfNotNull(nameof(node))]
    public static MirNode? ConstantFold(MirNode? node) => s_instance.Visit(node);

    public override MirNode VisitUnaryOperationExpression(UnaryOperationExpression node) => ExtractConstantValue(node);
    public override MirNode VisitBinaryOperationExpression(BinaryOperationExpression node) => ExtractConstantValue(node);
    private static Expression ExtractConstantValue(Expression expression)
    {
        var constVal = expression.GetConstantValue();
        return constVal switch
        {
            { HasValue: true, Value: bool val } => MirFactory.ConstantExpression(null, ResultKind.Bool, ConstantKind.Boolean, val),
            { HasValue: true, Value: double val } => MirFactory.ConstantExpression(null, ResultKind.Double, ConstantKind.Number, val),
            { HasValue: true, Value: long val } => MirFactory.ConstantExpression(null, ResultKind.Int, ConstantKind.Number, val),
            { HasValue: true, Value: string val } => MirFactory.ConstantExpression(null, ResultKind.Str, ConstantKind.String, val),
            _ => expression
        };
    }
}

public static class ConstantFoldingExtensions
{
    public static TypedConstant GetConstantValue(this MirNode node) =>
        Visitor.GetTypedConstant(node);

    private sealed class Visitor : MirVisitor<TypedConstant>
    {
        private static Visitor s_instance = new();
        public static TypedConstant GetTypedConstant(MirNode node) => s_instance.Visit(node);

        private static readonly ConditionalWeakTable<MirNode, object> s_cache = [];
        private static TypedConstant GetOrSetValue(MirNode node, Func<MirNode, TypedConstant> func) =>
            Unsafe.Unbox<TypedConstant>(s_cache.GetValue(node, node => func(node)));

        private Func<MirNode, TypedConstant>? _visit;
        public override TypedConstant Visit(MirNode? node) =>
            node is null ? TypedConstant.None : GetOrSetValue(node, _visit ??= base.Visit);

        public override TypedConstant VisitConstantExpression(ConstantExpression node)
        {
            return (node.ConstantKind, node.Value) switch
            {
                (ConstantKind.Nil, _) => TypedConstant.Nil,
                (ConstantKind.Boolean, true) => TypedConstant.True,
                (ConstantKind.Boolean, false) => TypedConstant.False,
                (ConstantKind.Number, long val) => new TypedConstant(val),
                (ConstantKind.Number, double val) => new TypedConstant(val),
                (ConstantKind.String, string val) => new TypedConstant(val),
                _ => throw new NotImplementedException($"Constant value obtaning for {node.ConstantKind} with value {node.Value} ({node?.GetType().FullName}) has not been implemented."),
            };
        }

        public override TypedConstant VisitUnaryOperationExpression(UnaryOperationExpression node)
        {
            var operand = Visit(node);
            return (node.UnaryOperationKind, operand) switch
            {
                (UnaryOperationKind.BitwiseNegation, { HasValue: true, Value: long n }) => new TypedConstant(~n),
                (UnaryOperationKind.LogicalNegation, { HasValue: true, Value: var x }) => new TypedConstant(!(x is null or false)),
                (UnaryOperationKind.NumericalNegation, { HasValue: true, Value: long n }) => new TypedConstant(-n),
                (UnaryOperationKind.NumericalNegation, { HasValue: true, Value: double n }) => new TypedConstant(-n),

                _ => TypedConstant.None
            };
        }

        public override TypedConstant VisitBinaryOperationExpression(BinaryOperationExpression node)
        {
            var left = Visit(node.Left);
            if (!left.HasValue)
                return TypedConstant.None;

            var right = Visit(node.Right);
            if (!right.HasValue)
                return TypedConstant.None;

            return (node.BinaryOperationKind, left, right) switch
            {
                (BinaryOperationKind.Addition, { HasValue: true, Value: long l }, { HasValue: true, Value: long r }) => new TypedConstant(l + r),
                (BinaryOperationKind.Addition, { HasValue: true, Value: double l }, { HasValue: true, Value: long r }) => new TypedConstant(l + r),
                (BinaryOperationKind.Addition, { HasValue: true, Value: long l }, { HasValue: true, Value: double r }) => new TypedConstant(l + r),
                (BinaryOperationKind.Addition, { HasValue: true, Value: double l }, { HasValue: true, Value: double r }) => new TypedConstant(l + r),
                (BinaryOperationKind.Subtraction, { HasValue: true, Value: long l }, { HasValue: true, Value: long r }) => new TypedConstant(l - r),
                (BinaryOperationKind.Subtraction, { HasValue: true, Value: double l }, { HasValue: true, Value: long r }) => new TypedConstant(l - r),
                (BinaryOperationKind.Subtraction, { HasValue: true, Value: long l }, { HasValue: true, Value: double r }) => new TypedConstant(l - r),
                (BinaryOperationKind.Subtraction, { HasValue: true, Value: double l }, { HasValue: true, Value: double r }) => new TypedConstant(l - r),
                (BinaryOperationKind.Multiplication, { HasValue: true, Value: long l }, { HasValue: true, Value: long r }) => new TypedConstant(l * r),
                (BinaryOperationKind.Multiplication, { HasValue: true, Value: double l }, { HasValue: true, Value: long r }) => new TypedConstant(l * r),
                (BinaryOperationKind.Multiplication, { HasValue: true, Value: long l }, { HasValue: true, Value: double r }) => new TypedConstant(l * r),
                (BinaryOperationKind.Multiplication, { HasValue: true, Value: double l }, { HasValue: true, Value: double r }) => new TypedConstant(l * r),
                (BinaryOperationKind.Division, { HasValue: true, Value: long l }, { HasValue: true, Value: long r }) => new TypedConstant((double)(l / r)),
                (BinaryOperationKind.Division, { HasValue: true, Value: double l }, { HasValue: true, Value: long r }) => new TypedConstant(l / r),
                (BinaryOperationKind.Division, { HasValue: true, Value: long l }, { HasValue: true, Value: double r }) => new TypedConstant(l / r),
                (BinaryOperationKind.Division, { HasValue: true, Value: double l }, { HasValue: true, Value: double r }) => new TypedConstant(l / r),
                (BinaryOperationKind.IntegerDivision, { HasValue: true, Value: long l }, { HasValue: true, Value: long r }) => new TypedConstant(l / r),
                (BinaryOperationKind.IntegerDivision, { HasValue: true, Value: double l }, { HasValue: true, Value: long r }) => new TypedConstant((long)(l / r)),
                (BinaryOperationKind.IntegerDivision, { HasValue: true, Value: long l }, { HasValue: true, Value: double r }) => new TypedConstant((long)(l / r)),
                (BinaryOperationKind.IntegerDivision, { HasValue: true, Value: double l }, { HasValue: true, Value: double r }) => new TypedConstant((long)(l / r)),
                (BinaryOperationKind.Modulo, { HasValue: true, Value: int l }, { HasValue: true, Value: int r }) => new TypedConstant(l % r),
                (BinaryOperationKind.Modulo, { HasValue: true, Value: double l }, { HasValue: true, Value: int r }) => new TypedConstant(l % r),
                (BinaryOperationKind.Modulo, { HasValue: true, Value: int l }, { HasValue: true, Value: double r }) => new TypedConstant(l % r),
                (BinaryOperationKind.Modulo, { HasValue: true, Value: double l }, { HasValue: true, Value: double r }) => new TypedConstant(l % r),
                (BinaryOperationKind.Exponentiation, { HasValue: true, Value: int l }, { HasValue: true, Value: int r }) => new TypedConstant(Math.Pow(l, r)),
                (BinaryOperationKind.Exponentiation, { HasValue: true, Value: double l }, { HasValue: true, Value: int r }) => new TypedConstant(Math.Pow(l, r)),
                (BinaryOperationKind.Exponentiation, { HasValue: true, Value: int l }, { HasValue: true, Value: double r }) => new TypedConstant(Math.Pow(l, r)),
                (BinaryOperationKind.Exponentiation, { HasValue: true, Value: double l }, { HasValue: true, Value: double r }) => new TypedConstant(Math.Pow(l, r)),

                (BinaryOperationKind.Equals, { HasValue: true, Value: string l }, { HasValue: true, Value: string r }) => new TypedConstant(l == r),
                (BinaryOperationKind.Equals, { HasValue: true, Value: long l }, { HasValue: true, Value: double r }) => new TypedConstant(l == r),
                (BinaryOperationKind.Equals, { HasValue: true, Value: double l }, { HasValue: true, Value: long r }) => new TypedConstant(l == r),
                (BinaryOperationKind.Equals, { HasValue: true, Value: long l }, { HasValue: true, Value: long r }) => new TypedConstant(l == r),
                (BinaryOperationKind.Equals, { HasValue: true, Value: double l }, { HasValue: true, Value: double r }) => new TypedConstant(l == r),
                (BinaryOperationKind.Equals, { HasValue: true, Value: bool l }, { HasValue: true, Value: bool r }) => new TypedConstant(l == r),
                (BinaryOperationKind.Equals, { HasValue: true, Value: null }, { HasValue: true, Value: null }) => new TypedConstant(true),

                (BinaryOperationKind.NotEquals, { HasValue: true, Value: string l }, { HasValue: true, Value: string r }) => new TypedConstant(l != r),
                (BinaryOperationKind.NotEquals, { HasValue: true, Value: long l }, { HasValue: true, Value: double r }) => new TypedConstant(l != r),
                (BinaryOperationKind.NotEquals, { HasValue: true, Value: double l }, { HasValue: true, Value: long r }) => new TypedConstant(l != r),
                (BinaryOperationKind.NotEquals, { HasValue: true, Value: long l }, { HasValue: true, Value: long r }) => new TypedConstant(l != r),
                (BinaryOperationKind.NotEquals, { HasValue: true, Value: double l }, { HasValue: true, Value: double r }) => new TypedConstant(l != r),
                (BinaryOperationKind.NotEquals, { HasValue: true, Value: bool l }, { HasValue: true, Value: bool r }) => new TypedConstant(l != r),
                (BinaryOperationKind.NotEquals, { HasValue: true, Value: null }, { HasValue: true, Value: null }) => new TypedConstant(false),

                (BinaryOperationKind.LessThan, { HasValue: true, Value: string l }, { HasValue: true, Value: string r }) => new TypedConstant(l.CompareTo(r) < 0),
                (BinaryOperationKind.LessThan, { HasValue: true, Value: long l }, { HasValue: true, Value: double r }) => new TypedConstant(l < r),
                (BinaryOperationKind.LessThan, { HasValue: true, Value: double l }, { HasValue: true, Value: long r }) => new TypedConstant(l < r),
                (BinaryOperationKind.LessThan, { HasValue: true, Value: long l }, { HasValue: true, Value: long r }) => new TypedConstant(l < r),
                (BinaryOperationKind.LessThan, { HasValue: true, Value: double l }, { HasValue: true, Value: double r }) => new TypedConstant(l < r),

                (BinaryOperationKind.LessThanOrEquals, { HasValue: true, Value: string l }, { HasValue: true, Value: string r }) => new TypedConstant(l.CompareTo(r) <= 0),
                (BinaryOperationKind.LessThanOrEquals, { HasValue: true, Value: long l }, { HasValue: true, Value: double r }) => new TypedConstant(l <= r),
                (BinaryOperationKind.LessThanOrEquals, { HasValue: true, Value: double l }, { HasValue: true, Value: long r }) => new TypedConstant(l <= r),
                (BinaryOperationKind.LessThanOrEquals, { HasValue: true, Value: long l }, { HasValue: true, Value: long r }) => new TypedConstant(l <= r),
                (BinaryOperationKind.LessThanOrEquals, { HasValue: true, Value: double l }, { HasValue: true, Value: double r }) => new TypedConstant(l <= r),

                (BinaryOperationKind.GreaterThan, { HasValue: true, Value: string l }, { HasValue: true, Value: string r }) => new TypedConstant(l.CompareTo(r) > 0),
                (BinaryOperationKind.GreaterThan, { HasValue: true, Value: long l }, { HasValue: true, Value: double r }) => new TypedConstant(l > r),
                (BinaryOperationKind.GreaterThan, { HasValue: true, Value: double l }, { HasValue: true, Value: long r }) => new TypedConstant(l > r),
                (BinaryOperationKind.GreaterThan, { HasValue: true, Value: long l }, { HasValue: true, Value: long r }) => new TypedConstant(l > r),
                (BinaryOperationKind.GreaterThan, { HasValue: true, Value: double l }, { HasValue: true, Value: double r }) => new TypedConstant(l > r),

                (BinaryOperationKind.GreaterThanOrEquals, { HasValue: true, Value: string l }, { HasValue: true, Value: string r }) => new TypedConstant(l.CompareTo(r) >= 0),
                (BinaryOperationKind.GreaterThanOrEquals, { HasValue: true, Value: long l }, { HasValue: true, Value: double r }) => new TypedConstant(l >= r),
                (BinaryOperationKind.GreaterThanOrEquals, { HasValue: true, Value: double l }, { HasValue: true, Value: long r }) => new TypedConstant(l >= r),
                (BinaryOperationKind.GreaterThanOrEquals, { HasValue: true, Value: long l }, { HasValue: true, Value: long r }) => new TypedConstant(l >= r),
                (BinaryOperationKind.GreaterThanOrEquals, { HasValue: true, Value: double l }, { HasValue: true, Value: double r }) => new TypedConstant(l >= r),

                // Values are equal but not the same types.
                (BinaryOperationKind.Equals, { HasValue: true }, { HasValue: true }) => new TypedConstant(false),
                (BinaryOperationKind.NotEquals, { HasValue: true }, { HasValue: true }) => new TypedConstant(true),
                _ => TypedConstant.None,
            };
        }

        protected override TypedConstant DefaultVisit(MirNode node) => TypedConstant.None;
    }
}
