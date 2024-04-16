


using System.Diagnostics;
using System.Net;
using System.Security.Authentication;
using System.Text;
using Effiry.Items;
using Microsoft.VisualBasic;



var a = new HttpsRequestAgregator();
a.Start();

while ()
{

}

a.Stop();

public class HttpsRequestAgregator
{
    public delegate Task<string> AgregateCallback(HttpListenerContext context);

    public bool IsActive { get; private set; } = false;

    private HttpListener httpServer = new ();
    private Thread? responseThread = null;

    private AgregateCallback agregateCallback;

    public HttpsRequestAgregator(AgregateCallback agregateCallback, params string[] prefixes)
    {
        this.agregateCallback = agregateCallback;
        
        foreach (var prefix in prefixes)
        {
            httpServer.Prefixes.Add(prefix);
        }
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
    }

    private void ResponseProcess()
    {
        while (IsActive)
        {   
            try
            {
                AgragateResponse(httpServer.GetContext());
            }
            catch (HttpListenerException) { }
            catch (Exception) { throw; }
        }

        responseThread = null;
        Stop();
    }

    private async void AgragateResponse(HttpListenerContext httpListenerContext)
    {
        var values = Encoding.UTF8.GetBytes(await agregateCallback.Invoke(httpListenerContext));

        httpListenerContext.Response.ContentLength64 = values.Length;
        await httpListenerContext.Response.OutputStream.WriteAsync(values);
        await httpListenerContext.Response.OutputStream.FlushAsync();
    }
}