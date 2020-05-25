using Microsoft.AspNetCore.Http;

namespace Codebot.Web
{
    public interface IHttpHandler
    {
        void ProcessRequest(HttpContext context);
    }
}
