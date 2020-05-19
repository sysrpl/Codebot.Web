using System;
using System.Collections.Generic;

namespace Codebot.Web
{
    [AttributeUsage(AttributeTargets.Class,  AllowMultiple = true)]
    public class TemplateAttribute : Attribute
    {
        public static string TemplateFolder = "/templates/";
        public static string TemplateExtension = ".template";

        public TemplateAttribute(params string[] names)
        {
            List<Template> items = new List<Template>();
            foreach (var n in names)
                items.Add(new Template(n));
            Items = items.ToArray();
        }

        public Template[] Items { get; private set; }
    }

    public class Template
    {
        public Template(string name)
        {
            Name = name;
            Resource = TemplateAttribute.TemplateFolder + name + TemplateAttribute.TemplateExtension;
        }

        public string Name { get; private set; }
        public string Resource { get; private set; }

        public static string FileName(string templateName)
        {
            return WebState.MapPath(TemplateAttribute.TemplateFolder + templateName + TemplateAttribute.TemplateExtension);
        }
    }
}
