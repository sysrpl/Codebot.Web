using System.Xml;

namespace Codebot.Xml
{
	public class Attribute : Node
	{
		internal Attribute(XmlAttribute node)
			: base(node)
		{
		}

		internal XmlAttribute InternalAttribute
		{
			get
			{
				return (XmlAttribute)Controller;
			}
		}

		public static implicit operator XmlAttribute(Attribute attribute)
		{
			return attribute.InternalAttribute;
		}

		public static implicit operator Attribute(XmlAttribute attribute)
		{
			return new Attribute(attribute);
		}
	}
}
