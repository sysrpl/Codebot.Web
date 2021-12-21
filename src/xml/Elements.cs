namespace Codebot.Xml;

using System.Collections;
using System.Xml;

public class Elements : Nodes<Element>
{
    internal Elements(XmlNode node) : base(node)
    {
    }

    protected override IEnumerable GetEnumerable() =>  InternalNode.ChildNodes;

    protected override XmlNode GetItem(string name) => InternalNode.SelectSingleNode(name);

    protected override XmlNode GetItem(int index) =>  InternalNode.ChildNodes.Item(index);

    public override int Count
    {
        get => InternalNode.ChildNodes.Count;
    }
}