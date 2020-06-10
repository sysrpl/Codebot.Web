using System;
using System.Linq;

namespace Codebot.Web
{
	/// <summary>
	/// Components can be used by web handlers to render html
	/// </summary>
	public class Component
	{
        public BasicHandler Owner { get; }

        public Component(BasicHandler owner)
		{
			Owner = owner;
		}

		/// <summary>
		/// Render the  component as html
		/// </summary>
		protected virtual string Render()
		{
			return GetType().Name.Split('.').Last();
		}

		public override string ToString()
		{
			return Render();
		}
	}
}
