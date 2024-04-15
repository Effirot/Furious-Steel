
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite; 

public class AccountsDataBaseServer
{
    public static AccountsDataBaseServer Instance { get; private set; } = new AccountsDataBaseServer();

    private SqliteConnection connection = new SqliteConnection("Data Source=usersdata.db");

    public bool isLoggingEnabled = true;

    private AccountsDataBaseServer() => Start(); 
    ~AccountsDataBaseServer() => connection.Dispose(); 

    private void Log(string text)
    {
        if (isLoggingEnabled)
        {
            Console.WriteLine(@$"[{DateTime.Now.ToString()}] - " + text);
        }
    }

    private void Start()
    {
        Log("Starting account dataBase server . . .");

        connection.Open();

        SendCommand(@$"
            CREATE TABLE IF NOT EXISTS Accounts (
                ID INTEGER UNIQUE PRIMARY KEY AUTOINCREMENT, 
                
                Name TEXT UNIQUE NOT NULL, 
                Password TEXT NOT NULL, 
                EMail TEXT NOT NULL, 
                
                IsOnline BOOL DEFAULT TRUE,

                LastOnline SNALLDATETIME,
                RegistrationDate SNALLDATETIME,

                CurrentRoomID INTEGER DEFAULT NULL,
                BanWhile SNALLDATETIME DEFAULT NULL,

                InventorySize SNALLINT DEFAULT 12);
                
            CREATE TABLE IF NOT EXISTS OppenedRooms (
                ID INTEGER UNIQUE PRIMARY KEY AUTOINCREMENT, 
                
                Name TEXT, 
                Password TEXT, 
                Address TEXT, 

                MaxPlayersCount SNALLINT
                CurrentPlayersCount SNALLINT);");

        Log("Account dataBase server was succesfully started");
    }

    public bool RegisterAccount(string Name, string Password, string Email)
    {
        try
        {
            GC.SuppressFinalize(new System.Net.Mail.MailAddress(Email));
        }
        catch (System.Exception)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Log("Email is not match");
            Console.ResetColor();

            return false;
        }
        
        return TrySendCommand(@$"
            INSERT INTO Accounts (
                Name, 
                Password, 
                EMail, 

                LastOnline,
                RegistrationDate) 
            VALUES (
                '{Name}', 
                '{Password}', 
                '{Email}', 

                '{DateTime.Now}', 
                '{DateTime.Now}');", out _);
    }
    public async Task<bool> RegisterAccountAsync(string Name, string Password, string Email)
    {
        try
        {
            GC.SuppressFinalize(new System.Net.Mail.MailAddress(Email));
        }
        catch (System.Exception)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Log("Email is not match");
            Console.ResetColor();

            return false;
        }
        
        try
        {
            var value = await SendCommandAsync(@$"
                INSERT INTO Accounts (
                    Name, 
                    Password, 
                    EMail, 

                    LastOnline,
                    RegistrationDate) 
                VALUES (
                    '{Name}', 
                    '{Password}', 
                    '{Email}', 

                    '{DateTime.Now}', 
                    '{DateTime.Now}');");

            return true;
        }
        catch (System.Exception e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Log(e.Message);
            Console.ResetColor();
        }

        return false;

    }

    public bool IsAccountRegistred(string Name)
    {
        return SendCommand<long>($"SELECT EXISTS(SELECT 1 FROM Accounts WHERE Name=\'{Name}\')") != 0;
    }
    public async Task<bool> IsAccountRegistredAsync(string Name)
    {
        return await SendCommandAsync<long>($"SELECT EXISTS(SELECT 1 FROM Accounts WHERE Name=\'{Name}\')") != 0;
    }

    public long IndexOfAccount(string Name)
    {
        return SendCommand<long>($"SELECT EXISTS(SELECT 1 FROM Accounts WHERE Name=\'{Name}\')");
    }
    public async Task<long> IndexOfAccountAsync(string Name)
    {
        return await SendCommandAsync<long>($"SELECT EXISTS(SELECT 1 FROM Accounts WHERE Name=\'{Name}\')");
    }


    private async Task<SqliteDataReader> SendCommandAsync(string SqliteCommand)
    {
        using (SqliteCommand command = new SqliteCommand())
        {
            command.Connection = connection;
            command.CommandText = SqliteCommand;

            return await command.ExecuteReaderAsync();
        }
    }
    private async Task<T?> SendCommandAsync<T>(string SqliteCommand) where T : notnull
    {
        using (SqliteCommand command = new SqliteCommand())
        {
            command.Connection = connection;
            command.CommandText = SqliteCommand;

            var value = await command.ExecuteScalarAsync();

            return (T)value!;
        }
    }
    private SqliteDataReader SendCommand(string SqliteCommand)
    {
        using (SqliteCommand command = new SqliteCommand())
        {
            command.Connection = connection;
            command.CommandText = SqliteCommand;

            return command.ExecuteReader();
        }
    }
    private T? SendCommand<T>(string SqliteCommand) where T : notnull
    {
        using (SqliteCommand command = new SqliteCommand())
        {
            command.Connection = connection;
            command.CommandText = SqliteCommand;

            return (T) command.ExecuteScalar()!;
        }
    }
    private bool TrySendCommand(string SqliteCommand, out SqliteDataReader? sqliteData)
    {
        try {
            using (SqliteCommand command = new SqliteCommand())
            {
                command.Connection = connection;
                command.CommandText = SqliteCommand;

                sqliteData = command.ExecuteReader();

                return true;
            }
        }
        catch (Exception e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Log(e.Message);
            Console.ResetColor();
        }
        
        sqliteData = null;
        return false;
    }
}