using System.Collections;
using System.Xml;

namespace Codebot.Xml
{
    public class Attributes : Nodes<Attribute>
    {
        private Filer filer;

        protected override XmlNode GetItem(string name)
        {
            return InternalNode.Attributes.GetNamedItem(name);
        }

        protected override XmlNode GetItem(int index)
        {
            return List.Item(index);
        }

        protected override IEnumerable GetEnumerable()
        {
            return List;
        }

        internal XmlAttributeCollection List { get; set; }

        internal Attributes(XmlAttributeCollection list, XmlNode node)
            : base(node)
        {
            List = list;
        }

        public override int Count
        {
            get
            {
                return List.Count;
            }
        }

        public Filer Filer
        {
            get
            {
                return filer ??= new AttributeFiler(InternalNode);
            }
        }
    }
}
