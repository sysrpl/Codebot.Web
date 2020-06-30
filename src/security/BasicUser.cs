using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

namespace Codebot.Web
{
    public class BasicUser : ClaimsPrincipal, IUser, IIdentity
    {
        private static BasicUser anonymous;

        public static BasicUser Anonymous
        {
            get => anonymous;
            set
            {
                 if (anonymous == null)
                    anonymous = value;
            }
        }

        private readonly List<string> roles;

        public BasicUser()
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
                if (IsAnonymous)
                    return;
                var values = string.IsNullOrWhiteSpace(value) ? "" : Regex.Replace(value, @"\s+", "").ToLower();
                roles.Clear();
                roles.AddRange(values.Split(','));
            }
        }

        public bool Login(IUserSecurity security, string name, string password, string salt)
        {
            IUser user;
            lock (Anonymous)
                user = security.Users.FirstOrDefault(u => u.Name == name);
            if (user == null)
            {
                Security.DeleteCredentials(App.Context);
                return false;
            }
            if (!user.Active || user.Hash != Security.ComputeHash(password))
            {
                Security.DeleteCredentials(App.Context);
                return false;
            }
            Security.WriteCredentials(App.Context, user, salt);
            return true;
        }

        public void Logout(IUserSecurity security) => Security.DeleteCredentials(App.Context);

        public IUser Restore(IUserSecurity security, string salt)
        {
            IUser user = null;
            var name = Security.ReadUserName(App.Context);
            var credentials = Security.ReadCredentials(App.Context);
            lock (Anonymous)
                user = security.Users.FirstOrDefault(u => u.Name == name);
            if (user == null)
                return Anonymous;
            return Security.Match(user, salt, credentials) ? user : Anonymous;
        }

		public HttpContext Context { get; set; }

        public override bool IsInRole(string role) => roles.IndexOf(role.ToLower()) > -1;

        public bool IsAdmin { get => !IsAnonymous && IsInRole("admin"); }

        public bool IsAnonymous { get => this == Anonymous; }

        public string AuthenticationType { get => "custom"; }

        public bool IsAuthenticated { get => this != Anonymous; }
    }
}
