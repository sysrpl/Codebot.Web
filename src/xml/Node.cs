using System.Xml;

namespace Codebot.Xml
{
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
            get
            {
                return new Document(InternalNode.OwnerDocument);
            }
        }

        public string Name
        {
            get
            {
                return InternalNode.Name;
            }
        }

        public Element Parent
        {
            get
            {
                return InternalNode.ParentNode is XmlElement node ? new Element(node) : null;
            }
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
            get
            {
                return InternalNode.OuterXml;
            }

            set
            {
            }
        }

        public virtual string Value
        {
            get
            {
                return InternalNode.InnerXml;
            }

            set
            {
                InternalNode.InnerXml = value;
            }
        }

        internal XmlNode InternalNode
        {
            get
            {
                return (XmlNode)Controller;
            }
        }

        public static implicit operator XmlNode(Node node)
        {
            return node.InternalNode;
        }

        public static XmlElement Force(XmlElement node, string path)
        {
            string[] items = path.Split('/');
            XmlElement parent = node, child;
            for (int i = 0; i < items.Length; i++)
            {
                child = parent.SelectSingleNode(items[i]) as XmlElement;
                if (child != null)
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
}
