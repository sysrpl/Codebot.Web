namespace Codebot.Xml;

using System.Xml;

public class Node : Markup
{
    internal Node()
    {
    }

    internal Node(XmlNode node) : base(node)
    {
    }

    public Document Document
    {
        get => new Document(InternalNode.OwnerDocument);
    }

    public string Name
    {
        get => InternalNode.Name;
    }

    public Element Parent
    {
        get =>InternalNode.ParentNode is XmlElement node ? new Element(node) : null;
    }

    public Attribute AsAttribute()
    {
        if (InternalNode is XmlAttribute)
            return new Attribute(InternalNode as XmlAttribute);
        return null;
    }

    public Element AsElement()
    {
        if (InternalNode is XmlElement)
            return new Element(InternalNode as XmlElement);
        return null;
    }

    public override string Text
    {
        get => InternalNode.OuterXml;

        set { }
    }

    public virtual string Value
    {
        get => InternalNode.InnerXml;

        set => InternalNode.InnerXml = value;
    }

    internal XmlNode InternalNode
    {
        get => (XmlNode)Controller;
    }

    public static implicit operator XmlNode(Node node) => node.InternalNode;

    public static XmlElement Force(XmlElement node, string path)
    {
        string[] items = path.Split('/');
        XmlElement parent = node, child;
        for (int i = 0; i < items.Length; i++)
        {
            child = parent.SelectSingleNode(items[i]) as XmlElement;
            if (child is not null)
            {
                parent = child;
                continue;
            }
            child = node.OwnerDocument.CreateElement(items[i]);
            parent.AppendChild(child);
            parent = child;
        }
        return parent;
    }
}
