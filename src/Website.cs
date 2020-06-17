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
        public static void FileWrite(string fileName, string content) =>
            File.WriteAllText(fileName, content);

        /// <summary>
        /// Optional user security
        /// </summary>
        public static IUserSecurity UserSecurity { get; private set;}

        /// <summary>
        /// HandlerType contains "home.dchc" but it can be adjusted to any value
        /// you want
        /// </summary>
        public static string HandlerType { get; set; }

        static Website() => HandlerType = "home.dchc";

        /// <summary>
        /// Context events are called in this order
        /// </summary>
        public static event EventHandler<ContextEventArgs> OnStart;
        public static event EventHandler<ContextEventArgs> OnBeginRequest;
        public static event EventHandler<ContextEventArgs> OnProcessRequest;
        public static event EventHandler<ContextEventArgs> OnError;
        public static event EventHandler<ContextEventArgs> OnEndRequest;

        private static bool started;
        private void Start(HttpContext context)
        {
            if (!started)
                lock (locker)
                    if (!started)
                    {
                        started = true;
                        UserSecurity?.Start(context);
                        OnStart?.Invoke(this, new ContextEventArgs(context));
                    }
        }

        /// <summary>
        /// Look for a home.acdc and try to convert it into a BasicHandler,
        /// otherwise serve static files. We also reject attempts to read from
        /// parent folders.
        /// </summary>
        private async Task ProcessRequest(HttpContext context, Func<Task> next)
        {
            Start(context);
            UserSecurity?.BeginReuqest(context);
            OnBeginRequest?.Invoke(this, new ContextEventArgs(context));
            var requestHandled = false;
            try
            {
                IHttpHandler handler = null;
                if (OnProcessRequest != null)
                {
                    var args = new ContextEventArgs(context);
                    OnProcessRequest(this, args);
                    handler = args.Handler;
                }
                if (handler != null)
                {
                    requestHandled = true;
                    await WebState.Attach(handler as BasicHandler);
                }
                else
                {
                    var s = WebState.MapPath(string.Empty);
                    s = Path.Combine(s, HandlerType);
                    if (File.Exists(s))
                    {
                        s = s.Contains("..") ? s = string.Empty : FileRead(s);
                        if (s.Length > 0)
                        {
                            var t = Type.GetType(s);
                            if (t != null && Activator.CreateInstance(t) is BasicHandler b)
                            {
                                requestHandled = true;
                                await WebState.Attach(b);
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
                    var args = new ContextEventArgs(context, e);
                    OnError(this, args);
                    errorHandled = args.Handled;
                }
                if (!errorHandled)
                    throw;
            }
            if (!requestHandled)
                await next();
            OnEndRequest?.Invoke(this, new ContextEventArgs(context));
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
        /// Oplationally add user security
        /// </summary>
        public static void UseSecurity(IUserSecurity security)
        {
            if (UserSecurity is null)
                UserSecurity = security;
        }

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
