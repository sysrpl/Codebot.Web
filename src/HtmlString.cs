namespace Codebot.Web;

using System;
using System.Web;
using System.ComponentModel;

[TypeConverter(typeof(HtmlStringTypeConverter)), Serializable()]
public sealed class HtmlString
{
    private readonly string data;

    public HtmlString(string s) => data = HttpUtility.HtmlEncode(s);
    public static implicit operator string(HtmlString value) => value.data;
    public static explicit operator HtmlString(string value) => new HtmlString(value);
    public override string ToString() => data;
}
