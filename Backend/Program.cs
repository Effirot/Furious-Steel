
using System.Diagnostics;
using System.Net;
using System.Runtime.CompilerServices;
using System.Security.Authentication;
using System.Text;
using System.Text.RegularExpressions;
using Effiry.Items;

#region --- Main

Console.Clear();
AccountsDataBaseServer.Initialize();

var prefix = "http://127.0.0.1:8888/account/";
var agregator = new HttpsRequestAgregator(AgragateFunction, prefix);
agregator.Start();

while (true)
{
    var input = Console.ReadLine() ?? "";
    
    if (input.ToLower() == "stop")
        break;

    var splitInput = Regex.Split(input, @"\s+");
    
    try
    {
        Command.Invoke(splitInput[0], splitInput.Skip(1).ToArray());
    }
    catch (Exception e)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(e.Message);
        Console.ResetColor();
    }
} 

agregator.Stop();

#endregion

async Task<string> AgragateFunction(HttpListenerContext context)
{
    var Request = context.Request.Url!.AbsoluteUri.Replace(prefix, "");
    
    var command = Regex.Match(Request, @"\w+").Value;
    var args = Regex.Matches(Request.Remove(0, command.Length), @"([^,(]?[\w|@|.]+[^,)]?)");
    
    foreach (var item in args)
    {
        Console.WriteLine(item);
    }
    return await HttpsRequest.Invoke(command, args.Select(a => a.Value).ToArray());
}

public static class AdditiveCommands
{
    [Command]
    public static void Register(string Name, string Password, string Email)
    {
        AccountsDataBaseServer.Instance.RegisterAccount(Name, Password, Email);
    }
}

public static class HttpCommands
{
    [HttpsRequestable]
    public static bool Validate(string Name, string Password)
    {
        return AccountsDataBaseServer.Instance.ValidateAccount(Name, Password);
    }

    [HttpsRequestable]
    public static async Task<bool> Register(string Name, string Password, string email)
    {
        Console.WriteLine(email);
        return await AccountsDataBaseServer.Instance.RegisterAccountAsync(Name, Password, Password);
    }
}