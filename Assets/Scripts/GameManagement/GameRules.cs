
using Mirror;

public struct SyncGameRulesMessage : NetworkMessage
{
    public GameRules data;
}

public struct GameRules
{
    public static GameRules current { get; private set; } = Default();

    public static GameRules Default() => new GameRules()
    {
        AllowChampionMode = true,
    };

    public static void SetAsCurrent(GameRules config)
    {
        current = config;
    }
    public static void SetAsCurrent(SyncGameRulesMessage config)
    {
        current = config.data;
    }
    public static void Reset()
    {
        SetAsCurrent(Default());
    }
 
    public bool AllowChampionMode;

    // public bool ;

}
