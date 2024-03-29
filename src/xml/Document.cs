namespace Codebot.Xml;

using System;
using System.IO;
using System.Text;
using System.Xml;

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
        get => InternalDocument.DocumentElement is XmlElement node ? new Element(node) : null;
        set
        {
            InternalDocument.RemoveAll();
            InternalDocument.AppendChild(value.InternalNode);
        }
    }

    public override string Text
    {
        get => InternalDocument.InnerXml;
        set => InternalDocument.InnerXml = value;
    }

    internal XmlDocument InternalDocument
    {
        get => (XmlDocument)Controller;
    }

    public static Document Open(string fileName)
    {
        var document = new Document();
        document.Load(fileName);
        return document;
    }

    public static Element OpenElement(string fileName, string query) => Open(fileName).FindNode(query);

    public static Elements OpenElements(string fileName, string query) => Open(fileName).FindNodes(query);

    public static bool operator ==(Document a, Document b)
    {
        object x = a, y = b;
        if ((x is null) && (y is null))
            return true;
        if (x is null)
            return false;
        if (y is null)
            return false;
        return a.Text == b.Text;
    }

    public static bool operator !=(Document a, Document b) => !(a == b);

    public static explicit operator Document(string text) => new(text);

    public static explicit operator string(Document document) => document.Text;

    public static implicit operator Document(XmlDocument document) => new(document);

    public static implicit operator XmlDocument(Document document) => document.InternalDocument;

    public override int GetHashCode() => Text.GetHashCode();

    public override bool Equals(object obj)
    {
        if (obj is Document)
            return (obj as Document)?.Text == Text;
        return ReferenceEquals(this, obj);
    }

    public override string ToString() => Text;

    public string ToString(bool beautiful)
    {
        if (beautiful)
        {
            using var stream = new MemoryStream();
            using var streamWriter = new StreamWriter(stream);
            var settings = new XmlWriterSettings { Encoding = Encoding.UTF8, Indent = true };
            using XmlWriter writer = XmlWriter.Create(streamWriter, settings);
            InternalDocument.Save(writer);
            using var streamReader = new StreamReader(stream);
            stream.Position = 0;
            return streamReader.ReadToEnd();
        }
        else
            return Text;
    }

    public Attribute CreateAttribute(string name) => new (InternalDocument.CreateAttribute(name));

    public Element CreateElement(string name) => new (InternalDocument.CreateElement(name));

    public void Instruct(string target, string data)
    {
        XmlNode node = InternalDocument.CreateProcessingInstruction(target, data);
        InternalDocument.AppendChild(node);
    }

    public void Load(string filename) => InternalDocument.Load(filename);

    public void Save(string filename) => InternalDocument.Save(filename);

    public void Save(string filename, bool beautiful)
    {
        if (beautiful)
        {
            using var stream = new FileStream(filename, FileMode.Create);
            using var streamWriter = new StreamWriter(stream);
            var settings = new XmlWriterSettings { Encoding = Encoding.UTF8, Indent = true };
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
        if ((node is null) || (node.Name != names[0]))
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
        return node == null ? null : new (node as XmlElement);
    }

    public Element FindNode(string xpath, params object[] args)
    {
        XmlNode node = InternalDocument.SelectSingleNode(string.Format(xpath, args));
        return node == null ? null : new (node as XmlElement);
    }

    public Elements FindNodes(string xpath)
    {
        var nodes = InternalDocument.SelectNodes(xpath);
        if (nodes is null)
            return null;
        return new ElementSelect(nodes, InternalDocument);
    }

    public Elements FindNodes(string xpath, params object[] args)
    {
        XmlNodeList nodes = InternalDocument.SelectNodes(string.Format(xpath, args));
        if (nodes is null)
            return null;
        return new ElementSelect(nodes, InternalDocument);
    }

    public Document Clone() => new (InternalDocument.Clone() as XmlDocument);

    #region ICloneable Members
    object ICloneable.Clone() => InternalDocument.Clone();
    #endregion
}
