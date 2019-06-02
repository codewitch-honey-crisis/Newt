using System;
using System.Collections.Generic;
using System.Text;

namespace Grimoire
{
#if GRIMOIRELIB
	public
#else
	internal
#endif
	partial class ParseNode
	{
		WeakReference<ParseNode> _parent;
		int _line;
		int _column;
		long _position;
		public object ParsedValue { get; set; }
		public ParseNode Parent {
			get {
				ParseNode result = null;
				if (null != _parent && !_parent.TryGetTarget(out result))
					return null;
				return result;
			}
			set {
				if (null != value)
					_parent = new WeakReference<ParseNode>(value);
				else
					_parent = null;
			}
		}
		public IEnumerable<ParseNode> Select(object symbol, ISymbolResolver resolver)
		{
			if (null == resolver)
				throw new ArgumentNullException("resolver");
			var ic = Children.Count;
			for (var i = 0; i < ic; ++i)
			{
				var child = Children[i];
				if (Equals(symbol, resolver.GetSymbolById(child.SymbolId)))
					yield return child;
			}
		}
		public IEnumerable<ParseNode> Select(int symbolId)
		{
			var ic = Children.Count;
			for (var i = 0; i < ic; ++i)
			{
				var child = Children[i];
				if (symbolId == child.SymbolId)
					yield return child;
			}
		}
		public IList<ParseNode> FillDescendantsAndSelf(IList<ParseNode> result = null)
		{
			if (null == result) result = new List<ParseNode>();
			result.Add(this);
			var ic = Children.Count;
			for (var i = 0; i < ic; ++i)
				Children[i].FillDescendantsAndSelf(result);
			return result;
		}
		internal void SetLineInfo(int line, int column, long position)
		{
			_line = line;
			_column = column;
			_position = position;
		}
		public int Line {
			get {
				if (null == Value)
				{
					if (0 < Children.Count)
						return Children[0].Line;
					return 0;
				}
				else
				{
					return _line;
				}
			}
		}
		public int Column {
			get {
				if (null == Value)
				{
					if (0 < Children.Count)
						return Children[0].Column;
					return 0;
				}
				else
				{
					return _column;
				}
			}
		}
		public long Position {
			get {
				if (null == Value)
				{
					if (0 < Children.Count)
						return Children[0].Position;
					return 0;
				}
				else
				{
					return _position;
				}
			}
		}

		public int Length {
			get {
				if (null == Value)
				{
					if (0 < Children.Count)
					{
						var c = Children.Count - 1;
						var p = Children[c].Position;
						var l = Children[c].Length;
						return (int)(p - Position) + l;
					}
					return 0;
				}
				else
					return Value.Length;
			}
		}

		public int SymbolId { get; set; }
		public string Value { get; set; }
		public IList<ParseNode> Children { get; } = new List<ParseNode>();

		public override string ToString()
		{
			var sb = new StringBuilder();
			AppendTreeTo(sb);
			return sb.ToString();
		}
		public string ToString(ISymbolResolver resolver)
		{
			var sb = new StringBuilder();
			AppendTreeTo(sb, resolver);
			return sb.ToString();
		}

		public void AppendTreeTo(StringBuilder result, ISymbolResolver resolver = null) { AppendTreeTo(result, this, resolver); }
		public static void AppendTreeTo(StringBuilder result, ParseNode node, ISymbolResolver resolver = null)
		{
			// adapted from https://stackoverflow.com/questions/1649027/how-do-i-print-out-a-tree-structure
			List<ParseNode> firstStack = new List<ParseNode>();
			firstStack.Add(node);

			List<List<ParseNode>> childListStack = new List<List<ParseNode>>();
			childListStack.Add(firstStack);

			while (childListStack.Count > 0)
			{
				List<ParseNode> childStack = childListStack[childListStack.Count - 1];

				if (childStack.Count == 0)
				{
					childListStack.RemoveAt(childListStack.Count - 1);
				}
				else
				{
					node = childStack[0];
					childStack.RemoveAt(0);

					string indent = "";
					for (int i = 0; i < childListStack.Count - 1; i++)
					{
						indent += (childListStack[i].Count > 0) ? "|  " : "   ";
					}
					var s = node.SymbolId.ToString();
					if (null != resolver)
						s = Convert.ToString(resolver.GetSymbolById(node.SymbolId));
					if (s == node.SymbolId.ToString())
						result.Append(string.Concat(indent, "+- ", s, " ", node.Value ?? "").TrimEnd());
					else
						result.Append(string.Concat(indent, "+- ", s, " ", node.Value ?? "").TrimEnd());
					result.AppendLine();// string.Concat(" at line ", node.Line, ", column ", node.Column, ", position ", node.Position, ", length of ", node.Length));
					if (node.Children.Count > 0)
					{
						childListStack.Add(new List<ParseNode>(node.Children));
					}
				}
			}
		}
	}
}
