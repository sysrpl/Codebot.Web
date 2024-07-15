#pragma warning disable RECS0060 // Warns when a culture-aware 'IndexOf' call is used by default.
#pragma warning disable RECS0063 // Warns when a culture-aware 'StartsWith' call is used by default.

namespace Codebot.Web;

using System;
using System.Linq;
using System.Text.RegularExpressions;

public static class NameCheck
{
	public delegate bool CustomCheck(string value);

	public static CustomCheck CustomCheckUserName { get; set; }
	public static CustomCheck CustomCheckPassword { get; set; }

	public static bool IsValidUserName(string userName)
	{
		if (string.IsNullOrWhiteSpace(userName))
			return false;
		if (CustomCheckUserName != null)
			return CustomCheckUserName(userName);
		userName = userName.ToLower();
		if (Security.Roles.Contains(userName))
			return false;
		var length = userName.Length;
		if (length < 4 || length > 32)
			return false;
		if (!IsAlpha(userName[0]))
			return false;
		if (!IsAlphanumeric(userName[length - 1]))
			return false;
		if (userName.Contains(' '))
			return false;
		if (userName.Contains('"'))
			return false;
		if (userName.Contains('\''))
			return false;
		if (!Regex.IsMatch(userName, "^[a-z0-9._-]*$"))
			return false;
		if (Regex.IsMatch(userName, "[0-9]{5,}"))
			return false;
		var punctuation = new[] { '.', '_', '-' };
		if (punctuation.Count(userName.Contains) > 1)
			return false;
		for (var i = 0; i < length - 1; i++)
			if (punctuation.Contains(userName[i]) && !IsAlphanumeric(userName[i + 1]))
				return false;
		return true;
	}

	public static bool IsValidPassword(string password)
	{
		if (string.IsNullOrWhiteSpace(password))
			return false;
		if (CustomCheckPassword != null)
			return CustomCheckPassword(password);
		var length = password.Length;
		if (length < 4 || length > 32)
			return false;
		var alphaCount = 0;
		foreach (var c in password)
		{
			if (c <= ' ')
				return false;
			if (c > '~')
				return false;
			if (IsAlpha(c))
				alphaCount++;
		}
		return alphaCount > 0;
	}

	private static bool IsAlpha(char c) => (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
	private static bool IsAlphanumeric(char c) => IsAlpha(c) || (c >= '0' && c <= '9');
}
