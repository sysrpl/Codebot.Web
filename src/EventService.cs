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
    private readonly List<HttpResponse> connections = new();
    private readonly object mutex = new();

    /// <summary>
    /// Do not use
    /// </summary>
    public async Task AddRequest(HttpContext context)
    {
        context.Response.Headers.Append("Content-Type", "text/event-stream");
        context.Response.Headers.Append("Cache-Control", "no-cache");
        context.Response.Headers.Append("Connection", "keep-alive");
        lock (mutex)
            connections.Add(context.Response);
        try
        {
            await Task.Delay(-1, context.RequestAborted);
        }
        catch (TaskCanceledException)
        {
            // Client disconnected
        }
        finally
        {
            lock (mutex)
                connections.Remove(context.Response);
        }
    }

    /// <summary>
    /// Broadcast pushes text data to every client that is connected
    /// </summary>
    /// <param name="message">The data to push</param>
    public async Task Broadcast(string message)
    {
        message = message.Replace("\n", "\\n").Replace("\r", "\\r");
        var s = $"data:{message}\n\n";
        List<HttpResponse> connections;
        lock (mutex)
            connections = this.connections.ToList();
        foreach (var c in connections)
        {
            try
            {
                await c.WriteAsync(s);
                await c.Body.FlushAsync();
            }
            catch
            {
                lock (mutex)
                    this.connections.Remove(c);
            }
        }
    }
}
