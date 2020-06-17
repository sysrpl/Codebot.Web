#pragma warning disable RECS0060 // Warns when a culture-aware 'IndexOf' call is used by default.
#pragma warning disable RECS0063 // Warns when a culture-aware 'StartsWith' call is used by default.

using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Codebot.Web
{
    /// <summary>
    /// The WebSate class provides access to the current context, handler,
    /// and path information
    /// </summary>
    public static class WebState
    {
        private static IHttpContextAccessor accessor;
        private static object key;
        private static string approot;
        private static string webroot;

        /// <summary>
        /// Called once at application startup
        /// </summary>
        public static void Configure(IHttpContextAccessor accessor)
        {
            WebState.accessor = accessor;
            key = new object();
            approot = Directory.GetCurrentDirectory();
            webroot = Path.Combine(approot, "wwwroot");
        }

        /// <summary>
        /// Attach asynchronously processes a handler
        /// </summary>
        public static async Task Attach(BasicHandler handler)
        {
            var c = Context;
            if (c.Items.ContainsKey(key))
                c.Items[key] = handler;
            else
                c.Items.Add(key, handler);
            await Task.Run(() => handler.ProcessRequest(c));
        }

        /// <summary>
        /// The current HttpContext
        /// </summary>
        public static HttpContext Context { get => accessor.HttpContext; }

        /// <summary>
        /// The current BasicHandler
        /// </summary>
        public static BasicHandler Handler { get => Context.Items[key] as BasicHandler; }

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
        /// Map a path to application file path
        /// </summary>
        public static string AppPath(string path) =>
            string.IsNullOrEmpty(path) ? approot : Path.Combine(approot, path);

        /// <summary>
        /// Map a web request path to a physical file path
        /// </summary>
        /// <remarks>If path is empty the return value is the requested path</remarks>
        public static string MapPath(string path)
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
    }
}
