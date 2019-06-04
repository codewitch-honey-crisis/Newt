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
	abstract class LL1ParserBase : LLParser
	{
		int _symbolId = -1;
		int _errorId = -1;
		int _eosId = -1;
		LLNodeType _nodeType = LLNodeType.Initial;
		StringBuilder _lexerBuffer = new StringBuilder();
		
		protected LL1ParserBase(ParseContext parseContext = null) { ParseContext = parseContext; }
		protected int EosId { get { if (-1 == _eosId) _eosId = GetSymbolId("#EOS"); return _eosId; } }
		protected int ErrorId { get { if (-1 == _errorId) _errorId = GetSymbolId("#ERROR"); return _errorId; } }
		public override int Line => (LLNodeType.Error == _nodeType) ? ErrorToken.Line : Token.Line;
		public override int Column => (LLNodeType.Error == _nodeType) ? ErrorToken.Column : Token.Column;
		public override long Position => (LLNodeType.Error == _nodeType) ? ErrorToken.Position : Token.Position;

		protected ParseContext ParseContext { get; private set; }
		protected (int SymbolId, string Value, int Line, int Column, long Position, (int First, int Last)[] ExpectingRanges, int[] ExpectingSymbols) Token { get; private set; }
		protected (int SymbolId, string Value, int Line, int Column, long Position, (int First, int Last)[] ExpectingRanges, int[] ExpectingSymbols) ErrorToken { get; private set; }

		protected Stack<int> Stack { get; } = new Stack<int>();
		protected abstract bool IsHidden(int symbolId);
		protected abstract int Substitute(int symbolId);
		protected abstract bool IsCollapsed(int symbolId);
		protected abstract string GetBlockEnd(int symbolId);
		protected abstract Type GetType(int symbolId);

		protected abstract (int SymbolId, string Value, (int First, int Last)[] ExpectingRanges, int[] ExpectingSymbols) Lex(StringBuilder lexerBuffer);
		protected abstract bool ReadImpl();
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
			if(null!=ParseContext)
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
				switch(_nodeType)
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
	}
}
