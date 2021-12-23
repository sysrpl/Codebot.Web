namespace Codebot.Web;

using System;

[AttributeUsage(AttributeTargets.Class)]
public class PageTypeAttribute : AuthorizeAttribute
{
    public PageTypeAttribute(string fileName)
    {
        ContentType = "text/html; charset=utf-8";
        FileName = fileName;
        IsTemplate = false;
    }

    public string FileName { get; set; }
    public bool IsTemplate { get; set; }
}
