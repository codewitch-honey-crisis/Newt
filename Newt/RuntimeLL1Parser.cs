using System;
using System.Collections.Generic;
using System.Text;

namespace Grimoire
{
#if GRIMOIRELIB || NEWT
	public
#else
	internal
#endif
	class RuntimeLL1Parser : LL1ParserBase
	{
		Cfg _cfg;
		int _errorSymbolId;
		int _endSymbolId;
		(int Accept, ((char First, char Last)[] Ranges, int Destination)[] Transitions, int[] PossibleAccepts)[] _lexTable;
		IDictionary<int, IDictionary<int, (int Left, int[] Right)>> _parseTable;
		HashSet<int> _hidden;
		HashSet<int> _collapsed;
		IDictionary<int,int> _substitute;
		IDictionary<int,string> _blockEnds;
		IDictionary<int, Type> _types;
		public RuntimeLL1Parser(Cfg cfg,FA lexer,ParseContext parseContext = null) : base(parseContext)
		{
			_cfg = cfg;
			_endSymbolId = cfg.GetSymbolId("#EOS");
			_errorSymbolId = cfg.GetSymbolId("#ERROR");
			_lexTable = lexer.ToDfaTable2<int>();
			_parseTable = cfg.ToLL1ParseTable();
			_hidden = new HashSet<int>();
			_collapsed = new HashSet<int>();
			_substitute = new Dictionary<int, int>();
			_blockEnds = new Dictionary<int, string>();
			_types = new Dictionary<int, Type>();
			foreach(var attrs in cfg.AttributeSets)
			{
				object o;
				if (attrs.Value.TryGetValue("hidden", out o) && o is bool && (bool)o)
					_hidden.Add(cfg.GetSymbolId(attrs.Key));
				if (attrs.Value.TryGetValue("collapse", out o) && o is bool && (bool)o)
					_collapsed.Add(cfg.GetSymbolId(attrs.Key));
				if (attrs.Value.TryGetValue("substitute", out o) && !string.IsNullOrEmpty(o as string))
					_substitute.Add(cfg.GetSymbolId(attrs.Key), cfg.GetSymbolId(o as string));
				if (attrs.Value.TryGetValue("blockEnd", out o) && !string.IsNullOrEmpty(o as string))
					_blockEnds.Add(cfg.GetSymbolId(attrs.Key), o as string);
				if (attrs.Value.TryGetValue("type", out o) && !string.IsNullOrEmpty(o as string))
					_types.Add(cfg.GetSymbolId(attrs.Key), ParserUtility.ResolveType(o as string));

			}
		}
		void _DoPop()
		{
			Stack.Pop();
			if (0 < Stack.Count)
			{
				var s = Stack.Peek();
				UpdateSymbolId(s);
				if (Token.SymbolId == s)
					UpdateNodeType(LLNodeType.Terminal);
				else if (0 > s)
					UpdateNodeType(LLNodeType.EndNonTerminal);
				else
					UpdateNodeType(LLNodeType.NonTerminal);
			}
			else
			{
				UpdateSymbolId(_endSymbolId);
				UpdateNodeType(LLNodeType.EndDocument);
			}
		}
		public override string GetSymbolById(int symbolId)
			=> _cfg.GetSymbolById(symbolId);
		public override int GetSymbolId(string symbol)
			=> _cfg.GetSymbolId(symbol);
		protected override bool IsCollapsed(int symbolId)
			=>_collapsed.Contains(symbolId);
		
		protected override int Substitute(int symbolId)
			=>_substitute.TryGetValue(symbolId, symbolId);
		
		protected override bool IsHidden(int symbolId)
			=>_hidden.Contains(symbolId);
		
		protected override string GetBlockEnd(int symbolId)
			=> _blockEnds.TryGetValue(symbolId, null);
		protected override Type GetType(int symbolId) => _types.TryGetValue(symbolId, null);
		
		protected override (int SymbolId, string Value, (int First, int Last)[] ExpectingRanges, int[] ExpectingSymbols) Lex(StringBuilder lexerBuffer)
		{
			var t= ParserUtility.Lex2(_lexTable, _errorSymbolId, ParseContext, lexerBuffer);
			return (t.Accept, t.Value, t.ExpectingRanges, t.ExpectingSymbols);
		}
		
		
		protected override bool ReadImpl()
		{
			if (LLNodeType.Error == NodeType && -1 == ParseContext.Current) return false;
			if(NodeType==LLNodeType.Initial)
			{
				var ss = _cfg.StartSymbol;
				var sid = _cfg.GetSymbolId(ss);
				Stack.Push(sid);
				if (_cfg.IsNonTerminal(ss))
					UpdateNodeType(LLNodeType.NonTerminal);
				else
					UpdateNodeType(LLNodeType.Terminal);
				UpdateSymbolId(sid);
				NextToken();
				return true;
			}
			if(0<Stack.Count)
			{
				var sid = Stack.Peek();
				UpdateSymbolId(sid);
				if (0>sid) // end non-terminal
				{
					_DoPop();
					return true;
				}
				if (Token.SymbolId == sid) // terminal
				{
					NextToken();
					_DoPop();
					return true;
				}
				IDictionary<int, (int Left, int[] Rule)> d;
				if(_parseTable.TryGetValue(sid,out d))
				{
					(int Left, int[] Right) rule;
					if(d.TryGetValue(Token.SymbolId,out rule))
					{
						_DoPop();
						sid = ~sid;
						Stack.Push(sid);
						for(int j=rule.Right.Length-1;j>=0;--j)
						{
							sid = rule.Right[j];
							Stack.Push(sid);
						}
						UpdateSymbolId(sid);
						if (Token.SymbolId == sid)
							UpdateNodeType(LLNodeType.Terminal);
						else if (0 > sid)
							UpdateNodeType(LLNodeType.EndNonTerminal);
						else
							UpdateNodeType(LLNodeType.NonTerminal);
						return true;
					}
					Panic();
					return true;
				}
				Panic();
				return true;
			}
			if(_endSymbolId!=Token.SymbolId)
			{
				Panic();
				return true;
			}
			return false;
		}
	}
}
