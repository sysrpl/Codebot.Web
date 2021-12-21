namespace Codebot.Xml;

using System.Collections;
using System.Xml;

public class Attributes : Nodes<Attribute>
{
    private Filer filer;

    protected override XmlNode GetItem(string name) => InternalNode.Attributes.GetNamedItem(name);

    protected override XmlNode GetItem(int index) => List.Item(index);

    protected override IEnumerable GetEnumerable() => List;

    internal XmlAttributeCollection List { get; set; }

    internal Attributes(XmlAttributeCollection list, XmlNode node)
        : base(node)
    {
        List = list;
    }

    public override int Count
    {
        get => List.Count;
    }

    public Filer Filer
    {
        get => filer ??= new AttributeFiler(InternalNode);
    }
}
