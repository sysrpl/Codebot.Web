namespace Codebot.Web;

using System;

[AttributeUsage(AttributeTargets.Method)]
public class AuthorizeAttribute : Attribute
{
    public string ContentType { get; set; }
    public string Allow { get; set; }
    public string Deny { get; set; }
}
