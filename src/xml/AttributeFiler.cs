using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;

namespace Codebot.Xml
{
	internal class AttributeFiler : Filer
	{
		internal XmlAttributeCollection InternalAttributes
		{
			get
			{
				return InternalNode.Attributes;
			}
		}

		internal AttributeFiler(XmlNode node)
			: base(node)
		{
		}

		protected override string ReadValue(string name, string value, bool stored)
		{
			XmlNode node = InternalNode;
			node = InternalAttributes.GetNamedItem(name);
			if ((node == null) && (stored))
			{
				node = InternalNode.OwnerDocument.CreateAttribute(name);
				InternalAttributes.SetNamedItem(node);
				node.InnerText = value;
			};
			return node != null ? node.InnerText : value;
		}

		protected override void WriteValue(string name, string value)
		{
			XmlNode node = InternalNode.OwnerDocument.CreateAttribute(name);
			InternalAttributes.SetNamedItem(node);
			node.Value = value;
		}
	}
}
