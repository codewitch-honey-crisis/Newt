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
	enum LLNodeType
	{
		Initial =0,
		NonTerminal=1,
		EndNonTerminal=2,
		Terminal=3,
		Error=4,
		EndDocument=5
	}
#if GRIMOIRELIB
	public
#else
	internal
#endif
	abstract class LLParser : ISymbolResolver,IDisposable
	{
		public abstract LLNodeType NodeType { get; }

		public abstract int SymbolId { get; }
		public abstract string Value { get; }

		public abstract int Line { get; }
		public abstract int Column { get; }
		public abstract long Position { get; }

		public abstract string GetSymbolById(int symbolId);
		public abstract int GetSymbolId(string symbol);
		public abstract bool Read();
		public abstract void Close();
		public abstract void Restart(ParseContext parseContext);
		void IDisposable.Dispose() { Close(); }

		public virtual ParseNode ParseSubtree(bool trimEmpties = false)
		{
			if (!Read())
				return null;
			if (NodeType == LLNodeType.EndNonTerminal)
				return null;

			var result = new ParseNode();
			var id = SymbolId;
			if (LLNodeType.NonTerminal == NodeType)
			{
				while (true)
				{
					var k = ParseSubtree(trimEmpties);
					if (null != k)
					{
						k.Parent = result;
						if (!trimEmpties || ((null != k.Value) || 0 < k.Children.Count))
							result.Children.Add(k);
					}
					else
						break;
				}
				result.SymbolId = id;
				return result;
			}
			else if (LLNodeType.Terminal == NodeType)
			{
				result.SetLineInfo(Line, Column, Position);
				result.SymbolId = id;
				result.Value = Value;
				//result.ParsedValue = ParsedValue;
				return result;
			}
			else if (LLNodeType.Error == NodeType)
			{
				result.SetLineInfo(Line, Column, Position);
				result.SymbolId = id;
				result.Value = Value;
				result.ParsedValue = Value;
				return result;
			}
			return null;
		}
	}
}
