
namespace Grimoire
{
	using System;
	using System.Collections.Generic;
	using System.Text;

#if GRIMOIRELIB
	public
#else
	internal
#endif
	class RuntimeLL1Parser : LLParserBase
	{
		CfGrammar _cfg;
		IDictionary<int, IDictionary<int, (int Id, int Left, int[] Right)>> _parseTable;
		FA _lexer;
		
		ICollection<int> _hiddenTerminals;
		IDictionary<int, int> _substitutions;
		IDictionary<int, string> _blockEnds;
		ICollection<int> _collapsedNonTerminals;
		IDictionary<int, Type> _terminalTypes;
		(int Accept, ((char First, char Last)[] Ranges, int Destination)[] Transitions, int[] PossibleAccepts)[] _lexTable;
		public RuntimeLL1Parser(
			CfGrammar cfg,
			FA lexer,
			ParseContext pc,
			IDictionary<int, IDictionary<int, (int RuleId, int Left, int[] Right)>> parseTable = null,
			(System.Int32 Accept, ((char First, char Last)[] Ranges, int Destination)[] Transitions, System.Int32[] PossibleAccepts)[] dfaTable = null)
		{
			_cfg = cfg;
			_lexer = lexer;
			_parseTable = parseTable ?? cfg.ToLL1ParseTable();
			_PopulateHiddenTerminals();
			_PopulateCollapsedNonTerminals();
			_PopulateSubstitutions();
			_PopulateBlockEnds();
			_PopulateTerminalTypes();

			_lexTable = dfaTable ?? lexer.ToDfaTable2<int>();
			Restart(pc);
		}

		public override object ParsedValue => ParserUtility.GetParsedValue(_terminalTypes.TryGetValue(SymbolId, null), Value);
		void _PopulateHiddenTerminals()
		{

			_hiddenTerminals = new HashSet<int>();

			foreach (var attrsym in _cfg.Attributes)
			{
				object hidden;

				if (attrsym.Value.TryGetValue("hidden", out hidden))
				{
					if (hidden is bool && ((bool)hidden))
					{
						var id = _cfg.GetSymbolId(attrsym.Key);
						_hiddenTerminals.Add(id);
					}
				}
			}
		}
		protected override bool IsHidden(int symId)
		{
			return _hiddenTerminals.Contains(symId);
		}
		void _PopulateCollapsedNonTerminals()
		{
			_collapsedNonTerminals = new HashSet<int>();

			foreach (var attrsym in _cfg.Attributes)
			{
				object collapsed;

				if (attrsym.Value.TryGetValue("collapse", out collapsed))
				{
					if (collapsed is bool && ((bool)collapsed))
					{
						var id = _cfg.GetSymbolId(attrsym.Key);
						_collapsedNonTerminals.Add(id);
					}
				}
			}
		}
		protected override bool IsCollapsed(int symId)
		{
			return _collapsedNonTerminals.Contains(symId);
		}
		void _PopulateBlockEnds()
		{

			_blockEnds = new Dictionary<int, string>();

			foreach (var attrsym in _cfg.Attributes)
			{
				object blockEnd;
				if (attrsym.Value.TryGetValue("blockEnd", out blockEnd))
				{
					var s = blockEnd as string;
					if (!string.IsNullOrEmpty(s))
					{
						var id = _cfg.GetSymbolId(attrsym.Key);
						_blockEnds.Add(id, s);
					}

				}
			}
		}
		protected override string GetBlockEnd(int symId)
		{
			string o;
			if(_blockEnds.TryGetValue(symId,out o))
				return o;
			return null;
		}
		void _PopulateTerminalTypes()
		{
			_terminalTypes = new Dictionary<int, Type>();
			foreach (var attrsym in _cfg.Attributes)
			{
				object type;
				if (attrsym.Value.TryGetValue("type", out type))
				{
					var id = _cfg.GetSymbolId(attrsym.Key);
					Type t = null;
					var s = type as string;
					if (!string.IsNullOrEmpty(s))
						_terminalTypes.Add(id, ParserUtility.ResolveType(s));
					else
					{
						t = type as Type;
						if (null != t)
							_terminalTypes.Add(id, t);
					}

				}
			}
		}
		protected override Type GetTerminalType(int symId)
		{
			Type o;
			if (_terminalTypes.TryGetValue(symId, out o))
				return o;
			return null;
		}

