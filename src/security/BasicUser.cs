namespace Codebot.Web;

using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

public class BasicUser : ClaimsPrincipal, IUser, IIdentity
{
    static BasicUser anonymous;

    public static BasicUser Anonymous
    {
        get => anonymous;
        set
        {
            if (anonymous is null) anonymous = value;
        }
    }

    readonly List<string> roles = [];
    string name = string.Empty;

    public bool Active { get; set; } = true;
    public object Data { get; set; } = null;
    public string Name { get => name; set { if (string.IsNullOrWhiteSpace(name)) name = value; } }
    public string Hash { get; set; } = string.Empty;

    public string Roles
    {
        get => string.Join(",", roles);
        set
        {
            if (IsAnonymous)
                return;
            var values = string.IsNullOrWhiteSpace(value) ? "" : Regex.Replace(value, @"\s+", "").ToLower();
            roles.Clear();
            roles.AddRange(values.Split(',',StringSplitOptions.RemoveEmptyEntries).Select(r => r.Trim()));
        }
    }

    public bool Login(HttpContext context, IUserSecurity security, string name, string password, string salt = "")
    {
        IUser user;
        lock (Anonymous)
            user = security.Users.FirstOrDefault(u => u.Name == name);
        if (user is null || !user.Active || user.Hash != Security.ComputeHash(password))
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
        if (user is null || !user.Active)
            return Anonymous;
        return Security.Match(context, user, salt) ? user : Anonymous;
    }

    public override bool IsInRole(string role) => roles.IndexOf(role.ToLower()) > -1;

    public bool IsAdmin { get => !IsAnonymous && IsInRole("admin"); }

    public bool IsAnonymous { get => this == Anonymous; }

    public string AuthenticationType { get => "custom"; }

    public bool IsAuthenticated { get => this != Anonymous; }
}
