using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Text.RegularExpressions;

namespace Codebot.Web
{
    public enum BasicUserKind
    {
        Anonymous = 0,
        Guest = 1,
        User = 2,
        Moderator = 3,
        Administrator = 4
    }

    public class BasicUser : ClaimsPrincipal, IUser, IIdentity
    {
        private static BasicUser anonymous;

        public static BasicUser Anonymous
        {
            get => anonymous;
            set { if (anonymous == null) anonymous = value; }
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

        public IUser Restore(IUserSecurity security, string salt)
        {
            IUser user = null;
            var name = Security.ReadUserName(security.Context);
            var credentials = Security.ReadCredentials(security.Context);
            lock (Anonymous)
                user = security.Users.FirstOrDefault(u => u.Name == name);
            if (user == null)
                return Anonymous;
            return Security.Match(user, salt, credentials) ? user : Anonymous;
        }

        public BasicUserKind Kind
        {
            get
            {
                if (IsAnonymous)
                    return BasicUserKind.Anonymous;
                if (IsAdmin)
                    return BasicUserKind.Administrator;
                if (IsModerator)
                    return BasicUserKind.Moderator;
                if (IsUser)
                    return BasicUserKind.User;
                return BasicUserKind.Guest;
            }
        }

        public override bool IsInRole(string role) => roles.IndexOf(role.ToLower()) > -1;

        public bool IsAdmin { get => IsInRole("admin"); }

        public bool IsModerator { get => IsInRole("moderator"); }

        public bool IsUser { get => IsInRole("user"); }

        public bool IsGuest { get => IsAdmin || IsInRole("guest"); }

        public bool IsAnonymous { get => this == Anonymous; }

        public string AuthenticationType { get => "custom"; }

        public bool IsAuthenticated { get => this != Anonymous; }
    }
}
