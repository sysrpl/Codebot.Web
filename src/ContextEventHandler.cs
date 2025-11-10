namespace Codebot.Web;

using System;
using Microsoft.AspNetCore.Http;

public class ContextEventArgs
{
    public HttpContext Context { get; set; }
    public bool Handled { get; set; }
    public IHttpHandler Handler { get; set; }
    public Exception Error { get; set; }
    public Task Task { get; set; }

    public ContextEventArgs(HttpContext context, Exception error = null)
    {
        Context = context;
        Error = error;
        Handler = null;
        Handled = false;
        Task = null;
    }
}
