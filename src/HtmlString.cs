using System;
using System.Web;
using System.ComponentModel;

namespace Codebot.Web
{
    [TypeConverter(typeof(HtmlStringTypeConverter)), Serializable()]
    public sealed class HtmlString
    {
        private string data;

        public HtmlString(string s)
        {
            data = HttpUtility.HtmlEncode(s);
        }

        public static implicit operator string(HtmlString value)
        {
            return value.data;
        }

        public static explicit operator HtmlString(string value)
        {
            return new HtmlString(value);
        }

        public override string ToString()
        {
            return data;
        }
    }
}
