using System.Linq.Expressions;
using System.Reflection;

namespace SimpleCompiler.Helpers;

public class ExpressionHelper
{
    public static MethodInfo MethodInfo(Expression<Action> expression) => (expression.Body as MethodCallExpression)!.Method;
    public static MethodInfo MethodInfo<T>(Expression<Func<T>> expression) => (expression.Body as MethodCallExpression)!.Method;
}
