using System.Linq.Expressions;
using System.Reflection;

namespace SimpleCompiler.Helpers;

public class ExpressionHelper
{
    public static MethodInfo MethodInfo(Expression<Action> expression) => (expression.Body as MethodCallExpression)!.Method;
    public static MethodInfo MethodInfo<T>(Expression<Action<T>> expression) => (expression.Body as MethodCallExpression)!.Method;
    public static MethodInfo MethodInfo<T>(Expression<Func<T>> expression) => (expression.Body as MethodCallExpression)!.Method;
    public static PropertyInfo PropertyInfo(Expression<Func<object>> expression) => ((expression.Body as MemberExpression)!.Member as PropertyInfo)!;
    public static PropertyInfo PropertyInfo<T>(Expression<Func<T, object>> expression) => ((expression.Body as MemberExpression)!.Member as PropertyInfo)!;
    public static FieldInfo FieldInfo(Expression<Func<object>> expression) => ((expression.Body as MemberExpression)!.Member as FieldInfo)!;
    public static FieldInfo FieldInfo<T>(Expression<Func<T, object>> expression) => ((expression.Body as MemberExpression)!.Member as FieldInfo)!;
    public static ConstructorInfo ConstructorInfo<T>(Expression<Func<T>> expression) => (expression.Body as NewExpression)!.Constructor!;
}
