using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace Codebot.Web
{
    /// <summary>
    /// The WebSite class is used to run the web server
    /// </summary>
    public sealed class Website
    {
        static readonly object locker = new object();
        static readonly Dictionary<string, DateTime> fileDates = new Dictionary<string, DateTime>();
        static readonly Dictionary<string, string> fileContent = new Dictionary<string, string>();

        /// <summary>
        /// HandlerType contains "home.ashx" but it can be adjusted to any value
        /// you want
        /// </summary>
        public static string HandlerType { get; set; }

        static Website()
        {
            HandlerType = "home.ashx";
        }

        /// <summary>
        /// Read from files by keeping a cached copy of their content
        /// </summary>
        static string FileContent(string fileName)
        {
            lock (locker)
            {
                if (File.Exists(fileName))
                {
                    string content;
                    var a = fileDates[fileName];
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
        /// Look for a home.ashx and try to convert it into a BasicHandler,
        /// otherwise serve static files. We also reject attempts to read from
        /// parent folders 
        /// </summary>
        async Task ProcessRequest(HttpContext ctx, Func<Task> next)
        {
            var handled = false;
            var s = BasicHandler.MapPath(ctx.Request.Path.Value);
            if (Directory.Exists(s))
            {
                if (s.Contains(".."))
                    s = string.Empty;
                else
                    s = FileContent(Path.Combine(s, HandlerType));
                if (s.Length > 0)
                {
                    var t = Type.GetType(s);
                    if (t != null)
                    {
                        if (Activator.CreateInstance(t) is BasicHandler b)
                        {
                            handled = true;
                            await Task.Run(() => b.ProcessRequest(ctx));
                        }
                    }
                }
            }
            if (!handled)
                await next();
        }

        /// <summary>
        /// Before processing request we have a chance to modify the app
        /// </summary>
        /// <remarks>We may want to set server limits here such as max request size</remarks>
        public void Configure(IApplicationBuilder app)
        {
            app.UseStaticFiles();
            app.Use(ProcessRequest);
        }

        /// <summary>
        /// This is the main entry point for this framework
        /// </summary>
        public static void Run(string[] args)
        {
            var host = Host
                .CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(w => w.UseStartup<Website>());
            host.Build().Run();
        }
    }
}
