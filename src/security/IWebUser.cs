﻿using System.Security.Principal;
using System.Web;

namespace Codebot.Web
{
	public interface IWebUser : IPrincipal
	{
		bool Active { get; }
		string Name { get; }
		string Hash { get; }
		bool IsAdmin { get; }
		bool IsAnonymous { get; }
		bool Login(IUserSecurity security, string name, string password, string salt);
		void Logout(IUserSecurity security);
		IWebUser Restore(IUserSecurity security, string salt);
	}
}
