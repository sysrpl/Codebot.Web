﻿namespace Codebot.Web;

using System.Security.Principal;

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
