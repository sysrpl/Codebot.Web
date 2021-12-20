namespace Codebot.Web;

using System;
using System.IO;
using Microsoft.AspNetCore.Http;

public static class WebObjectExtensions
{
	public static string ReadBody(this HttpRequest request)
	{
		using StreamReader reader = new StreamReader(request.Body);
		return reader.ReadToEnd();
	}

	public static void DeleteCookie(this HttpContext context, string key)
	{
		if (context.Request.Cookies[key] != null)
			context.Response.Cookies.Delete(key);
	}

	public static string ReadCookie(this HttpContext context, string key, string defaultValue = "")
	{
		var cookie = context.Request.Cookies[key];
		return string.IsNullOrEmpty(cookie) ? defaultValue : cookie;
	}

	public static void WriteCookie(this HttpContext context, string key, string value, DateTime? expires = null)
	{
		CookieOptions option = new CookieOptions();
		if (expires.HasValue)
			option.Expires = expires;
		else
			option.Expires = DateTime.Now.AddYears(5);
		context.Response.Cookies.Append(key, value, option);
	}
}
