namespace Codebot.Web;

using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

public class BasicUser : ClaimsPrincipal, IUser, IIdentity
{
    private static BasicUser anonymous;

    public static BasicUser Anonymous
    {
        get => anonymous;
        set
        {
            if (anonymous is null)
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

    public bool Login(HttpContext context, IUserSecurity security, string name, string password, string salt = "")
    {
        IUser user;
        lock (Anonymous)
            user = security.Users.FirstOrDefault(u => u.Name == name);
        if (user is null)
        {
            Security.DeleteCredentials(context);
            return false;
        }
        if (!user.Active || user.Hash != Security.ComputeHash(password))
        {
            Security.DeleteCredentials(context);
            return false;
        }
        Security.WriteCredentials(context, user, salt);
        return true;
    }

    public void Logout(HttpContext context, IUserSecurity security) => Security.DeleteCredentials(context);

    public IUser Restore(HttpContext context, IUserSecurity security, string salt = "")
    {
        IUser user = null;
        var name = Security.ReadUserName(context);
        lock (Anonymous)
            user = security.Users.FirstOrDefault(u => u.Name == name);
        if (user is null)
            return Anonymous;
        return Security.Match(context, user, salt) ? user : Anonymous;
    }

    public override bool IsInRole(string role) => roles.IndexOf(role.ToLower()) > -1;

    public bool IsAdmin { get => !IsAnonymous && IsInRole("admin"); }

    public bool IsAnonymous { get => this == Anonymous; }

    public string AuthenticationType { get => "custom"; }

    public bool IsAuthenticated { get => this != Anonymous; }
}
