namespace Codebot.Web;

using System;

[AttributeUsage(AttributeTargets.Method)]
public class ActionAttribute : Attribute
{
    public ActionAttribute(string actionName)
    {
        ContentType = "text/html; charset=utf-8";
        ActionName = actionName;
    }

    public string ContentType { get; set; }
    public string ActionName { get; set; }
    public string Allow { get; set; }
    public string Deny { get; set; }
}
