using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace Codebot.Web
{
	public interface IUserSecurity
	{
        HttpContext Context { get; }
        IUser User { get; }
		IEnumerable<IUser> Users { get; }
	}
}
