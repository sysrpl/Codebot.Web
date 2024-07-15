namespace Codebot.Web;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class FileUserPage<TUser> : PageHandler where TUser : BasicUser
{
    public TUser User { get { return Context.User as TUser; } }

    public override bool IsAdmin
    {
        get => User.IsAdmin;
    }

    public override bool IsAuthenticated
    {
        get => !User.IsAnonymous;
    }

    protected string UserReadFile(string user, string fileName, string empty = "")
    {
        fileName = App.AppPath($"private/data/{user}/{fileName}");
        return File.Exists(fileName) ? App.Read(fileName).Trim() : empty;
    }

    protected void UserWriteFile(string user, string fileName, string content)
    {
        fileName = App.AppPath($"private/data/{user}/{fileName}");
        Directory.CreateDirectory(Path.GetDirectoryName(fileName));
        App.Write(fileName, content);
    }

    [Action("login")]
    public void LoginAction()
    {
        var security = App.Security;
        var name = ReadAny("name", "username", "login");
        var password = Read("password");
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(password))
        {
            Write("FAIL");
            return;
        }
        var success = User.Login(Context, security, name, password, App.UserAgent);
        if (success && ReadBool("redirect"))
            Redirect(".");
        else
            Write(success ? "OK" : "FAIL");
    }

    [Action("logout")]
    public void LogoutAction()
    {
        var security = App.Security;
        User.Logout(Context, security);
        if (ReadBool("redirect"))
            Redirect(".");
        else
            Write("OK");
    }

    [Action("users", Allow = "admin")]
    public void UsersAction()
    {
        var users = new List<string>() { User.Name };
        if (User.IsAdmin)
        {
            var security = App.Security;
            lock (BasicUser.Anonymous)
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

public class BasicUserPage : FileUserPage<BasicUser>
{
}
