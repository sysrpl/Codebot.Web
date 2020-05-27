using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Codebot.Web
{
    /// <summary>
    /// The Website class is used to run the web server
    /// </summary>
    public sealed class Website
    {
        private static readonly object locker = new object();
        private static readonly Dictionary<string, DateTime> fileDates = new Dictionary<string, DateTime>();
        private static readonly Dictionary<string, string> fileContent = new Dictionary<string, string>();

        /// <summary>
        /// Read from files by keeping a cached copy of their content
        /// </summary>
        public static string FileRead(string fileName)
        {
            lock (locker)
            {
                if (File.Exists(fileName))
                {
                    string content;
                    var a = DateTime.UnixEpoch;
                    if (fileDates.ContainsKey(fileName))
                        a = fileDates[fileName];
                    else
                        fileDates.Add(fileName, a);
                    var b = File.GetLastWriteTimeUtc(fileName);
                    if (a != b)
                    {
                        fileDates[fileName] = b;
                        content = File.ReadAllText(fileName).Trim();
                        fileContent[fileName] = content;
                    }
                    else
                        content = fileContent[fileName];
                    return content;
                }
                return string.Empty;
            }
        }

        /// <summary>
        /// Write a file using content
        /// </summary>
        public static void FileWrite(string fileName, string content)
        {
            File.WriteAllText(fileName, content);
        }

        public static IUserSecurity UserSecurity { get; set; }

        /// <summary>
        /// HandlerType contains "home.dchc" but it can be adjusted to any value
        /// you want
        /// </summary>
        public static string HandlerType { get; set; }

        static Website()
        {
             HandlerType = "home.dchc";
        }

        /// <summary>
        /// Context events are called in this order
        /// </summary>
        public static event EventHandler<ContextEventArgs> OnStart;
        public static event EventHandler<ContextEventArgs> OnStartRequest;
        public static event EventHandler<ContextEventArgs> OnProcessRequest;
        public static event EventHandler<ContextEventArgs> OnError;
        public static event EventHandler<ContextEventArgs> OnFinishRequest;

        private static bool started;
        private void Start(HttpContext ctx)
        {
            if (!started)
                lock (locker)
                    if (!started)
                    {
                        started = true;
                        OnStart?.Invoke(this, new ContextEventArgs(ctx));
                    }
        }

        /// <summary>
        /// Look for a home.acdc and try to convert it into a BasicHandler,
        /// otherwise serve static files. We also reject attempts to read from
        /// parent folders.
        /// </summary>
        private async Task ProcessRequest(HttpContext ctx, Func<Task> next)
        {
            Start(ctx);
            OnStartRequest?.Invoke(this, new ContextEventArgs(ctx));
            var requestHandled = false;
            try
            {
                IHttpHandler handler = null;
                if (OnProcessRequest != null)
                {
                    var args = new ContextEventArgs(ctx);
                    OnProcessRequest(this, args);
                    handler = args.Handler;
                }
                if (handler != null)
                {
                    requestHandled = true;
                    WebState.Attach(handler as BasicHandler);
                    await Task.Run(() => handler.ProcessRequest(ctx));
                }
                else
                {
                    var s = WebState.MapPath("");
                    if (Directory.Exists(s))
                    {
                        if (s.Contains(".."))
                            s = string.Empty;
                        else
                            s = FileRead(Path.Combine(s, HandlerType));
                        if (s.Length > 0)
                        {
                            var t = Type.GetType(s);
                            if (t != null)
                            {
                                if (Activator.CreateInstance(t) is BasicHandler b)
                                {
                                    requestHandled = true;
                                    WebState.Attach(b);
                                    await Task.Run(() => b.ProcessRequest(ctx));
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                requestHandled = true;
                var errorHandled = false;
                if (OnError != null)
                {
                    var args = new ContextEventArgs(ctx, e);
                    OnError(this, args);
                    errorHandled = args.Handled;
                }
                if (!errorHandled)
                    throw;
            }
            if (!requestHandled)
                await next().ConfigureAwait(false);
            OnFinishRequest?.Invoke(this, new ContextEventArgs(ctx));
        }

        /// <summary>
        /// Request IHttpContextAccessor as a service
        /// </summary>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpContextAccessor();
        }

        /// <summary>
        /// Before processing request we have a chance to modify the app
        /// </summary>
        /// <remarks>We may want to set server limits here such as max request size</remarks>
        public void Configure(IApplicationBuilder app)
        {
            var accessor = app.ApplicationServices.GetRequiredService<IHttpContextAccessor>();
            WebState.Configure(accessor);
            if (debug)
                app.UseDeveloperExceptionPage();
            app.UseStaticFiles();
            app.Use(ProcessRequest);
        }

        /// <summary>
        /// Debug pages are enabled by adding --debug to Run args
        /// </summary>
        private static bool debug;

        /// <summary>
        /// This is the main entry point for this framework
        /// </summary>
        public static void Run(string[] args)
        {
            debug = args.Contains("--debug");
            var host = Host
                .CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(w => w.UseStartup<Website>());
            host.Build().Run();
        }
    }
}

