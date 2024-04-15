

public class ItemServer
{
    public static ItemServer Singleton { get; private set; } = new();

    private ItemServer() => Start();

    private void Start()
    {

    }
}