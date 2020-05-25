using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Codebot.Web
{
	public class FileUserPage<TUser> : PageHandler where TUser : WebUser
	{
		public TUser User { get { return Context.User as TUser; } }

		public override bool IsAdmin
		{
			get
			{
				return User.IsAdmin;
			}
		}

		public override bool IsAuthenticated
		{
			get
			{
				return !User.IsAnonymous;
			}
		}

		protected string UserReadFile(string user, string fileName, string empty = "")
		{
			fileName =  WebState.AppPath($"private/data/{user}/{fileName}");
			return File.Exists(fileName)
				? Website.FileRead(fileName).Trim()
				: empty;
		}

		protected void UserWriteFile(string user, string fileName, string content)
		{
			fileName = WebState.AppPath($"/private/data/{user}/{fileName}");
			Directory.CreateDirectory(Path.GetDirectoryName(fileName));
            Website.FileWrite(fileName, content);
		}

		[Action("login")]
		public void LoginAction()
		{
            var security = Website.Security;
            var name = ReadAny("name", "username", "login");
			var password = Read("password");
			if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(password))
			{
				Write("FAIL");
				return;
			}
			var success = User.Login(security, name, password, WebState.UserAgent);
			var redirect = Read("redirect");
			if (redirect == "true")
				Redirect("/");
			else
				Write(success ? "OK" : "FAIL");
		}

		[Action("logout")]
		public void LogoutAction()
		{
			var security = Website.Security;
			User.Logout(security);
			var redirect = Read("redirect");
			if (redirect == "true")
                Redirect("/");
			else
				Write("OK");
		}

		[Action("users", Allow = "admin")]
		public void UsersAction()
		{
			var users = new List<string>() { User.Name };
			if (User.IsAdmin)
			{
                var security = Website.Security;
                lock (WebUser.Anonymous)
				{
					var names = security
						.Users
						.Select(user => user.Name)
						.Where(name => name != User.Name)
						.OrderBy(name => name);
					users.AddRange(names);
				}
			}
			var list = string.Join(", ", users.Select(name => $"\"{name}\""));
			Write($"[ {list} ]");
		}
	}

    public class BasicUserPage : FileUserPage<WebUser>
    {
    }
}
