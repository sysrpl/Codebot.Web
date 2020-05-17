using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System.IO;

namespace Codebot.Web
{
    public sealed class Website
    {
        private async Task ProcessRequest(HttpContext ctx, Func<Task> next)
        {
            var handled = false;
            var s = BasicHandler.MapPath(ctx.Request.Path.Value);
            if (Directory.Exists(s))
            {
                s = Path.Combine(s, "home.ashx");
                if (File.Exists(s))
                {
                    s = File.ReadAllText(s).Trim();
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
