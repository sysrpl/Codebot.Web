﻿#pragma warning disable RECS0060 // Warns when a culture-aware 'IndexOf' call is used by default.
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
        private static string webroot;

        /// <summary>
        /// Called once at application startup
        /// </summary>
        public static void Configure(IHttpContextAccessor state)
        {
            accessor = state;
            key = new object();
            webroot = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        }

        /// <summary>
        /// Called every time a BasicHandler is created
        /// </summary>
        public static void Attach(BasicHandler handler)
        {
            Context.Items.Add(key, handler);
        }

        public static HttpContext Context { get => accessor.HttpContext; }
        public static BasicHandler Handler { get => Context.Items[key] as BasicHandler; }
        public static string Path { get => Context.Request.Path.Value; }

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
