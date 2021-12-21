namespace Codebot.Xml;

using System.Xml;

internal class AttributeFiler : Filer
{
	internal XmlAttributeCollection InternalAttributes
	{
		get => InternalNode.Attributes;
	}

	internal AttributeFiler(XmlNode node)
		: base(node)
	{
	}

	protected override string ReadValue(string name, string value, bool stored)
	{
		var node = InternalAttributes.GetNamedItem(name);
		if ((node == null) && stored)
		{
			node = InternalNode.OwnerDocument.CreateAttribute(name);
			InternalAttributes.SetNamedItem(node);
			node.InnerText = value;
		}
		return node != null ? node.InnerText : value;
	}

	protected override void WriteValue(string name, string value)
	{
		XmlNode node = InternalNode.OwnerDocument.CreateAttribute(name);
		InternalAttributes.SetNamedItem(node);
		node.Value = value;
	}
}
