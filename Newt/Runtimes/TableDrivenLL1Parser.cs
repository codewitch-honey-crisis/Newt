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
	class TableDrivenLL1Parser : LLParser
	{
		int _symbolId = -1;
		int _errorId = -1;
		int _eosId = -1;
		LLNodeType _nodeType = LLNodeType.Initial;
		StringBuilder _lexerBuffer = new StringBuilder();

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
		protected int EosId { get { if (-1 == _eosId) _eosId = GetSymbolId("#EOS"); return _eosId; } }
		protected int ErrorId { get { if (-1 == _errorId) _errorId = GetSymbolId("#ERROR"); return _errorId; } }

		protected (int Accept, ((char First, char Last)[] Ranges, int Destination)[] Transitions, int[] PossibleAccepts)[] LexTable { get; }
		protected (int SymbolId, bool IsNonTerminal,int NonTerminalCount) StartingConfiguration { get; }
		protected (int Left, int[] Right)[][] ParseTable { get; }
		protected int[] SubstitutionsAndHiddenTerminals { get; }
		protected string[] BlockEnds { get; }
		protected int[] CollapsedNonTerminals { get; } // we may end up holding other things from attributes here too eventually
		protected Type[] TerminalTypes { get; }
		public override int Line => (LLNodeType.Error == _nodeType) ? ErrorToken.Line : Token.Line;
		public override int Column => (LLNodeType.Error == _nodeType) ? ErrorToken.Column : Token.Column;
		public override long Position => (LLNodeType.Error == _nodeType) ? ErrorToken.Position : Token.Position;

		protected ParseContext ParseContext { get; private set; }
		protected (int SymbolId, string Value, int Line, int Column, long Position, (int First, int Last)[] ExpectingRanges, int[] ExpectingSymbols) Token { get; private set; }
		protected (int SymbolId, string Value, int Line, int Column, long Position, (int First, int Last)[] ExpectingRanges, int[] ExpectingSymbols) ErrorToken { get; private set; }

		protected Stack<int> Stack { get; } = new Stack<int>();
		protected bool IsHidden(int symId)
		{
			if (0 > symId || SubstitutionsAndHiddenTerminals.Length <= symId)
				return false;
			return -2 == SubstitutionsAndHiddenTerminals[symId];
		}
		protected int Substitute(int symId)
		{
			if (0 > symId || SubstitutionsAndHiddenTerminals.Length <= symId)
				return symId;
			var result = SubstitutionsAndHiddenTerminals[symId];
			if (-2 == result) return symId;
			return result;
		}
		protected string GetBlockEnd(int symId)
		{
			if (0 > symId || BlockEnds.Length <= symId) return null;
			return BlockEnds[symId];
		}
		protected bool IsCollapsed(int symId)
		{
			if (0 > symId || CollapsedNonTerminals.Length <= symId) return false;
			return null != CollapsedNonTerminals && -3 == CollapsedNonTerminals[symId];
		}
		protected Type GetType(int symId)
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
		protected (int SymbolId, string Value, (int First, int Last)[] ExpectingRanges, int[] ExpectingSymbols) Lex(StringBuilder lexerBuffer = null)
			=> ParserUtility.Lex2(LexTable, ErrorId, ParseContext, lexerBuffer);
		public override bool Read()
		{
			var result = ReadImpl();
			// this is a big part of the "magic" behind clean parse trees
			// all it does is skip "collapsed" nodes in the parse tree
			// meaning any symbol with a "collapse" attribute
			while (result && IsCollapsed(SymbolId))
				result = ReadImpl();
			return result;
		}
		protected bool ReadImpl()
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
					(int Left, int[] Right) rule = (-1, null);
					if(-1<tid && tid<d.Length)
						rule = d[tid];
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
			if (EosId!= Token.SymbolId)
			{
				Panic();
				return true;
			}
			return false;
		}
		protected void UpdateNodeType(LLNodeType nodeType) { _nodeType = nodeType; }
		protected void UpdateSymbolId(int symbolId) { _symbolId = symbolId; }
		protected void NextToken()
		{
			while (true)
			{
				if (-1 == ParseContext.Current)
				{
					Token = (SymbolId: EosId, Value: null, Line: ParseContext.Line, Column: ParseContext.Column, Position: ParseContext.Position, null, null);
					break;
				}
				else
				{
					long pos = ParseContext.Position;
					int l = ParseContext.Line;
					int c = ParseContext.Column;
					var t = Lex(_lexerBuffer);
					Token = (SymbolId: t.SymbolId, Value: t.Value, Line: l, Column: c, Position: pos, t.ExpectingRanges, t.ExpectingSymbols);

				}
				string blockEnd = GetBlockEnd(Token.SymbolId);
				if (null != blockEnd)
				{
					var l = ParseContext.CaptureBuffer.Length;
					if (!ParseContext.TryReadUntil(blockEnd))
						ParseContext.Expecting();
					Token = (Token.SymbolId, Token.Value + ParseContext.GetCapture(l), Token.Line, Token.Column, Token.Position, Token.ExpectingRanges, Token.ExpectingSymbols);
				}
				if (!IsHidden(Token.SymbolId))
					break;
			}
		}
		public override void Restart(ParseContext parseContext)
		{
			Stack.Clear();
			UpdateNodeType(LLNodeType.Initial);
			if (null != ParseContext)
				ParseContext.Close();
			ParseContext = parseContext;
		}
		public override void Close()
		{
			if (null != ParseContext)
				ParseContext.Close();
			UpdateNodeType(LLNodeType.EndDocument);
		}
		public override int SymbolId {
			get {
				if (LLNodeType.Error == _nodeType)
					return ErrorId;
				return (0 > _symbolId) ? ~_symbolId : _symbolId;
			}
		}
		public override string Value {
			get {
				switch (_nodeType)
				{
					case LLNodeType.Terminal:
						return Token.Value;
					case LLNodeType.Error:
						return ErrorToken.Value;
				}
				return null;
			}
		}
		public override object ParsedValue {
			get {
				if (LLNodeType.Terminal == NodeType)
				{
					Type t = GetType(Token.SymbolId);
					if (null == t) return Value;
					return ParserUtility.GetParsedValue(t, Value);
				}
				return Value;
			}
		}
		public override LLNodeType NodeType => _nodeType;

		protected bool Panic()
		{
			UpdateNodeType(LLNodeType.Error);
			var l = ParseContext.Line;
			var c = ParseContext.Column;
			var pos = ParseContext.Position;
			ErrorToken = (ErrorId, "", l, c, pos, ErrorToken.ExpectingRanges, ErrorToken.ExpectingSymbols);
			var sb = new StringBuilder();
			bool first = true;
			while (first || (!Stack.Contains(Token.SymbolId) && -1 != ParseContext.Current))
			{
				first = false;
				ErrorToken = (ErrorId, string.Concat(ErrorToken.Value, Token.Value), l, c, pos, Token.ExpectingRanges, Token.ExpectingSymbols);
				if (-1 == ParseContext.Current)
				{
					Token = (SymbolId: EosId, Value: "", Line: ParseContext.Line, Column: ParseContext.Column, Position: ParseContext.Position, null, null);
				}
				else
				{
					var t = Lex(_lexerBuffer);
					Token = (SymbolId: t.SymbolId, Value: t.Value, Line: l, Column: c, Position: pos, t.ExpectingRanges, t.ExpectingSymbols);
				}
			}
			while (Stack.Contains(Token.SymbolId) && Stack.Peek() != Token.SymbolId)
				Stack.Pop();

			if (0 < Stack.Count && Stack.Peek() == Token.SymbolId)
			{
				if (IsHidden(Token.SymbolId))
					NextToken();
				return true;
			}
			if (IsHidden(Token.SymbolId))
				NextToken();
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
