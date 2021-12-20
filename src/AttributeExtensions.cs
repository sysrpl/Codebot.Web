namespace Codebot.Web;

using System;
using System.Reflection;

public static class AttributeExtensions
{
    public static T GetCustomAttribute<T>(this MemberInfo memberInfo, bool inherit = false) where T : Attribute
    {
        return (T)Attribute.GetCustomAttribute(memberInfo, typeof(T), inherit);
    }

    public static T[] GetCustomAttributes<T>(this MemberInfo memberInfo, bool inherit = false) where T : Attribute
    {
        return (T[])Attribute.GetCustomAttributes(memberInfo, typeof(T), inherit);
    }
}
