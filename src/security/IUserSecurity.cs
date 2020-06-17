using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace Codebot.Web
{
	public interface IUserSecurity
	{
        IUser User { get; }
		IEnumerable<IUser> Users { get; }
		void Start(HttpContext context);
		void BeginReuqest(HttpContext context);
	}
}
