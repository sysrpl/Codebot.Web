using System.Collections;
using System.Xml;

namespace Codebot.Xml
{
	public class ElementSelect : Elements
	{
		private readonly XmlNodeList list;

		internal ElementSelect(XmlNodeList list, XmlNode node) : base(node)
		{
			this.list = list;
		}

		protected override IEnumerable GetEnumerable()
		{
			return list;
		}

		protected override XmlNode GetItem(string name)
		{
			return InternalNode.SelectSingleNode(name);
		}

		protected override XmlNode GetItem(int index)
		{
			return list.Item(index);
		}

		public override int Count
		{
			get
			{
				return list.Count;
			}
		}
	}
}