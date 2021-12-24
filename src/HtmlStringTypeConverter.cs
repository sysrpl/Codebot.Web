namespace Codebot.Web;

using System;
using System.ComponentModel;
using System.Globalization;

public class HtmlStringTypeConverter : TypeConverter
{
    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
    {
        if (destinationType == typeof(string))
            return true;
        return base.CanConvertTo(context, destinationType);
    }

    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
    {
        if (sourceType == typeof(string))
            return true;
        return base.CanConvertFrom(context, sourceType);
    }

    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture,
        object value, Type destinationType)
    {
        HtmlString s = (HtmlString)value;
        if (destinationType == typeof(string))
            return s.ToString();
        return base.ConvertTo(context, culture, value, destinationType);
    }

    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture,
        object value)
    {
        if (value is not string)
            return base.ConvertFrom(context, culture, value);
        return new HtmlString(value.ToString());
    }
}
