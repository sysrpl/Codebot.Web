using System.Security.Principal;
using System.Web;
using Microsoft.AspNetCore.Http;

namespace Codebot.Web
{
	public interface IUser : IPrincipal
	{
		bool Active { get; }
		string Name { get; }
		string Hash { get; }
		bool IsAdmin { get; }
		bool IsAnonymous { get; }
		bool Login(IUserSecurity security, string name, string password, string salt);
		void Logout(IUserSecurity security);
		IUser Restore(IUserSecurity security, string salt);
	}
}
