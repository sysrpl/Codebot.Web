using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace Codebot.Web
{
	public interface IUserSecurity
	{
		HttpContext Context { get; }
		IWebUser User { get; }
		IEnumerable<IWebUser> Users { get; }
	}
}
