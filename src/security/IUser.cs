namespace Codebot.Web;

using System.Security.Principal;
using Microsoft.AspNetCore.Http;

public interface IUser : IPrincipal
{
	bool Active { get; }
	string Name { get; }
	string Hash { get; }
	bool IsAdmin { get; }
	bool IsAnonymous { get; }

	bool Login(HttpContext context, IUserSecurity security, string name, string password, string salt = "");
	void Logout(HttpContext context, IUserSecurity security);
	IUser Restore(HttpContext context, IUserSecurity security, string salt = "");
}
