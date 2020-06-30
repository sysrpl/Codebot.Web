using System;
using Microsoft.AspNetCore.Http;

namespace Codebot.Web
{
    public class ContextEventArgs
    {
        public HttpContext Context { get; set; }
        public bool Handled { get; set; }
        public IHttpHandler Handler { get; set; }
        public Exception Error { get; set; }

        public ContextEventArgs(HttpContext context, Exception error = null)
        {
            Context = context;
            Error = error;
            Handler = null;
            Handled = false;
        }
    }
}