		void _PopulateSubstitutions()
		{

			_substitutions = new Dictionary<int, int>();

			foreach (var attrsym in _cfg.Attributes)
			{
				var id = _cfg.GetSymbolId(attrsym.Key);
				object sym;
				if (attrsym.Value.TryGetValue("substitute", out sym))
				{
					var id2 = _cfg.GetSymbolId(sym);
					_substitutions.Add(id, id2);
				}
			}
		}
		protected override int Substitute(int symId)
		{
			int o;
			if (_substitutions.TryGetValue(symId, out o))
				return o;
			return symId;
		}
		protected override (int Accept, string Value, (int First, int Last)[] ExpectingRanges, int[] ExpectingSymbols) Lex(ParseContext pc, StringBuilder lexerBuffer = null)
			=> FA.Lex2(_lexTable, -2, ParseContext, LexerBuffer);

		
		protected override bool ReadImpl()
		{
			if (LLNodeType.EndDocument == NodeType)
				return false;
			if (LLNodeType.Error == NodeType )
			{
				if (-1 == Token.SymbolId)
				{
					UpdateNodeType(LLNodeType.EndDocument);
					return true;
				}
			}
			if (null == Stack)
			{
				Stack = new Stack<int>();
				LexerBuffer = new StringBuilder();
				var ss = _cfg.StartSymbol;
				if (_cfg.NonTerminals.Contains(ss))
					UpdateNodeType(LLNodeType.NonTerminal);
				else
					UpdateNodeType(LLNodeType.Terminal);
				var sid = _cfg.GetSymbolId(ss);
				Stack.Push(sid);
				UpdateSymbolId(sid);
				NextToken();
				return true;
			}
			if (0 < Stack.Count)
			{
				var symId = Stack.Peek();
				
				UpdateSymbolId(symId);
				if (-4 > symId) // close symbol
				{
					_DoPop();
					return true;
				}
				if (Token.SymbolId == symId) // terminal
				{
					NextToken();
					_DoPop();
					return true;
				}
				else // non-terminal
				{
					IDictionary<int, (int Id, int Left, int[] Right)> d;
					if (_parseTable.TryGetValue(Stack.Peek(), out d))
					{
						(int Id, int Left, int[] Right) t;
						if (d.TryGetValue(Token.SymbolId, out t))
						{
							_DoPop();
							var c = t.Right.Length;
							var ci = (~symId) - 4;
							Stack.Push(ci); // close symbol id

							// push the rule symbols
							for (int i = c - 1; i > -1; --i)
								if (-3 != t.Right[i]) // skip epsilons
									Stack.Push(t.Right[i]);
							var s = Stack.Peek();
							UpdateSymbolId(s);
							if (Token.SymbolId == SymbolId)
								UpdateNodeType(LLNodeType.Terminal);
							else if (-4 > s)
								UpdateNodeType(LLNodeType.EndNonTerminal);
							else
								UpdateNodeType(LLNodeType.NonTerminal);
							return true;
						}
						else
						{
							Panic();
							return true;
						}
					}
					else
					{
						Panic();
						return true;
						// ThrowExpecting(_stack.Peek(), _parseTable.Keys, _pc.Line, _pc.Column, _pc.Position);
					}
				}
			}
			if (-1 != Token.SymbolId)
			{
				Panic();
				return true;
				//ThrowExpecting(_tok.SymbolId, new int[] { -1 }, _tok.Line, _tok.Column, _tok.Position);
			}
			return false;
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
				else if (-4 > s)
					UpdateNodeType(LLNodeType.EndNonTerminal);
				else
					UpdateNodeType(LLNodeType.NonTerminal);
			}
			else
			{
				UpdateSymbolId(-1);
				UpdateNodeType(LLNodeType.EndDocument);
			}
		}
		
		public override object GetSymbolById(int id)
		{
			return _cfg.GetSymbolById(id);
		}
	}
}
