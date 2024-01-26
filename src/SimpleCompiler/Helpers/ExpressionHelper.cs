using System.Linq.Expressions;
using System.Reflection;

namespace SimpleCompiler.Helpers;

public class ExpressionHelper
{
    public static MethodInfo MethodInfo(Expression<Action> expression) => ((MethodCallExpression)expression.Body)!.Method;
    public static MethodInfo MethodInfo<T>(Expression<Action<T>> expression) => ((MethodCallExpression)expression.Body)!.Method;
    public static MethodInfo MethodInfo<T>(Expression<Func<T>> expression) => ((MethodCallExpression)expression.Body)!.Method;
    public static PropertyInfo PropertyInfo(Expression<Func<object>> expression) => (PropertyInfo)((MemberExpression)expression.Body).Member;
    public static PropertyInfo PropertyInfo<T>(Expression<Func<T, object>> expression) => (PropertyInfo)((MemberExpression)expression.Body).Member;
    public static MethodInfo PropertyGet(Expression<Func<object>> expression) => ((PropertyInfo)((MemberExpression)expression.Body).Member).GetMethod!;
    public static MethodInfo PropertyGet<T>(Expression<Func<T, object>> expression) => ((PropertyInfo)((MemberExpression)expression.Body).Member).GetMethod!;
    public static FieldInfo FieldInfo<T>(Expression<Func<T>> expression) => (FieldInfo)((MemberExpression)expression.Body).Member;
    public static FieldInfo FieldInfo<T>(Expression<Func<T, object>> expression) => (FieldInfo)((MemberExpression)expression.Body).Member;
    public static ConstructorInfo ConstructorInfo<T>(Expression<Func<T>> expression) => ((NewExpression)expression.Body)!.Constructor!;
}
