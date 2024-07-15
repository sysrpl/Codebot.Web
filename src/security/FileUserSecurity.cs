namespace Codebot.Web;

using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using Codebot.Xml;
using Microsoft.AspNetCore.Http;

public class FileUserSecurity<TUser> : IUserSecurity where TUser : BasicUser, new()
{
    public delegate IWriter GenerateUser();

    protected virtual void GenerateDefaultUsers(GenerateUser generate)
    {
        var filer = generate();
        filer.WriteBool("active", true);
        filer.Write("name", "admin");
        filer.Write("hash", Hasher("admin"));
        filer.Write("roles", "admin,user");
    }

    protected virtual void ReadUser(IReader filer, TUser user)
    {
        user.Active = filer.ReadBool("active");
        user.Name = filer.ReadString("name");
        user.Hash = filer.ReadString("hash");
        user.Roles = filer.ReadString("roles");
    }

    protected virtual void WriteUser(IWriter filer, TUser user)
    {
        filer.WriteBool("active", user.Active);
        filer.Write("name", user.Name);
        filer.Write("hash", user.Hash);
        filer.Write("roles", user.Roles);
    }

    protected virtual TUser CreateUser(Dictionary<string, string> args)
    {
        var name = args["name"].Trim();
        var password = args["password"].Trim();
        var roles = args["roles"].Trim();
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(password))
            return null;
        name = name.Trim();
        if (!NameCheck.IsValidUserName(name))
            return null;
        var lowerName = name.ToLower();
        password = password.Trim();
        if (!NameCheck.IsValidPassword(password))
            return null;
        var hash = Hasher(password);
        lock (BasicUser.Anonymous)
        {
            var user = Users.Find(u => u.Name.ToLower() == lowerName);
            if (user is not null)
                return null;
        }
        return new TUser() { Active = true, Name = name, Hash = hash, Roles = roles };
    }

    public static TUser CurrentUser
    {
        get => App.Context.User as TUser;
    }

    protected static string Hasher(string value) => Security.ComputeHash(value);

    private const string securityFile = "private/users.xml";

    private static readonly List<TUser> users = new();

    public List<TUser> Users { get => users; }

    public bool AddUser(Dictionary<string, string> args)
    {
        Start();
        var user = CreateUser(args);
        if (user is null)
            return false;
        var doc = new Document();
        var fileName = App.AppPath(securityFile);
        lock (BasicUser.Anonymous)
        {
            doc.Load(fileName);
            var filer = doc.Force("security/users").Nodes.Add("user").Filer;
            WriteUser(filer, user);
            doc.Save(fileName, true);
            Users.Add(user);
        }
        return true;
    }

    public bool AddUser(string name, string password, string roles = "user")
    {
        Start();
        var args = new Dictionary<string, string>()
        {
            {"name", name},
            {"password", password},
            {"roles", roles}
        };
        return AddUser(args);
    }

    public bool ModifyUser(TUser user)
    {
        Start();
        if (user.IsAnonymous)
            return false;
        lock (BasicUser.Anonymous)
        {
            var doc = new Document();
            var fileName = App.AppPath(securityFile);
            doc.Load(fileName);
            var node = doc.Root.FindNode($"users/user[name='{user.Name}']");
            if (node is null)
                return false;
            var filer = node.Filer;
            WriteUser(filer, user);
            doc.Save(fileName, true);
        }
        return true;
    }

    public bool FindUser(string name)
    {
        return FindUser(name, out _);
    }

    public bool FindUser(string name, out TUser user)
    {
        Start();
        lock (BasicUser.Anonymous)
        {
            var lowerName = name.ToLower();
            user = Users.Find(u => u.Name.ToLower() == lowerName);
            return user is not null;
        }
    }

    public bool DeleteUser(TUser user)
    {
        Start();
        if (user.IsAnonymous)
            return false;
        lock (BasicUser.Anonymous)
        {
            Users.Remove(user);
            var doc = new Document();
            var fileName = App.AppPath(securityFile);
            doc.Load(fileName);
            var node = doc.Root.FindNode($"users/user[name='{user.Name}']");
            if (node is null)
                return false;
            node.Parent.Nodes.Remove(node);
            doc.Save(fileName, true);
        }
        return true;
    }

    private static void CreateConfig(Document doc)
    {
        var filer = doc.Force("security").Filer;
        var secret = filer.ReadString("secret");
        if (string.IsNullOrWhiteSpace(secret))
        {
            secret = Security.RandomSecretKey(32);
            filer.WriteString("secret", secret);
        }
        Security.SecretKey(secret);
    }

    private void CreateUsers(Document doc)
    {
        var nodes = doc.Force("security/users").Nodes;
        if (nodes.Count == 0)
            GenerateDefaultUsers(() => nodes.Add("user").Filer);
        foreach (var node in nodes)
        {
            if (node.Name != "user")
                continue;
            var filer = node.Filer;
            var user = new TUser();
            ReadUser(filer, user);
            if (!user.Active)
                continue;
            Users.Add(user);
        }
    }

    protected bool Started { get; private set; }

    public virtual void Start()
    {
        if (Started)
            return;
        Started = true;
        BasicUser.Anonymous = new TUser() { Active = false, Name = "anonymous" };
        var fileName = App.AppPath(securityFile);
        var doc = new Document();
        var fileInfo = new FileInfo(fileName);
        if (fileInfo.Exists)
            doc.Load(fileName);
        else
            Directory.CreateDirectory(fileInfo.Directory.FullName);
        var state = doc.ToString();
        CreateConfig(doc);
        CreateUsers(doc);
        if (state != doc.ToString())
            doc.Save(fileName, true);
    }

    public virtual void Stop()
    {
    }

    public virtual void RestoreUser(HttpContext context)
    {
        Start();
        context.User = BasicUser.Anonymous.Restore(context, this) as ClaimsPrincipal;
    }

    IEnumerable<IUser> IUserSecurity.Users { get => Users; }
}

public class BasicUserSecurity : FileUserSecurity<BasicUser> { }
