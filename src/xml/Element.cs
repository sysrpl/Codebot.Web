using System;
using System.Xml;
using System.Collections.Generic;

namespace Codebot.Xml
{
    public class Element : Node
    {
        private Elements elements;
        private Filer attributesFiler;
        private Filer filer;

        internal Element(XmlElement element)
            : base(element)
        {
            elements = null;
            attributesFiler = null;
            filer = null;
        }

        public Attributes Attributes
        {
            get
            {
                return new Attributes(InternalNode.Attributes, InternalNode);
            }
        }

        public Element Remove(Func<Element, bool> func)
        {
            var items = new List<Element>(Nodes as IEnumerable<Element>);
            var list = new List<Element>();
            items.ForEach(element => { if (!func(element)) list.Add(element); });
            Elements node = Nodes;
            node.Clear();
            list.ForEach((element) => node.Add(element));
            return this;
        }

        public Element Sort(Comparison<Element> comparison)
        {
            List<Element> items = new List<Element>(Nodes as IEnumerable<Element>);
            Elements node = Nodes;
            node.Clear();
            items.Sort(comparison);
            items.ForEach(element => node.Add(element));
            return this;
        }

        public Elements Nodes
        {
            get
            {
                return elements ??= new Elements(InternalNode);
            }
        }

        private Filer AttributesFiler
        {
            get
            {
                return attributesFiler ??= Attributes.Filer;
            }
        }

        public Filer Filer
        {
            get
            {
                return filer ??= new ElementFiler(InternalNode);
            }
        }

        internal XmlElement InternalElement
        {
            get
            {
                return (XmlElement)Controller;
            }
        }

        public static implicit operator Element(XmlElement element)
        {
            return new Element(element);
        }

        public static implicit operator XmlElement(Element element)
        {
            return element.InternalElement;
        }

        public void RemoveChild(Node node)
        {
            var e = InternalNode as XmlElement;
            e.RemoveChild(node.InternalNode);
        }

        public void AppendChild(Node node)
        {
            var e = InternalNode as XmlElement;
            e.AppendChild(node.InternalNode);
        }

        public void InsertAfter(Node newChild, Node refChild)
        {
            var e = InternalNode as XmlElement;
            e.InsertAfter(newChild, refChild);
        }

        public void InsertBefore(Node newChild, Node refChild)
        {
            var e = InternalNode as XmlElement;
            e.InsertAfter(newChild, refChild);
        }

        public Element FirstChild
        {
            get
            {
                var e = InternalNode as XmlElement;
                var n = e.FirstChild;
                while (n != null)
                {
                    if (n is XmlElement)
                        return new Element(n as XmlElement);
                    n = n.NextSibling;
                }
                return null;
            }
        }

        public Element LastChild
        {
            get
            {
                var e = InternalNode as XmlElement;
                var n = e.LastChild;
                while (n != null)
                    if (n is XmlElement)
                        return new Element(n as XmlElement);
                    else
                        n = n.PreviousSibling;
                return null;
            }
        }

        public Element NextSibling
        {
            get
            {
                var e = InternalNode as XmlElement;
                var n = e.NextSibling;
                while (n != null)
                {
                    if (n is XmlElement)
                        return new Element(n as XmlElement);
                    n = n.NextSibling;
                }
                return null;
            }
        }

        public Attribute FindAttribute(string xpath)
        {
            return InternalNode.SelectSingleNode(xpath) is XmlAttribute node ? new Attribute(node) : null;
        }

        public Attribute FindAttribute(string xpath, params object[] args)
        {
            return FindAttribute(string.Format(xpath, args));
        }

        public Element FindNode(string xpath)
        {
            return InternalNode.SelectSingleNode(xpath) is XmlElement node ? new Element(node) : null;
        }

        public Element FindNode(string xpath, params object[] args)
        {
            return FindNode(string.Format(xpath, args));
        }

        public Elements FindNodes(string xpath)
        {
            var nodes = InternalNode.SelectNodes(xpath);
            if (nodes == null)
                return null;
            return new ElementSelect(nodes, InternalNode);
        }

        public Elements FindNodes(string xpath, params object[] args)
        {
            return FindNodes(String.Format(xpath, args));
        }

        public void CopyValue(Element element, string name)
        {
            WriteString(name, element.ReadString(name));
        }

        public void CopyValue(Element element, string[] names)
        {
            foreach (string name in names)
                WriteString(name, element.ReadString(name));
        }

        public Element Force(string name)
        {
            return new Element(Force(InternalElement, name));
        }

        public bool ReadBool(string name)
        {
            if (String.IsNullOrEmpty(name))
                return false;
            if (name[0] == '@')
                return AttributesFiler.ReadBool(name.Substring(1));
            else
                return Filer.ReadBool(name);
        }

        public void WriteBool(string name, bool value)
        {
            if (String.IsNullOrEmpty(name))
                return;
            if (name[0] == '@')
                AttributesFiler.WriteBool(name.Substring(1), value);
            else
                Filer.WriteBool(name, value);
        }

        public string ReadString(string name)
        {
            if (String.IsNullOrEmpty(name))
                return String.Empty;
            if (name[0] == '@')
                return AttributesFiler.ReadString(name.Substring(1));
            else
                return Filer.ReadString(name);
        }

        public void WriteString(string name, string value)
        {
            if (String.IsNullOrEmpty(name))
                return;
            if (name[0] == '@')
                AttributesFiler.WriteString(name.Substring(1), value);
            else
                Filer.WriteString(name, value);
        }

        public int ReadInt(string name)
        {
            if (String.IsNullOrEmpty(name))
                return 0;
            if (name[0] == '@')
                return AttributesFiler.ReadInt(name.Substring(1));
            else
                return Filer.ReadInt(name);
        }

        public void WriteInt(string name, int value)
        {
            if (String.IsNullOrEmpty(name))
                return;
            if (name[0] == '@')
                AttributesFiler.WriteInt(name.Substring(1), value);
            else
                Filer.WriteInt(name, value);
        }

        public long ReadLong(string name)
        {
            if (String.IsNullOrEmpty(name))
                return 0;
            if (name[0] == '@')
                return AttributesFiler.ReadLong(name.Substring(1));
            else
                return Filer.ReadLong(name);
        }

        public void WriteLong(string name, long value)
        {
            if (String.IsNullOrEmpty(name))
                return;
            if (name[0] == '@')
                AttributesFiler.WriteLong(name.Substring(1), value);
            else
                Filer.WriteLong(name, value);
        }
    }
}
