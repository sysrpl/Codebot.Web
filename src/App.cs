namespace Codebot.Web;

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public delegate void StaticEventHandler<T>(T args);
public delegate void StaticEventHandler(EventArgs args);

/// <summary>
/// The App class is used to run the web server
/// </summary>
public static class App
{
    private static IHttpContextAccessor accessor;
    private static readonly object handlerKey;
    private static readonly object errorKey;
    private static readonly string approot;
    private static readonly string webroot;

    /// <summary>
    /// Static constructor
    /// </summary>
    static App()
    {
        handlerKey = new object();
        errorKey = new object();
        approot = Directory.GetCurrentDirectory();
        webroot = Path.Combine(approot, "wwwroot");
        HandlerType = "home.dchc";
    }

    /// <summary>
    /// Read from a file and keep a cached copy of its content
    /// </summary>
    public static string Read(string fileName)
    {
        return FileCache.Read(fileName);
    }

    /// <summary>
    /// Write to a file
    /// </summary>
    public static void Write(string fileName, string contents)
    {
        FileCache.Write(fileName, contents);
    }

    /// <summary>
    /// The current HttpContext
    /// </summary>
    public static HttpContext Context { get => accessor.HttpContext; }

    /// <summary>
    /// Attach and processes a BasicHandler
    /// </summary>
    public static void Attach(HttpContext context, IHttpHandler handler)
    {
        if (context.Items.ContainsKey(handlerKey))
            context.Items[handlerKey] = handler;
        else
            context.Items.Add(handlerKey, handler);
        handler.ProcessRequest(context);
    }

    /// <summary>
    /// Attach an error
    /// </summary>
    private static void SetError(HttpContext context, object error)
    {
        if (context.Items.ContainsKey(errorKey))
            context.Items[errorKey] = error;
        else
            context.Items.Add(errorKey, error);
    }

    /// <summary>
    /// The last Error if any
    /// </summary>
    public static object GetLastError(HttpContext context) =>
        context.Items.ContainsKey(errorKey) ? context.Items[errorKey] : null;

    /// <summary>
    /// The current handler if any
    /// </summary>
    public static BasicHandler CurrentHandler
    {
        get
        {
            var c = Context;
            return c.Items.ContainsKey(handlerKey) ? c.Items[handlerKey] as BasicHandler : null;
        }
    }

    /// <summary>
    /// The current ip address of the client
    /// </summary>
    public static string IpAddress { get => Context.Connection.RemoteIpAddress.ToString(); }

    /// <summary>
    /// The current user agent of the client
    /// </summary>
    public static string UserAgent { get => Context.Request.Headers["User-Agent"].ToString(); }

    /// <summary>
    /// The current requested path
    /// </summary>
    public static string RequestPath { get => Context.Request.Path.Value; }

    /// <summary>
    /// The current response status code
    /// </summary>
    public static int StatusCode { get => Context.Response.StatusCode; }

    /// <summary>
    /// Map a path to application file path
    /// </summary>
    public static string AppPath(string path = "")
    {
        return string.IsNullOrEmpty(path) ? approot : Path.Combine(approot, path);
    }

    /// <summary>
    /// Map a web request path to a physical file path
    /// </summary>
    /// <remarks>If path is empty the return value is the requested path</remarks>
    public static string MapPath(string path = "")
    {
        // TODO ponder blocking ".." in path for security reasons
        if (string.IsNullOrEmpty(path))
            path = RequestPath;
        if (path.StartsWith("/"))
        {
            path = path.Substring(1);
            return string.IsNullOrEmpty(path) ? webroot : Path.Combine(webroot, path);
        }
        var root = RequestPath;
        if (root.StartsWith("/"))
            root = root.Substring(1);
        root = string.IsNullOrEmpty(root) ? webroot : Path.Combine(webroot, root);
        return Path.Combine(root, path);
    }

    /// <summary>
    /// Optional user security
    /// </summary>
    public static IUserSecurity Security { get; private set; }

    /// <summary>
    /// Allow user security to be set
    /// </summary>
    public static void UseSecurity(IUserSecurity security)
    {
        if (Security is null)
            Security = security;
    }

    /// <summary>
    /// HandlerType contains "home.dchc" but it can be adjusted to any value
    /// you want
    /// </summary>
    public static string HandlerType { get; set; }

    /// <summary>
    /// Context events are called in this order
    /// </summary>
    public static event StaticEventHandler OnStart;
    public static event StaticEventHandler OnStop;
    public static event StaticEventHandler<ContextEventArgs> OnBeginRequest;
    public static event StaticEventHandler<ContextEventArgs> OnProcessRequest;
    public static event StaticEventHandler<ContextEventArgs> OnError;
    public static event StaticEventHandler<ContextEventArgs> OnEndRequest;

    /// <summary>
    /// Look for a home.acdc and try to convert it into a BasicHandler,
    /// otherwise serve static files. We also reject attempts to read from
    /// parent folders.
    /// </summary>
    private static async Task ProcessRequest(HttpContext context, Func<Task> next)
    {
        Security?.RestoreUser(context);
        OnBeginRequest?.Invoke(new ContextEventArgs(context));
        var requestHandled = false;
        try
        {
            IHttpHandler handler = null;
            if (!(OnProcessRequest is null))
            {
                var args = new ContextEventArgs(context);
                OnProcessRequest(args);
                if (args.Handled)
                    handler = args.Handler;
            }
            if (!(handler is null))
            {
                requestHandled = true;
                Attach(context, handler);
            }
            else
            {
                var s = MapPath();
                s = Path.Combine(s, HandlerType);
                if (File.Exists(s))
                {
                    s = s.Contains("..") ? string.Empty : Read(s);
                    if (s.Length > 0)
                    {
                        var t = Type.GetType(s);
                        if (!(t is null) && Activator.CreateInstance(t) is IHttpHandler h)
                        {
                            requestHandled = true;
                            Attach(context, h);
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            SetError(context, e);
            requestHandled = true;
            var errorHandled = false;
            if (!(OnError is null))
            {
                var args = new ContextEventArgs(context, e);
                OnError.Invoke(args);
                errorHandled = args.Handled;
            }
            if (!errorHandled)
                throw;
        }
        if (!requestHandled)
            await next();
        OnEndRequest?.Invoke(new ContextEventArgs(context));
    }

    /// <summary>
    /// This is the main entry point for this framework
    /// </summary>
    public static void Run(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(builder =>
        {
            builder
            .ConfigureServices(services => services.AddHttpContextAccessor())
            .Configure(app =>
            {
                accessor = app.ApplicationServices.GetRequiredService<IHttpContextAccessor>();
                if (args.Contains("--debug") || args.Contains("-d"))
                    app.UseDeveloperExceptionPage();
                app.UseStaticFiles();
                app.Use(ProcessRequest);
            });
        });
        Security?.Start();
        OnStart?.Invoke(EventArgs.Empty);
        host.Build().Run();
        OnStop?.Invoke(EventArgs.Empty);
        Security?.Stop();
    }
}
