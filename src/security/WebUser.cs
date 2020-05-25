using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Text.RegularExpressions;

namespace Codebot.Web
{
    public class WebUser : ClaimsPrincipal, IWebUser, IIdentity
	{
		private static IWebUser anonymous;

		public static IWebUser Anonymous 
		{ 
			get => anonymous;
            set { if (anonymous == null) anonymous = value; }
		}

		private List<string> roles;

		public WebUser()
		{
			Active = true;
			Data = null;
			Name = string.Empty;
			Hash = string.Empty;
			roles = new List<string>();
		}

		public bool Active { get; set; }
		public object Data { get; set; }
		public string Name { get; set; }
		public string Hash { get; set; }

		public string Roles
		{ 
			get => string.Join(",", roles);
			set
			{
				var values = string.IsNullOrWhiteSpace(value) ? "" : Regex.Replace(value, @"\s+", "").ToLower();
				roles.Clear();
				roles.AddRange(values.Split(','));
			}
		}

		public bool Login(IUserSecurity security, string name, string password, string salt)
		{
			IWebUser user;
			lock (Anonymous)
				user = security.Users.FirstOrDefault(u => u.Name == name);
			if (user == null)
			{
				Security.DeleteCredentials(security.Context);
				return false;
			}
			if (!user.Active || user.Hash != Security.ComputeHash(password))
			{
				Security.DeleteCredentials(security.Context);
				return false;
			}
			Security.WriteCredentials(security.Context, user, salt);
			return true;
		}

		public void Logout(IUserSecurity security) => Security.DeleteCredentials(security.Context);

		public IWebUser Restore(IUserSecurity security, string salt)
		{
			IWebUser user = null;
			var name = Security.ReadUserName(security.Context);
			var credentials = Security.ReadCredentials(security.Context);
			lock (Anonymous)
				user = security.Users.FirstOrDefault(u => u.Name == name);
			if (user == null)
				return Anonymous;
			return Security.Match(user, salt, credentials) ? user : Anonymous;
		}

		public override bool IsInRole(string role) => roles.IndexOf(role.ToLower()) > -1;

		public bool IsAdmin { get => IsInRole("admin"); }

		public bool IsAnonymous { get => this == Anonymous; }

		public string AuthenticationType { get => "custom"; }

		public bool IsAuthenticated { get => this != Anonymous; }
	}
}
