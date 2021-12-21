namespace Codebot.Xml;

using System.Xml;

public class Attribute : Node
{
	internal Attribute(XmlAttribute node)
		: base(node)
	{
	}

	internal XmlAttribute InternalAttribute
	{
		get => (XmlAttribute)Controller;
	}

	public static implicit operator XmlAttribute(Attribute attribute) => attribute.InternalAttribute;

	public static implicit operator Attribute(XmlAttribute attribute) => new Attribute(attribute);
}
