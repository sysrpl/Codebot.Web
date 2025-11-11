using Microsoft.AspNetCore.Http;

namespace Codebot.Web;

/// <summary>
/// ServiceEvent maintains an open connection to the client, allowing the server to
/// push text data to one or more persistently connected clients
/// </summary>
/// <example>
/// To use register your endpoint before running App
///
///     App.RegisterEvent("/movies");
///
/// Send messages to the clients by wwriting
///
///     App.FindEvent("/movies").Broadcast("A new movie was added!")
///
/// You can access via javascript like so:
///
///     const movieEvents = new EventSource('/movies');
///     movieEvents.onmessage = (e: MessageEvent) => {
///         let s = e.data;
///         s = s.replace(/\\n/g, "\n").replace(/\\r/g, "\r");
///         console.log(s);
///     };
/// </example>
public class ServiceEvent
{
    internal class Connection
    {
        internal HttpResponse Response { get; }
        internal SemaphoreSlim WriteLock { get; } = new(1, 1);
        internal Connection(HttpResponse response) => Response = response;
    }

    private readonly List<Connection> connections = [];

    internal async Task AddRequest(HttpContext context)
    {
        context.Response.ContentType = "text/event-stream";
        context.Response.Headers.CacheControl = "no-cache";
        context.Response.Headers.Connection = "keep-alive";
        var c = new Connection(context.Response);
        lock (this)
            connections.Add(c);
        var cancel = context.RequestAborted;
        await c.Response.Body.FlushAsync(cancel);
        try
        {
            while (!cancel.IsCancellationRequested)
            {
                await c.WriteLock.WaitAsync(cancel);
                try
                {
                    await c.Response.WriteAsync(":\n\n", cancel);
                    await c.Response.Body.FlushAsync(cancel);
                }
                finally
                {
                    c.WriteLock.Release();
                }
                await Task.Delay(TimeSpan.FromSeconds(30), cancel);
            }
        }
        catch
        {
            // swallow all exceptions (disconnects, etc.)
        }
        finally
        {
            lock (this)
                connections.Remove(c);
        }
    }

    /// <summary>
    /// Broadcast pushes text data to every client that is connected
    /// </summary>
    /// <param name="message">The data to push</param>
    public async Task Broadcast(string message)
    {
        if (string.IsNullOrEmpty(message))
            message = string.Empty;
        message = message.Replace("\n", "\\n").Replace("\r", "\\r");
        var s = $"data:{message}\n\n";
        List<Connection> snapshot;
        lock (this)
            snapshot = connections.ToList();
        var tasks = snapshot.Select(async c =>
        {
            try
            {
                await c.WriteLock.WaitAsync();
                try
                {
                    await c.Response.WriteAsync(s);
                    await c.Response.Body.FlushAsync();
                }
                finally
                {
                    c.WriteLock.Release();
                }
            }
            catch
            {
                lock (this)
                    connections.Remove(c);
            }
        });
        await Task.WhenAll(tasks);
    }
}
