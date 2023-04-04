namespace Codebot.Web;

using System;
using System.Collections.Generic;

[AttributeUsage(AttributeTargets.Class,  AllowMultiple = true)]
public sealed class TemplateAttribute : Attribute
{
    public static string TemplateFolder = "/templates/";
    public static string TemplateExtension = ".template";

    public TemplateAttribute(params string[] names)
    {
        var items = new List<Template>();
        foreach (var n in names)
            items.Add(new Template(n));
        Items = items.ToArray();
    }

    public Template[] Items { get; }
}

public sealed class Template
{
    public Template(string name)
    {
        Name = name;
        Resource = TemplateAttribute.TemplateFolder + name + TemplateAttribute.TemplateExtension;
    }

    public string Name { get; }
    public string Resource { get; }

    public static string FileName(string templateName)
    {
        return App.MapPath(TemplateAttribute.TemplateFolder + templateName + TemplateAttribute.TemplateExtension);
    }
}
