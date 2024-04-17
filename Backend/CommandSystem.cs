
using System.Reflection;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.ReturnValue, Inherited = false, AllowMultiple = true)]
public class CommandAttribute : Attribute
{

}

public static class Command
{
    private static MethodInfo[] AllMethods = AppDomain.CurrentDomain
        .GetAssemblies()
        .SelectMany(t => t.GetTypes())
        .SelectMany(t => t.GetMethods())
        .Where(t => t.IsStatic)
        .Where(t => t.GetCustomAttributes(typeof(CommandAttribute), false).Length > 0)
        .ToArray();

    public static void Invoke(string commandName, params string[] args)
    {
        var method = Array.Find(AllMethods, method => method.Name.ToLower() == commandName.ToLower());
    
        if (method == null)
        {
            throw new NullReferenceException("Method not found");
        }

        method.Invoke(null, args);
    }
}