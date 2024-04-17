
using System.Reflection;
using System.Runtime.CompilerServices;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
public class HttpsRequestableAttribute : Attribute
{

}

public static class HttpsRequest
{
    private static MethodInfo[] AllMethods = AppDomain.CurrentDomain
        .GetAssemblies()
        .SelectMany(t => t.GetTypes())
        .SelectMany(t => t.GetMethods())
        .Where(t => t.IsStatic && t.ReturnType != null)
        .Where(m => m.GetCustomAttributes(typeof(HttpsRequestableAttribute), false).Length > 0)
        .ToArray();

    public static async Task<string> Invoke(string commandName, params string[] args)
    {
        var method = Array.Find(AllMethods, method => method.Name.ToLower() == commandName.ToLower());
    
        if (method == null)
        {
            throw new NullReferenceException("Method not found");
        }

        string result;
        if (IsAsyncMethod(method))
        {
            var preResult = await (Task<object>)method.Invoke(null, args)! ?? "command not found";
            result = preResult.ToString()!;
        }
        else
        {
            result = method.Invoke(null, args)?.ToString() ?? "command not found";
        }

        return result;
    }

    private static bool IsAsyncMethod(MethodInfo method)
    {
        var attrib = method.GetCustomAttribute(typeof(AsyncStateMachineAttribute));

        return (attrib is not null and AsyncStateMachineAttribute);
    }
}