using System;
using System.ComponentModel;

namespace Codebot
{
    public static class Converter
    {
        public static TResult Convert<TSource, TResult>(TSource value)
        {
            if (typeof(TSource) == typeof(TResult))
                return (TResult)(object)value;
            TypeConverter c = TypeDescriptor.GetConverter(typeof(TResult));
            if (c.CanConvertFrom(typeof(TSource)))
                return (TResult)c.ConvertFrom(value);
            c = TypeDescriptor.GetConverter(typeof(TSource));
            return (TResult)c.ConvertTo(value, typeof(TResult));
        }

        public static bool TryConvert<TSource, TResult>(TSource value, out TResult result)
        {
            if (typeof(TSource) == typeof(TResult))
            {
                result = (TResult)(object)value;
                return true;
            }
            result = default;
            try
            {
                TypeConverter c = TypeDescriptor.GetConverter(typeof(TResult));
                if (c.CanConvertFrom(typeof(TSource)))
                {
                    result = (TResult)c.ConvertFrom(value);
                    return true;
                }
                c = TypeDescriptor.GetConverter(typeof(TSource));
                if (c.CanConvertTo(typeof(TResult)))
                {
                    result = (TResult)c.ConvertTo(value, typeof(TResult));
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}