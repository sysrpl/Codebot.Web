namespace Codebot.Web;

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
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
    private static string approot;
    private static string webroot;

    /// <summary>
    /// Static constructor
    /// </summary>
    static App()
    {
        DisableCors = true;
        handlerKey = new object();
        errorKey = new object();
        HandlerType = "home.dchc";
    }

    /// <summary>
    /// Read from a file and keep a cached copy of its content
    /// </summary>
    public static string Read(string fileName) => FileCache.Read(fileName);

    /// <summary>
    /// Write to a file
    /// </summary>
    public static void Write(string fileName, string contents) => FileCache.Write(fileName, contents);

    /// <summary>
    /// The current HttpContext
    /// </summary>
    public static HttpContext Context => accessor.HttpContext;

    /// <summary>
    /// Attach and processes a BasicHandler
    /// </summary>
    public static void Attach(HttpContext context, IHttpHandler handler)
    {
        context.Items[handlerKey] = handler;
        handler.ProcessRequest(context);
    }

    /// <summary>
    /// Attach an error
    /// </summary>
    private static void SetError(HttpContext context, object error)
    {
        context.Items[errorKey] = error;
    }

    /// <summary>
    /// The last Error if any
    /// </summary>
    public static object GetLastError(HttpContext context) =>
        context.Items.TryGetValue(errorKey, value: out var value) ? value : null;

    /// <summary>
    /// The current handler if any
    /// </summary>
    public static BasicHandler CurrentHandler => Context.Items.TryGetValue(handlerKey, out var value) ? value as BasicHandler : null;

    /// <summary>
    /// The current ip address of the client
    /// </summary>
    public static string IpAddress => Context.Connection?.RemoteIpAddress?.ToString() ?? "UnknownIP";

    /// <summary>
    /// The current user agent of the client
    /// </summary>
    public static string UserAgent
    {
        get
        {
            var agent = "UnknownUserAgent";
            if (Context.Request.Headers.TryGetValue("User-Agent", out var values))
                agent = values.FirstOrDefault() ?? agent;
            return agent;
        }
    }

    /// <summary>
    /// The current requested path
    /// </summary>
    public static string RequestPath => Context.Request.Path.Value;

    /// <summary>
    /// The current response status code
    /// </summary>
    public static int StatusCode => Context.Response.StatusCode;

    /// <summary>
    /// IsLocal returns true if the client is from local area network
    /// </summary>
    public static bool IsLocal => IpAddress.StartsWith("192.168.0.") || IpAddress.StartsWith("192.168.1.");

    private static string CombinePath(string a, string b)
    {
        if (string.IsNullOrEmpty(a))
            a = string.Empty;
        if (string.IsNullOrEmpty(b))
            b = string.Empty;
        if (a.Contains(".."))
            return a;
        if (b.Contains(".."))
            return a;
        while (b.StartsWith("/"))
            b = b.Substring(1);
        return string.IsNullOrEmpty(b) ? a : Path.Combine(a, b);
    }

    public static bool DisableCors { get; set; }

    /// <summary>
    /// Map a path to application file path
    /// </summary>
    public static string AppPath(string path = "") => CombinePath(approot, path);

    /// <summary>
    /// Map a web request path to a physical file path
    /// </summary>
    /// <remarks>If path is empty the return value is the web root</remarks>
    public static string MapPath(string path = "") => MapPath(Context, path);

    /// <summary>
    /// Map a web request path to a physical file path given a context
    /// </summary>
    /// <remarks>If path is empty the return value is the web root</remarks>
    public static string MapPath(HttpContext context, string path = "")
    {
        if (string.IsNullOrEmpty(path))
            path = context.Request.Path.Value;
        path ??= string.Empty;
        if (path.StartsWith("/"))
            return CombinePath(webroot, path);
        var root = context.Request.Path.Value;
        root = CombinePath(webroot, root);
        return CombinePath(root, path);
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
            if (OnProcessRequest is not null)
            {
                var args = new ContextEventArgs(context);
                OnProcessRequest(args);
                if (args.Handled)
                    handler = args.Handler;
            }
            if (handler is not null)
            {
                requestHandled = true;
                Attach(context, handler);
            }
            else
            {
                var s = MapPath(context);
                s = Path.Combine(s, HandlerType);
                if (File.Exists(s))
                {
                    var path = context.Request.Path.Value;
                    if (path != null && !path.EndsWith('/'))
                    {
                        context.Response.Redirect(path + "/");
                        return;
                    }
                    s = Read(s);
                    var t = Type.GetType(s);
                    if (t is not null && Activator.CreateInstance(t) is IHttpHandler h)
                    {
                        requestHandled = true;
                        Attach(context, h);
                    }
                }
            }
        }
        catch (Exception e)
        {
            SetError(context, e);
            requestHandled = true;
            var errorHandled = false;
            if (OnError is not null)
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
        var builder = Host.CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(defaults =>
        {
            defaults
            .ConfigureServices(services =>
            {
                services.AddHttpContextAccessor();
                if (DisableCors)
                    services.AddCors(options =>
                    {
                        options.AddPolicy("AllowAll", builder =>
                        {
                            builder
                                .AllowAnyOrigin()
                                .AllowAnyMethod()
                                .AllowAnyHeader();
                        });
                    });
                services.Configure<ForwardedHeadersOptions>(options =>
                {
                    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                    options.KnownNetworks.Clear();
                    options.KnownProxies.Clear();
                });
            })
            .Configure((ctx, app) =>
            {
                approot = ctx.HostingEnvironment.ContentRootPath;
                webroot = ctx.HostingEnvironment.WebRootPath;
                accessor = app.ApplicationServices.GetRequiredService<IHttpContextAccessor>();
                if (args.Contains("--debug") || args.Contains("-d"))
                    app.UseDeveloperExceptionPage();
                if (DisableCors)
                    app.UseCors("AllowAll");
                app.UseForwardedHeaders();
                app.UseStaticFiles();
                app.Use(ProcessRequest);
                Security?.Start();
                OnStart?.Invoke(EventArgs.Empty);
            });
        });
        builder.Build().Run();
        OnStop?.Invoke(EventArgs.Empty);
        Security?.Stop();
    }
}
