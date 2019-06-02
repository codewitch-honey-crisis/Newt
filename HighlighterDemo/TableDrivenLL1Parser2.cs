
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
	class TableDrivenLL1Parser2 : LLParserBase
	{
		public TableDrivenLL1Parser2(
			(int RuleId, int Left, int[] Right)[][] parseTable,
			(int SymbolId, bool IsNonTerminal) startingConfiguration,
			(int Accept, ((char First, char Last)[] Ranges, int Destination)[] Transitions, int[] PossibleAccepts)[] lexTable,
			object[] symbols,
			int[] substitutionsAndHiddenTerminals,
			string[] blockEnds,
			int[] collapsedNonTerminals,
			Type[] terminalTypes,
			ParseContext context)
		{

			SubstitutionsAndHiddenTerminals = substitutionsAndHiddenTerminals;
			CollapsedNonTerminals = collapsedNonTerminals;
			Symbols = symbols;
			ParseTable = parseTable;
			StartingConfiguration = startingConfiguration;
			LexTable = lexTable;
			BlockEnds = blockEnds;
			TerminalTypes = terminalTypes;
			Restart(context);
		}

		protected (int Accept, ((char First, char Last)[] Ranges, int Destination)[] Transitions, int[] PossibleAccepts)[] LexTable { get; }
		protected (int SymbolId, bool IsNonTerminal) StartingConfiguration { get; }
		protected (int RuleId, int Left, int[] Right)[][] ParseTable { get; }
		protected int[] SubstitutionsAndHiddenTerminals { get; }
		protected string[] BlockEnds { get; }
		protected int[] CollapsedNonTerminals { get; } // we may end up holding other things from attributes here too eventually
		protected Type[] TerminalTypes { get; }
		protected override bool IsHidden(int symId)
		{
			if (symId == -1) symId = Symbols.Length - 1;
			if (0 > symId || SubstitutionsAndHiddenTerminals.Length <= symId)
				return false;
			return -2 == SubstitutionsAndHiddenTerminals[symId];
		}
		protected override int Substitute(int symId)
		{

			if (symId == -1) symId = Symbols.Length - 1;
			if (0 > symId) return symId;
			var result = SubstitutionsAndHiddenTerminals[symId];
			if (-2 == result) return symId;
			return result;
		}
		protected override string GetBlockEnd(int symId)
		{
			if (0 > symId || BlockEnds.Length <= symId) return null;
			return BlockEnds[symId];
		}
		protected override bool IsCollapsed(int symId)
		{
			if (0 > symId || CollapsedNonTerminals.Length <= symId) return false;
			return null != CollapsedNonTerminals && -3 == CollapsedNonTerminals[symId];
		}
		protected override Type GetTerminalType(int symId)
		{
			if (0 > symId || TerminalTypes.Length <= symId) return null;
			return TerminalTypes[symId];
		}
		public object[] Symbols { get; }


		public override object GetSymbolById(int symbolId)
		{
			if (null == Symbols) return symbolId;
			if (-1 == symbolId)
				return Symbols[Symbols.Length - 1];
			if (-2 == symbolId)
				return "#ERROR";
			return Symbols[symbolId];
		}

		protected override (int Accept, string Value, (int First, int Last)[] ExpectingRanges, int[] ExpectingSymbols) Lex(ParseContext pc, StringBuilder lexerBuffer = null)
			=> ParserUtility.Lex2(LexTable, -2, ParseContext, LexerBuffer);

		protected override bool ReadImpl()
		{
			if (LLNodeType.EndDocument == NodeType)
				return false;
			if (LLNodeType.Error == NodeType)
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
				var sid = StartingConfiguration.SymbolId;
				if (StartingConfiguration.IsNonTerminal)
					UpdateNodeType(LLNodeType.NonTerminal);
				else
					UpdateNodeType(LLNodeType.Terminal);
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
					(int Id, int Left, int[] Right)[] d;
					var s = Stack.Peek();
					if (-1 < s && s < ParseTable.Length)
						d = ParseTable[Stack.Peek()];
					else
						d = null;
					if (null!=d)
					{
						var tid = Token.SymbolId;
						if (-1 == tid)
							tid = d.Length - 1;
						(int Id, int Left, int[] Right) t = (-1, -1, null);
						if(-1<tid && tid<d.Length) t=d[tid];
						if (!(0>t.Id))
						{
							_DoPop();
							var c = t.Right.Length;
							var ci = (~symId) - 4;
							Stack.Push(ci); // close symbol id

							// push the rule symbols
							for (int i = c - 1; i > -1; --i)
								if (-3 != t.Right[i]) // skip epsilons
									Stack.Push(t.Right[i]);
							s = Stack.Peek();
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

	}
}
