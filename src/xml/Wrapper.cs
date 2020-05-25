using System;

namespace Codebot.Xml
{
    public class Wrapper
	{
		internal Wrapper()
		{
		}

		internal Wrapper(object controller)
        {
            Controller = controller ?? throw new NullReferenceException();
		}

		internal object Controller { get; set; }
	}
}
