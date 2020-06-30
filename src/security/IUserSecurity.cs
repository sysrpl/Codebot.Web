using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace Codebot.Web
{
	public interface IUserSecurity
	{
		void Start();
		void RestoreUser(HttpContext context);
		void Stop();
        IUser User { get; }
		IEnumerable<IUser> Users { get; }
	}
}
