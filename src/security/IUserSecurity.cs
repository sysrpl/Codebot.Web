namespace Codebot.Web;

using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

public interface IUserSecurity
{
	void Start();
	void RestoreUser(HttpContext context);
	void Stop();
	IEnumerable<IUser> Users { get; }
}
