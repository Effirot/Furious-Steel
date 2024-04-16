
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
        .Where(m => m.GetCustomAttributes(typeof(CommandAttribute), false).Length > 0)
        .ToArray();

    public static void Invoke(string commandName)
    {

    }
}