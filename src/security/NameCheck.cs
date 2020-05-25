#pragma warning disable RECS0060 // Warns when a culture-aware 'IndexOf' call is used by default.
#pragma warning disable RECS0063 // Warns when a culture-aware 'StartsWith' call is used by default.

using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Codebot.Web
{
	public static class NameCheck
	{
		public static bool IsValidUserName(string userName)
		{
			if (userName == null)
				return false;
			userName = userName.ToLower();
			if (userName == "anonymous")
				return false;
			if (userName.StartsWith("admin"))
				return false;
			var length = userName.Length;
			if (length < 5 || length > 32)
				return false;
			if (!IsAlpha(userName[0]))
				return false;
			if (!IsAlphanumeric(userName[length - 1]))
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
			if (password == null)
				return false;
			var length = password.Length;
			if (length < 5 || length > 32)
				return false;
			var alphaCount = 0;
			foreach (var c in password)
				if (c <= ' ')
					return false;
				else if (c > '~')
					return false;
				else if (IsAlpha(c))
					alphaCount++;
			return alphaCount > 0;
		}

		private static bool IsAlpha(char c)
		{
			return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
		}

		private static bool IsAlphanumeric(char c)
		{
			return IsAlpha(c) || (c >= '0' && c <= '9');
		}
	}
}
