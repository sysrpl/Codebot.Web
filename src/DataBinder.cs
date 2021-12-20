namespace Codebot.Web;

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

public static class DataBinder
{
    internal static string FormatResult(object result, string format)
    {
        if (result == null)
            return string.Empty;

        if (string.IsNullOrEmpty(format))
            return result.ToString();

        return string.Format(format, result);
    }

    public static object Eval(object container, string expression)
    {
        expression = expression?.Trim();
        if (string.IsNullOrEmpty(expression))
            throw new ArgumentNullException(nameof(expression));

        object current = container;
        while (current != null)
        {
            int dot = expression.IndexOf('.');
            int size = (dot == -1) ? expression.Length : dot;
            string prop = expression.Substring(0, size);

            if (prop.IndexOf('[') != -1)
                current = GetIndexedPropertyValue(current, prop);
            else
                current = GetPropertyValue(current, prop);

            if (dot == -1)
                break;

            expression = expression.Substring(prop.Length + 1);
        }

        return current;
    }

    public static string Eval(object container, string expression, string format)
    {
        object result = Eval(container, expression);
        return FormatResult(result, format);
    }

    public static object GetIndexedPropertyValue(object container, string expr)
    {
        if (container == null)
            throw new ArgumentNullException(nameof(container));
        if (string.IsNullOrEmpty(expr))
            throw new ArgumentNullException(nameof(expr));

        int openIdx = expr.IndexOf('[');
        int closeIdx = expr.IndexOf(']'); // see the test case. MS ignores all after the first ]
        if (openIdx < 0 || closeIdx < 0 || closeIdx - openIdx <= 1)
            throw new ArgumentException(expr + " is not a valid indexed expression.");

        string val = expr.Substring(openIdx + 1, closeIdx - openIdx - 1);
        val = val.Trim();
        if (val.Length == 0)
            throw new ArgumentException(expr + " is not a valid indexed expression.");

        bool isString = false;
        // a quoted val means we have a string
        if ((val[0] == '\'' && val[^1] == '\'') ||
            (val[0] == '\"' && val[^1] == '\"'))
        {
            isString = true;
            val = val[1..^1];
        }
        else
        {
            // if all chars are digits, then we have a int
            for (int i = 0; i < val.Length; i++)
                if (!Char.IsDigit(val[i]))
                {
                    isString = true;
                    break;
                }
        }

        int intVal = 0;
        if (!isString)
        {
            try
            {
                intVal = int.Parse(val);
            }
            catch
            {
                throw new ArgumentException(expr + " is not a valid indexed expression.");
            }
        }

        string property;
        if (openIdx > 0)
        {
            property = expr.Substring(0, openIdx);
            if (!string.IsNullOrEmpty(property))
                container = GetPropertyValue(container, property);
        }

        if (container == null)
            return null;

        if (container is IList list)
        {
            if (isString)
                throw new ArgumentException(expr + " cannot be indexed with a string.");
            return list[intVal];
        }

        Type t = container.GetType();

        // MS does not seem to look for any other than "Item"!!!
        object[] attrs = t.GetCustomAttributes(typeof(DefaultMemberAttribute), false);
        if (attrs.Length != 1)
            property = "Item";
        else
            property = ((DefaultMemberAttribute)attrs[0]).MemberName;

        Type[] argTypes = { isString ? typeof(string) : typeof(int) };
        PropertyInfo prop = t.GetProperty(property, argTypes);
        if (prop == null)
            throw new ArgumentException(expr + " indexer not found.");
        object[] args = new object[1];
        if (isString)
            args[0] = val;
        else
            args[0] = intVal;

        return prop.GetValue(container, args);
    }

    public static string GetIndexedPropertyValue(object container, string propName, string format)
    {
        object result = GetIndexedPropertyValue(container, propName);
        return FormatResult(result, format);
    }

    public static object GetPropertyValue(object container, string propName)
    {
        if (container == null)
            throw new ArgumentNullException(nameof(container));
        if (string.IsNullOrEmpty(propName))
            throw new ArgumentNullException(nameof(propName));

        PropertyDescriptor prop = TypeDescriptor.GetProperties(container).Find(propName, true);
        if (prop == null)
        {
            throw new Exception($"Property {propName} not found in {container.GetType()}");
        }
        return prop.GetValue(container);
    }

    public static string GetPropertyValue(object container, string propName, string format)
    {
        object result = GetPropertyValue(container, propName);
        return FormatResult(result, format);
    }

    [ThreadStatic]
    private static Dictionary<Type, PropertyInfo> dataItemCache;

    public static object GetDataItem(object container, out bool foundDataItem)
    {
        foundDataItem = false;
        if (container == null)
            return null;
        if (dataItemCache == null)
            dataItemCache = new Dictionary<Type, PropertyInfo>();
        Type type = container.GetType();
        if (!dataItemCache.TryGetValue(type, out PropertyInfo pi))
        {
            pi = type.GetProperty("DataItem", BindingFlags.Public | BindingFlags.Instance);
            dataItemCache[type] = pi;
        }
        if (pi == null)
            return null;
        foundDataItem = true;
        return pi.GetValue(container, null);
    }

    public static object GetDataItem(object container)
    {
        return GetDataItem(container, out bool _);
    }
}
