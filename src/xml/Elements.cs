using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;

namespace Codebot.Xml
{
    public class Elements : Nodes<Element>
	{
		internal Elements(XmlNode node) : base(node)
		{
		}

		protected override IEnumerable GetEnumerable()
		{
			return InternalNode.ChildNodes;
		}

		protected override XmlNode GetItem(string name)
		{
			return InternalNode.SelectSingleNode(name);
		}

		protected override XmlNode GetItem(int index)
		{
			return InternalNode.ChildNodes.Item(index);
		}

		public override int Count
		{
			get
			{
				return InternalNode.ChildNodes.Count;
			}
		}
	}
}