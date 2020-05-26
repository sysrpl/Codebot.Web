#pragma warning disable RECS0060 // Warns when a culture-aware 'IndexOf' call is used by default.
#pragma warning disable RECS0063 // Warns when a culture-aware 'StartsWith' call is used by default.

using System.IO;
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
            webroot = System.IO.Path.Combine(approot, "wwwroot");
        }

        /// <summary>
        /// Called every time a BasicHandler is created
        /// </summary>
        public static void Attach(BasicHandler handler)
        {
            Context.Items.Add(key, handler);
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
        /// The current user agent
        /// </summary>
        public static string UserAgent { get => Context.Request.Headers["User-Agent"].ToString(); }

        /// <summary>
        /// The current requested path
        /// </summary>
        public static string Path { get => Context.Request.Path.Value; }

        /// <summary>
        /// Map a path to application file path
        /// </summary>
        public static string AppPath(string path)
        {
            return string.IsNullOrEmpty(path) ? approot : System.IO.Path.Combine(approot, path);
        }

        /// <summary>
        /// Map a web request path to a physical file path 
        /// </summary>
        public static string MapPath(string path)
        {
            // TODO ponder blocking ".." in path for security reasons
            if (path.StartsWith("/"))
            {
                path = path.Substring(1);
                return string.IsNullOrEmpty(path) ? webroot : System.IO.Path.Combine(webroot, path);
            }
            var root = Path;
            if (root.StartsWith("/"))
                root = root.Substring(1);
            root = string.IsNullOrEmpty(root) ? webroot : System.IO.Path.Combine(webroot, root);
            return string.IsNullOrEmpty(path) ? root : System.IO.Path.Combine(root, path);
        }
    }
}
