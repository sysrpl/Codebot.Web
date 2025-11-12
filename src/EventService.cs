using System.Text.Json;
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
/// Send messages to the clients by writing
///
///     App.FindEvent("/movies").Broadcast("'A new movie was added!'");
///
/// You can access via javascript like so:
///
///     const movieEvents = new EventSource('/movies');
///     movieEvents.onmessage = (e: MessageEvent) => {
///         let obj = JSON.parse(e.data);
///         console.log(obj);
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

    readonly List<Connection> connections = [];
    readonly object mutex = new();
    long connects = 0;
    long disconnects = 0;
    readonly string name = "";

    public ServiceEvent(string endpoint)
    {
        name = string.IsNullOrWhiteSpace(endpoint) ? "unknown" : endpoint.Trim();
    }

    internal async Task AddRequest(HttpContext context)
    {
        context.Response.ContentType = "text/event-stream";
        context.Response.Headers.CacheControl = "no-cache";
        context.Response.Headers.Connection = "keep-alive";
        var c = new Connection(context.Response);
        lock (mutex)
        {
            connections.Add(c);
            connects++;
            Console.WriteLine($"service {name} add (addrequest): connects {connects} | disconnects {disconnects} | actual {connections.Count}");
        }
        try
        {
            var cancel = context.RequestAborted;
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
            lock (mutex)
            {
                if (connections.Contains(c))
                {
                    connections.Remove(c);
                    disconnects++;
                    Console.WriteLine($"service {name} remove (addrequest): connects {connects} | disconnects {disconnects} | actual {connections.Count}");
                }
            }
        }
    }

    /// <summary>
    /// Broadcast pushes text data to every client that is connected
    /// </summary>
    /// <param name="json">The valid json string data to push</param>
    /// <remarks>Broadcast requires a json compatible string or your message
    /// will be ignored</remarks>
    public async Task Broadcast(string json)
    {
        try
        {
            var obj = JsonSerializer.Deserialize<JsonElement>(json);
            json = JsonSerializer.Serialize(obj);
        }
        catch
        {
            return;
        }
        List<Connection> snapshot;
        lock (mutex)
            snapshot = connections.ToList();
        var s = $"data:{json}\n\n";
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
                if (connections.Contains(c))
                {
                    connections.Remove(c);
                    disconnects++;
                    Console.WriteLine($"service {name} remove (broadcast): connects {connects} | disconnects {disconnects} | actual {connections.Count}");
                }
            }
        });
        await Task.WhenAll(tasks);
    }
}
