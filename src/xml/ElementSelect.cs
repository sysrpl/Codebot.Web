namespace Codebot.Xml;

using System.Collections;
using System.Xml;

public class ElementSelect : Elements
{
	private readonly XmlNodeList list;

	internal ElementSelect(XmlNodeList list, XmlNode node) : base(node)
	{
		this.list = list;
	}

	protected override IEnumerable GetEnumerable() => list;

	protected override XmlNode GetItem(string name) => InternalNode.SelectSingleNode(name);

	protected override XmlNode GetItem(int index) => list.Item(index);

	public override int Count
	{
		get => list.Count;
	}
}
