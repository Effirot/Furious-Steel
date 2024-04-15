


using System.Diagnostics;
using System.Net;
using System.Security.Authentication;
using System.Text;
using Microsoft.VisualBasic;


Console.Clear();
var a = new HttpsRequestAgregator();
a.Start();

// HttpClient httpClient = new HttpClient(new HttpClientHandler()
// {
//     SslProtocols = SslProtocols.Tls12
// });
// httpClient.DefaultRequestHeaders.Add("Authorization", userprofile.Token);        

// while (true)
// {
//     await Task.Delay(1000);

//     using var value = await httpClient.GetAsync("https://127.0.0.1:8888/");
//     var bytes = await value.Content.ReadAsByteArrayAsync();

//     Console.WriteLine(Encoding.UTF8.GetString(bytes));
// }

Console.ReadKey();

public class HttpsRequestAgregator
{
    public bool IsActive { get; private set; } = false;

    private HttpListener httpServer = new ();
    private Thread? responseThread = null;

    public HttpsRequestAgregator()
    {
        httpServer.Prefixes.Add("http://127.0.0.1:8888/");
    }
    ~ HttpsRequestAgregator()
    {

    }

    public void Start()
    {
        httpServer.Start();

        IsActive = true;

        responseThread = new Thread (ResponseProcess);
        responseThread.Start();
    }
    public void Stop()
    {
        httpServer.Stop();

        IsActive = false;

        responseThread = new Thread (ResponseProcess);
        responseThread.Start();
    }

    private void ResponseProcess()
    {
        while (IsActive)
        {   
            AgragateResponse(httpServer.GetContext());
        }

        responseThread = null;
    }

    private async void AgragateResponse(HttpListenerContext httpListenerContext)
    {
        var values = Encoding.UTF8.GetBytes("AAA");

        httpListenerContext.Response.ContentLength64 = values.Length;
        await httpListenerContext.Response.OutputStream.WriteAsync(values);
        await httpListenerContext.Response.OutputStream.FlushAsync();
    }
}
