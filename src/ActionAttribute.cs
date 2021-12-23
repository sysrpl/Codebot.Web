namespace Codebot.Web;

using System;

[AttributeUsage(AttributeTargets.Method)]
public class ActionAttribute : AuthorizeAttribute
{
    public ActionAttribute(string actionName)
    {
        ContentType = "text/html; charset=utf-8";
        ActionName = actionName;
    }

    public string ActionName { get; set; }
}
