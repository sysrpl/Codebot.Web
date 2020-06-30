using System.Xml;

namespace Codebot.Xml
{
    internal class ElementFiler : Filer
    {
        internal ElementFiler(XmlNode node) : base(node)
        {
        }

        protected override string ReadValue(string name, string value, bool stored)
        {
            XmlNode node = InternalNode.SelectSingleNode(name);
            if ((node == null) && stored)
            {
                node = Node.Force(InternalNode as XmlElement, name);
                node.InnerText = value;
            }
            return node != null ? node.InnerText : value;
        }

        protected override void WriteValue(string name, string value)
        {
            Node.Force(InternalNode as XmlElement, name).InnerText = value;
        }
    }
}
