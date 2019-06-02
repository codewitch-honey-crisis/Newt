using System;
using System.Collections.Generic;
using System.Text;

namespace Grimoire
{
	class TableDrivenLL1Parser : LL1ParserBase
	{
		public TableDrivenLL1Parser(
				(int Left, int[] Right)[][] parseTable,
				(int SymbolId, bool IsNonTerminal,int NonTerminalCount) startingConfiguration,
				(int Accept, ((char First, char Last)[] Ranges, int Destination)[] Transitions, int[] PossibleAccepts)[] lexTable,
				string[] symbols,
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
		protected (int SymbolId, bool IsNonTerminal,int NonTerminalCount) StartingConfiguration { get; }
		protected (int Left, int[] Right)[][] ParseTable { get; }
		protected int[] SubstitutionsAndHiddenTerminals { get; }
		protected string[] BlockEnds { get; }
		protected int[] CollapsedNonTerminals { get; } // we may end up holding other things from attributes here too eventually
		protected Type[] TerminalTypes { get; }
		protected override bool IsHidden(int symId)
		{
			if (0 > symId || SubstitutionsAndHiddenTerminals.Length <= symId)
				return false;
			return -2 == SubstitutionsAndHiddenTerminals[symId];
		}
		protected override int Substitute(int symId)
		{
			if (0 > symId || SubstitutionsAndHiddenTerminals.Length <= symId)
				return symId;
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
		protected override Type GetType(int symId)
		{
			if (0 > symId || TerminalTypes.Length <= symId) return null;
			return TerminalTypes[symId];
		}
		public string[] Symbols { get; }


		public override string GetSymbolById(int symbolId)
		{
			if (null == Symbols) return null;
			if (0 > symbolId || Symbols.Length <= symbolId)
				return null;
			return Symbols[symbolId];
		}
		public override int GetSymbolId(string symbol)
		{
			if(null!=Symbols)
				for(var i = 0; i <Symbols.Length;i++)
					if (Equals(symbol, Symbols[i]))
						return i;
			return -1;
		}
		protected override (int SymbolId, string Value, (int First, int Last)[] ExpectingRanges, int[] ExpectingSymbols) Lex(StringBuilder lexerBuffer = null)
			=> ParserUtility.Lex2(LexTable, -2, ParseContext, lexerBuffer);

		protected override bool ReadImpl()
		{
			if (LLNodeType.Error == NodeType && -1 == ParseContext.Current) return false;
			if (NodeType == LLNodeType.Initial)
			{
				var sid = StartingConfiguration.SymbolId;
				Stack.Push(sid);
				if (StartingConfiguration.IsNonTerminal)
					UpdateNodeType(LLNodeType.NonTerminal);
				else
					UpdateNodeType(LLNodeType.Terminal);
				UpdateSymbolId(sid);
				NextToken();
				return true;
			}
			if (0 < Stack.Count)
			{
				var sid = Stack.Peek();
				UpdateSymbolId(sid);
				if (0 > sid) // end non-terminal
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
				(int Left, int[] Rule)[] d = ParseTable[sid];
				if (null!=d)
				{
					var tid = Token.SymbolId - StartingConfiguration.NonTerminalCount;
					(int Left, int[] Right) rule = d[tid];
					if (-1!=rule.Left)
					{
						_DoPop();
						sid = ~sid;
						Stack.Push(sid);
						for (int j = rule.Right.Length - 1; j >= 0; --j)
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
			if (GetSymbolId("#EOS")!= Token.SymbolId)
			{
				Panic();
				return true;
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
				else if (0 > s)
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
