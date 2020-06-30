using System;
using System.IO;
using System.Text;
using System.Xml;

namespace Codebot.Xml
{
    public class Document : Markup, ICloneable
    {
        public Document()
            : base(new XmlDocument())
        {
        }

        public Document(string text)
            : base(new XmlDocument())
        {
            Text = text;
        }

        internal Document(XmlDocument document)
            : base(document)
        {
        }

        public Element Root
        {
            get
            {
                return InternalDocument.DocumentElement is XmlElement node ? new Element(node) : null;
            }

            set
            {
                InternalDocument.RemoveAll();
                InternalDocument.AppendChild(value.InternalNode);
            }
        }

        public override string Text
        {
            get
            {
                return InternalDocument.InnerXml;
            }

            set
            {
                InternalDocument.InnerXml = value;
            }
        }

        internal XmlDocument InternalDocument
        {
            get
            {
                return (XmlDocument)Controller;
            }
        }

        public static Document Open(string fileName)
        {
            Document document = new Document();
            document.Load(fileName);
            return document;
        }

        public static Element OpenElement(string fileName, string query)
        {
            return Open(fileName).FindNode(query);
        }

        public static Elements OpenElements(string fileName, string query)
        {
            return Open(fileName).FindNodes(query);
        }

        public static bool operator ==(Document a, Document b)
        {
            object x = a, y = b;
            if ((x == null) && (y == null))
                return true;
            if (x == null)
                return false;
            if (y == null)
                return false;
            return a.Text == b.Text;
        }

        public static bool operator !=(Document a, Document b)
        {
            return !(a == b);
        }

        public static explicit operator Document(string text)
        {
            return new Document(text);
        }

        public static explicit operator string(Document document)
        {
            return document.Text;
        }

        public static implicit operator Document(XmlDocument document)
        {
            return new Document(document);
        }

        public static implicit operator XmlDocument(Document document)
        {
            return document.InternalDocument;
        }

        public override int GetHashCode()
        {
            return Text.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is Document)
                return (obj as Document)?.Text == Text;
            return ReferenceEquals(this, obj);
        }

        public override string ToString()
        {
            return Text;
        }

        public string ToString(bool beautiful)
        {
            if (beautiful)
            {
                using MemoryStream stream = new MemoryStream();
                using StreamWriter streamWriter = new StreamWriter(stream);
                XmlWriterSettings settings = new XmlWriterSettings { Encoding = Encoding.UTF8, Indent = true };
                using XmlWriter writer = XmlWriter.Create(streamWriter, settings);
                InternalDocument.Save(writer);
                using StreamReader streamReader = new StreamReader(stream);
                stream.Position = 0;
                return streamReader.ReadToEnd();
            }
            else
                return Text;
        }

        public Attribute CreateAttribute(string name)
        {
            return new Attribute(InternalDocument.CreateAttribute(name));
        }

        public Element CreateElement(string name)
        {
            return new Element(InternalDocument.CreateElement(name));
        }

        public void Instruct(string target, string data)
        {
            XmlNode node = InternalDocument.CreateProcessingInstruction(target, data);
            InternalDocument.AppendChild(node);
        }

        public void Load(string filename)
        {
            InternalDocument.Load(filename);
        }

        public void Save(string filename)
        {
            InternalDocument.Save(filename);
        }

        public void Save(string filename, bool beautiful)
        {
            if (beautiful)
            {
                using FileStream stream = new FileStream(filename, FileMode.Create);
                using StreamWriter streamWriter = new StreamWriter(stream);
                XmlWriterSettings settings = new XmlWriterSettings { Encoding = Encoding.UTF8, Indent = true };
                using XmlWriter writer = XmlWriter.Create(streamWriter, settings);
                InternalDocument.Save(writer);
            }
            else
                InternalDocument.Save(filename);
        }

        public Element Force(string name)
        {
            Element node = Root;
            string[] names = name.Split('/');
            if ((node == null) || (node.Name != names[0]))
            {
                InternalDocument.RemoveAll();
                node = CreateElement(name);
                Root = node;
            }
            for (int i = 1; i < names.Length; i++)
                node = node.Force(names[i]);
            return node;
        }

        public Element FindNode(string xpath)
        {
            XmlNode node = InternalDocument.SelectSingleNode(xpath);
            return node == null ? null : new Element(node as XmlElement);
        }

        public Element FindNode(string xpath, params object[] args)
        {
            XmlNode node = InternalDocument.SelectSingleNode(String.Format(xpath, args));
            return node == null ? null : new Element(node as XmlElement);
        }

        public Elements FindNodes(string xpath)
        {
            XmlNodeList nodes = InternalDocument.SelectNodes(xpath);
            if (nodes == null)
                return null;
            return new ElementSelect(nodes, InternalDocument);
        }

        public Elements FindNodes(string xpath, params object[] args)
        {
            XmlNodeList nodes = InternalDocument.SelectNodes(String.Format(xpath, args));
            if (nodes == null)
                return null;
            return new ElementSelect(nodes, InternalDocument);
        }

        public Document Clone()
        {
            return new Document(InternalDocument.Clone() as XmlDocument);
        }

        #region ICloneable Members
        object ICloneable.Clone()
        {
            return InternalDocument.Clone();
        }
        #endregion
    }
}
