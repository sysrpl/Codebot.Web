namespace Codebot.Web;

using Microsoft.AspNetCore.Http;

public interface IHttpHandler
{
    void ProcessRequest(HttpContext context);
}
