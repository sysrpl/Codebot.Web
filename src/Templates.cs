namespace Codebot.Web
{
    public class Templates
    {
        public static string TemplateFolder = "/templates/";
        public static string TemplateExtension = ".template";

        public delegate string TemplateLoad(string fileName);
        private readonly TemplateLoad load;

        public Templates(TemplateLoad load)
        {
            this.load = load;
        }

        public string this[string key]
        {
            get
            {
                return load(TemplateFolder + key + TemplateExtension);
            }
        }
    }
}
