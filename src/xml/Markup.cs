using System;

namespace Codebot.Xml
{
	public abstract class Markup : Wrapper
	{
		protected Markup(object controller)
			: base(controller)
		{
		}

		protected Markup()
		{
		}

		public abstract string Text { get; set; }

        public override string ToString()
        {
            return Text;
        }
	}
}
