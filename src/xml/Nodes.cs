using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;

namespace Codebot.Xml
{
	public abstract class Nodes<T> : Wrapper, IList, ICollection, IEnumerable, IEnumerable<T> where T : Node
	{
		internal Nodes(XmlNode node) : base(node)
		{
		}

		public T this[string name]
		{
			get
			{
				XmlNode node = GetItem(name);
				if (node == null)
					return null;
				return CreateElement(node);
			}
			set
			{
				Replace(this[name], value);
			}
		}

		public T this[int index]
		{
			get
			{
				return CreateElement(GetItem(index));
			}

			set
			{
				Replace(this[index], value);
			}
		}

		public abstract int Count { get; }

		internal XmlNode InternalNode
		{
			get
			{
				return (XmlNode)Controller;
			}
		}

		internal T CreateElement(XmlNode node)
		{
			object instance = null;
			if (typeof(T) == typeof(Attribute))
				instance = new Attribute(node as XmlAttribute);
			else if (typeof(T) == typeof(Element))
				instance = new Element(node as XmlElement);
			return instance as T;
		}

		internal XmlNode CreateElementController(string name)
		{
			if (typeof(T) == typeof(Attribute))
			{
				XmlAttribute attribute = InternalNode.OwnerDocument.CreateAttribute(name);
				InternalNode.Attributes.Append(attribute);
				return attribute;
			}
			if (typeof(T) == typeof(Element))
			{
				XmlNode node = InternalNode.OwnerDocument.CreateElement(name);
				InternalNode.AppendChild(node);
				return node;
			}
			return null;
		}

		protected abstract IEnumerable GetEnumerable();

		protected abstract XmlNode GetItem(string name);

		protected abstract XmlNode GetItem(int index);

		public T Add(string name)
		{
			XmlNode node = CreateElementController(name);
			return CreateElement(node);
		}

		public void Add(T node)
		{
			InternalNode.AppendChild(node.InternalNode);
		}

		public void Clear()
		{
			InternalNode.RemoveAll();
		}

		public void Delete(int index)
		{
			InternalNode.RemoveChild(this[index].InternalNode);
		}

		public void Delete(string name)
		{
			Node node = this[name];
			if (node != null)
				InternalNode.RemoveChild(node.InternalNode);
		}

		public void Insert(int index, T node)
		{
			if (Count == 0)
				InternalNode.AppendChild(node.InternalNode);
			else if (index < 1)
				InternalNode.InsertBefore(node.InternalNode, InternalNode.FirstChild);
			else if (index >= Count)
				InternalNode.AppendChild(node.InternalNode);
			else
				InternalNode.InsertBefore(GetItem(index), node.InternalNode);
		}

		public void InsertAfter(T child, T after)
		{
			InternalNode.InsertAfter(child, after);
		}

		public void InsertBefore(T child, T before)
		{
			InternalNode.InsertBefore(child, before);
		}

		public void Move(int curIndex, int newIndex)
		{
			XmlNode curNode = GetItem(curIndex), newNode = GetItem(newIndex);
			InternalNode.RemoveChild(curNode);
			InternalNode.InsertBefore(curNode, newNode);
		}

		public void Remove(T node)
		{
			InternalNode.RemoveChild(node.InternalNode);
		}

		public void Replace(T oldNode, T newNode)
		{
			if (newNode.InternalNode.ParentNode != null)
				newNode.InternalNode.ParentNode.RemoveChild(newNode.InternalNode);
			InternalNode.InsertBefore(oldNode.InternalNode, newNode.InternalNode);
			InternalNode.RemoveChild(oldNode.InternalNode);
		}

		#region IEnumerable Members
		IEnumerator IEnumerable.GetEnumerator()
		{
			for (int i = 0; i < Count; i++)
			{
				yield return this[i];
			}
		}
		#endregion

		#region IEnumerable<T> Members
		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			for (int i = 0; i < Count; i++)
			{
				yield return this[i];
			}
		}
		#endregion

		#region ICollection Members
		public void CopyTo(Array array, int index)
		{
		}

		public bool IsSynchronized
		{
			get { return false; }
		}

		public object SyncRoot
		{
			get { return null; }
		}
		#endregion

		#region IList Members
		public int Add(object value)
		{
			return -1;
		}

		public bool Contains(object value)
		{
			return false;
		}

		public int IndexOf(object value)
		{
			return -1;
		}

		public void Insert(int index, object value)
		{
		}

		public bool IsFixedSize
		{
			get { return true; }
		}

		public bool IsReadOnly
		{
			get { return true; }
		}

		public void Remove(object value)
		{
		}

		public void RemoveAt(int index)
		{
		}

		object IList.this[int index]
		{
			get
			{
				return GetItem(index);
			}
			set
			{
			}
		}
		#endregion
	}
}
