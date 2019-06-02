namespace Grimoire
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Text;
#if GRIMOIRELIB
	public
#else
	internal
#endif
	enum EbnfErrorLevel
	{
		Message = 0,
		Warning = 1,
		Error = 2
	}
#if GRIMOIRELIB
	public
#else
	internal
#endif
	sealed class EbnfMessage
	{
		public EbnfMessage(EbnfErrorLevel errorLevel,int errorCode,string message,int line, int column,long position)
		{
			ErrorLevel = errorLevel;
			ErrorCode = errorCode;
			Message = message;
			Line = line;
			Column = column;
			Position = position;
		}
		public EbnfErrorLevel ErrorLevel { get; private set; }
		public int ErrorCode { get; private set; }
		public string Message { get; private set; }
		public int Line { get; private set; }
		public int Column { get; private set; }
		public long Position { get; private set; }

		public override string ToString()
		{
			if (-1 == Position)
				return string.Format("{0}: {1} code {2}",
					ErrorLevel, Message, ErrorCode
					);
			else
				return string.Format("{0}: {1} code {2} at line {3}, column {4}, position {5}",
					ErrorLevel, Message, ErrorCode, Line, Column, Position
					);
		}
	}
#if GRIMOIRELIB
	public
#else
	internal
#endif
	sealed class EbnfException : Exception
	{
		public IList<EbnfMessage> Messages { get; }
		public EbnfException(string message,int errorCode=-1,int line=0,int column=0,long position=-1) : 
			this(new EbnfMessage[] { new EbnfMessage(EbnfErrorLevel.Error, errorCode, message, line, column, position)})
		{}
		static string _FindMessage(IEnumerable<EbnfMessage> messages)
		{
			var l = new List<EbnfMessage>(messages);
			if (null == messages) return "";
			int c = 0;
			foreach(var m in l)
			{
				if(EbnfErrorLevel.Error==m.ErrorLevel)
				{
					if(1==l.Count) 
						return m.ToString();
					return string.Concat(m, " (multiple messages)");
				}
				++c;
			}
			foreach (var m in messages)
				return m.ToString();
			return "";
		}
		public EbnfException(IEnumerable<EbnfMessage> messages) : base(_FindMessage(messages))
		{
			Messages = new List<EbnfMessage>(messages);
		}
		public static void ThrowIfErrors(IEnumerable<EbnfMessage> messages)
		{
			if (null == messages) return;
			foreach(var m in messages)
				if(EbnfErrorLevel.Error==m.ErrorLevel)
					throw new EbnfException(messages);
		}
	}
#if GRIMOIRELIB
	public
#else
	internal
#endif
	class EbnfDocument
	{
		public object StartSymbol {
			get {
				foreach (var attrs in Attributes)
				{
					object o;
					if (attrs.Value.TryGetValue("start", out o))
					{
						if (o is bool && ((bool)o))
							return attrs.Key;
					}
				}
				return null;
			}
			set {
				foreach (var attrs in Attributes)
					attrs.Value.Remove("start");
				IDictionary<string, object> d;
				if (!Attributes.TryGetValue(value, out d))
				{
					d = new Dictionary<string, object>();
					Attributes.Add(value, d);
				}
				d.Add("start", true);
			}
		}
		public IDictionary<object, EbnfProduction> Productions { get; } = new Dictionary<object, EbnfProduction>();
		public IDictionary<object, IDictionary<string, object>> Attributes { get; } = new Dictionary<object, IDictionary<string, object>>();

		public override bool Equals(object obj)
		{
			return Equals(obj as EbnfDocument);
		}
		public bool Equals(EbnfDocument rhs)
		{
			if (ReferenceEquals(this, rhs)) return true;
			if (null == rhs) return false;

			
			if (Productions.Count != rhs.Productions.Count)
				return false;
			if (Attributes.Count != rhs.Attributes.Count)
				return false;

			foreach(var prod in Productions)
			{
				EbnfProduction rprod;
				if (!rhs.Productions.TryGetValue(prod.Key, out rprod))
					return false;
				if (!Equals(prod.Value, rprod))
					return false;
			}
			return true;
		}
		public override int GetHashCode()
		{
			var result = 0;
			foreach(var prod in Productions)
			{
				result ^= (null != prod.Key) ? prod.Key.GetHashCode() : 0;
				result ^= (null != prod.Value) ? prod.Value.GetHashCode() : 0;
			}
			return result;
		}
		private void _ValidateExpression(EbnfExpression expr, IDictionary<object,int> refCounts,IList<EbnfMessage> messages)
		{
			var l = expr as EbnfLiteralExpression;
			if(null!=l)
			{

				var i = GetIdForExpression(l);
				// don't count itself. only things just like itself
				if(null!=i && !ReferenceEquals(Productions[i].Expression,l))
					refCounts[i] += 1;
			}
			var rx = expr as EbnfRegexExpression;
			if (null != rx)
			{
				var i = GetIdForExpression(rx);
				if (null != i && !ReferenceEquals(Productions[i].Expression, l))
					refCounts[i] += 1;
			}
			var r = expr as EbnfRefExpression;
			if(null!=r)
			{
				int rc;
				if (null == r.Id)
				{
					messages.Add(
						new EbnfMessage(
							EbnfErrorLevel.Error, 4,
							"Null reference expression",
							expr.Line, expr.Column, expr.Position));
					return;
				}
				if (!refCounts.TryGetValue(r.Id,out rc))
				{
					messages.Add(
						new EbnfMessage(
							EbnfErrorLevel.Error, 1,
							string.Concat(
								"Reference to undefined symbol \"",
								r.Id,
								"\""),
							expr.Line, expr.Column, expr.Position));
					return;
				}
				refCounts[r.Id] = rc + 1;
				return;
			}
			var b = expr as EbnfBinaryExpression;
			if(null!=b)
			{
				if(null==b.Left && null==b.Right)
				{
					messages.Add(
						new EbnfMessage(
							EbnfErrorLevel.Warning, 3,
								"Nil expression",
							expr.Line, expr.Column, expr.Position));
					return;
				}
				_ValidateExpression(b.Left,refCounts,messages);
				_ValidateExpression(b.Right, refCounts, messages);
				return;
			}
			var u = expr as EbnfUnaryExpression;
			if(null!=u)
			{
				if (null == u.Expression)
				{
					messages.Add(
						new EbnfMessage(
							EbnfErrorLevel.Warning, 3,
								"Nil expression",
							expr.Line, expr.Column, expr.Position));
					return;
				}
				_ValidateExpression(u.Expression, refCounts, messages);
			}
		}
		public IList<EbnfMessage> Validate(bool throwIfErrors=false)
		{
			var result = new List<EbnfMessage>();
			var refCounts = new Dictionary<object, int>(EqualityComparer<object>.Default);

			foreach(var prod in Productions)
				refCounts.Add(prod.Key, 0);
			foreach (var prod in Productions)
			{
				_ValidateExpression(prod.Value.Expression, refCounts, result);
			}
			foreach(var rc in refCounts)
			{

				if(0==rc.Value && !Attributes.ContainsKey(rc.Key))
				{
					var prod = Productions[rc.Key]; 
					result.Add(new EbnfMessage(EbnfErrorLevel.Warning, 2, string.Concat("Unreferenced production \"", rc.Key, "\""),
						prod.Line,prod.Column,prod.Position
						));
				}
			}
			foreach(var sattrs in Attributes)
			{
				if(!refCounts.ContainsKey(sattrs.Key))
				{
					result.Add(new EbnfMessage(EbnfErrorLevel.Warning, 4, string.Concat("Orphaned attribute \"", sattrs.Key, "\" has no matching production."),
						0, 0, -1
						));
				}
			}
			if (throwIfErrors)
				EbnfException.ThrowIfErrors(result);
			return result;

		}
		public object GetIdForExpression(EbnfExpression expression)
		{
			foreach (var prod in Productions)
				if (Equals(prod.Value.Expression ,expression))
					return prod.Key;
			return null;
		}
		public object GetUniqueId()
		{
			if (0 == Productions.Count)
			{
				return "implicit1";
			}
			Type type = Productions.Keys.InferElementType();
			if (typeof(object) == type) return new object();
			else if (typeof(int) == type)
			{
				int i = 0;
				while (Productions.ContainsKey(i))
					++i;
				return i;
			}
			else if (typeof(string) == type)
			{
				int i = 1;
				while (Productions.ContainsKey(string.Concat("implicit", i)))
					++i;
				return string.Concat("implicit", i);
			}
			else
				throw new NotSupportedException("The symbol type cannot be used to perform this operation.");
		}
		public LLParser ToLL1Parser(ParseContext parseContext = null)
		{
			var cfg = ToCfGrammar();
			cfg.PrepareLL1();
			var lexer = ToLexer(cfg);
			return cfg.ToLL1Parser(lexer, parseContext);
		}
		public CfGrammar ToCfGrammar()
		{
			var result = new CfGrammar();

			foreach (var prod in new List<KeyValuePair<object, EbnfProduction>>(Productions))
			{

				// give the productions an opportunity to augment the grammar.
				prod.Value.Expression.AugmentGrammar(prod.Key, result);
			}
			foreach (var prod in new List<KeyValuePair<object, EbnfProduction>>(Productions))
			{
				if (prod.Value.Expression.IsNonTerminal)
				{
					foreach (var n in prod.Value.Expression.ToNonTerminal(this,result))
					{
						CfgRule r = new CfgRule();
						r.Left = prod.Key;

						foreach (var s in n)
						{

							//if (1 < r.Right.Count && null == s)
							//	continue;
							r.Right.Add(s);
						}
						if(!result.Rules.Contains(r))
							result.Rules.Add(r);
					}
				}
			}
			foreach (var attrs in Attributes)
			{
				var d = new Dictionary<string, object>();
				result.Attributes.Add(attrs.Key, d);
				foreach (var nvp in attrs.Value)
				{
					d.Add(nvp.Key, nvp.Value);
				}
			}
			return result;
		}
		
		public FA ToLexer(CfGrammar cfg)
		{
			// passing null to cfg can lead to confusing errors in the parse 
			// so it's not optional. Specifying null is still possible
			if (null == cfg)
				cfg = ToCfGrammar();
			var fas = new List<FA>();
			foreach (var prod in Productions)
			{
				FA fa = null;
				if (!prod.Value.Expression.IsNonTerminal)
				{
					var l = prod.Value.Expression as EbnfLiteralExpression;
					if (null != l)
						fa = FA.Literal(l.Literal, cfg.GetSymbolId(prod.Key));
					var r = prod.Value.Expression as EbnfRegexExpression;
					if (null != r)
						fa = FA.Parse(r.Regex, cfg.GetSymbolId(prod.Key));
					if (null == fa)
						throw new NotSupportedException("The expression is not supported.");

					fas.Add(fa);
				}

			}
			return FA.Lexer(fas);
		}
		public override string ToString()
		{
			var sb = new StringBuilder();
			foreach (var prod in Productions)
			{
				sb.Append(prod.Key);
				IDictionary<string, object> d;
				if (Attributes.TryGetValue(prod.Key, out d))
				{
					if (0 < d.Count)
					{
						sb.Append('<');
						var delim = "";
						foreach (var attr in d)
						{
							sb.Append(delim);
							sb.Append(attr.Key);
							_AppendAttrVal(attr.Value, sb);
							delim = ", ";
						}
						sb.Append('>');
					}
				}
				sb.AppendLine(prod.Value.ToString());
			}
			return sb.ToString();
		}
		void _AppendAttrVal(object value, StringBuilder sb)
		{
			if (value is bool)
			{
				if (!(bool)value)
				{
					sb.Append("=false");
				}
			}
			else if (value is string)
			{

				sb.Append("=\"");
				sb.Append(((string)value).Replace("\"", "\\\""));
				sb.Append('\"');
			}
			else if (value is char)
			{
				sb.Append("=\"");
				sb.Append(Convert.ToString(value).Replace("\"", "\\\""));
				sb.Append('\"');
			}
			else
			{
				sb.Append('=');
				sb.Append(value);
			}
		}

		
		public static EbnfDocument ReadFrom(string filename)
		{
			using (StreamReader sr = new StreamReader(filename))
				return ReadFrom(sr);
		}

		public static EbnfDocument ReadFrom(TextReader reader)
			=> Parse(ParseContext.Create(reader));

		public static EbnfDocument Parse(IEnumerable<char> @string)
			=> Parse(ParseContext.Create(@string));
		public static EbnfDocument Parse(ParseContext pc)
		{
			var doc = new EbnfDocument();
			while (-1 != pc.Current)
			{
				_ParseProduction(doc, pc);
				pc.TrySkipCCommentsAndWhiteSpace();
			}
			return doc;
		}
		static void _ParseProduction(EbnfDocument doc, ParseContext pc)
		{
		
			pc.TrySkipCCommentsAndWhiteSpace();
			var line = pc.Line;
			var column = pc.Column;
			var position = pc.Position;
			var id = _ParseIdentifier(pc);
			pc.TrySkipCCommentsAndWhiteSpace();
			if ('<' == pc.Current)
			{
				_ParseAttributes(doc, id, pc);
				pc.TrySkipCCommentsAndWhiteSpace();
			}
			pc.Expecting('=');
			pc.Advance();
			pc.Expecting();
			var expr = _ParseExpression(doc, pc);
			pc.TrySkipCCommentsAndWhiteSpace();
			pc.Expecting(';');
			pc.Advance();
			pc.TrySkipCCommentsAndWhiteSpace();
			EbnfProduction prodPrev;
			// transform this into an OrExpression with the previous
			if (doc.Productions.TryGetValue(id, out prodPrev))
			{
				doc.Productions[id].Expression = new EbnfOrExpression(prodPrev.Expression, expr);
			}
			else
			{
				var prod = new EbnfProduction(expr);
				prod.SetPositionInfo(line, column, position);
				doc.Productions.Add(id, prod);

			}
		}
		static EbnfExpression _ParseExpression(EbnfDocument doc, ParseContext pc)
		{
			EbnfExpression current = null;
			EbnfExpression e;
			long position;
			int line;
			int column;
			pc.TrySkipCCommentsAndWhiteSpace();
			position = pc.Position; line = pc.Line; column = pc.Column;
			while (-1 != pc.Current && ']' !=pc.Current && ')' != pc.Current && '}' != pc.Current && ';' != pc.Current)
			{
				pc.TrySkipCCommentsAndWhiteSpace();
				position = pc.Position; line = pc.Line; column = pc.Column;
				switch (pc.Current)
				{
					case '|':
						pc.Advance();
						current = new EbnfOrExpression(current, _ParseExpression(doc, pc));
						current.SetPositionInfo(line, column, position);
						break;
					case '(':
						pc.Advance();
						e = _ParseExpression(doc, pc);
						current.SetPositionInfo(line, column, position);
						pc.Expecting(')');
						pc.Advance();
						e.SetPositionInfo(line, column, position);
						if (null == current)
							current = e;
						else 
							current = new EbnfConcatExpression(current, e);
						
						break;
					case '[':
						pc.Advance();
						e = new EbnfOptionalExpression(_ParseExpression(doc, pc));
						e.SetPositionInfo(line, column, position);
						pc.TrySkipCCommentsAndWhiteSpace();
						pc.Expecting(']');
						pc.Advance();
						if (null == current)
							current = e;
						else
							current = new EbnfConcatExpression(current, e);
						
						break;
					case '{':
						pc.Advance();
						e = new EbnfRepeatExpression(_ParseExpression(doc, pc));
						e.SetPositionInfo(line, column, position);
						pc.TrySkipCCommentsAndWhiteSpace();
						pc.Expecting('}');
						pc.Advance();
						if (null == current)
							current = e;
						else
							current = new EbnfConcatExpression(current, e);

						break;
					case '\"':
						e = new EbnfLiteralExpression(pc.ParseJsonString());
						if (null == current)
							current = e;
						else
							current = new EbnfConcatExpression(current, e);
						e.SetPositionInfo(line, column, position);
						break;
						
					case '\'':
						pc.Advance();
						pc.ClearCapture();
						pc.TryReadUntil('\'', '\\', false);
						pc.Expecting('\'');
						pc.Advance();
						e = new EbnfRegexExpression(pc.Capture);
						if (null == current)
							current = e;
						else
							current = new EbnfConcatExpression(current, e);
						e.SetPositionInfo(line, column, position);
						break;
					case ';':
					case ']':
					case ')':
					case '}':
						return current;
					
					default:
						e = new EbnfRefExpression(_ParseIdentifier(pc));
						if (null == current)
							current = e;
						else
							current = new EbnfConcatExpression(current, e);
						e.SetPositionInfo(line, column, position);
						break;
				}
			}
			pc.TrySkipCCommentsAndWhiteSpace();
			return current;
		}
		static void _ParseAttribute(EbnfDocument doc, string id, ParseContext pc)
		{
			pc.TrySkipCCommentsAndWhiteSpace();
			var attrid = _ParseIdentifier(pc);
			pc.TrySkipCCommentsAndWhiteSpace();
			pc.Expecting('=', '>', ',');
			object val = true;
			if ('=' == pc.Current)
			{
				pc.Advance();
				val = pc.ParseJsonValue();
			}
			pc.Expecting(',', '>');
			IDictionary<string, object> d;
			if (!doc.Attributes.TryGetValue(id, out d))
			{
				d = new Dictionary<string, object>();
				doc.Attributes.Add(id, d);
			}
			d[attrid] = val;
			pc.TrySkipCCommentsAndWhiteSpace();
		}
		static void _ParseAttributes(EbnfDocument doc, string id, ParseContext pc)
		{
			pc.TrySkipCCommentsAndWhiteSpace();
			pc.Expecting('<');
			pc.Advance();
			while (-1 != pc.Current && '>' != pc.Current)
			{
				_ParseAttribute(doc, id, pc);
				pc.TrySkipCCommentsAndWhiteSpace();
				pc.Expecting(',', '>');
				if (',' == pc.Current)
					pc.Advance();
			}
			pc.Expecting('>');
			pc.Advance();
			pc.TrySkipCCommentsAndWhiteSpace();
		}
		static string _ParseIdentifier(ParseContext pc)
		{
			pc.TrySkipCCommentsAndWhiteSpace();
			if (-1 == pc.Current)
			{
				pc.Expecting();
				return null;
			}
			var l = pc.CaptureBuffer.Length;
			if ('_' != pc.Current && !char.IsLetter((char)pc.Current))
				pc.Expecting("ABCDEFGHIJKLMNOPQRSTUVWXYZ_abcdefghijklmnopqrstuvwxyz".ToCharArray().Convert<int>().ToArray());
			pc.CaptureCurrent();
			while (-1 != pc.Advance() && ('_' == pc.Current || '-' == pc.Current || char.IsLetterOrDigit((char)pc.Current)))
				pc.CaptureCurrent();
			pc.TrySkipCCommentsAndWhiteSpace();
			return pc.GetCapture(l);
		}
	}
#if GRIMOIRELIB
	public
#else
	internal
#endif
	class EbnfProduction
	{
		public EbnfProduction() { }
		public EbnfProduction(EbnfExpression expression)
		{
			Expression = expression;
		}
		public EbnfExpression Expression { get; set; } = null;
		public void SetPositionInfo(int line, int column, long position)
		{
			Line = line;
			Column = column;
			Position = position;
		}
		public int Line { get; private set; }
		public int Column { get; private set; }
		public long Position { get; private set; }

		public override string ToString()
		{
			if (null != Expression)
			{
				return string.Concat("= ", Expression, ";");
			}
			else
				return "= ;";
		}
		public override bool Equals(object obj)
		{
			return base.Equals(obj as EbnfProduction);
		}
		public bool Equals(EbnfProduction rhs)
		{
			return Equals(Expression, rhs.Expression);
		}
		public override int GetHashCode()
		{
			return (null!=Expression)?Expression.GetHashCode():0;
		}
	}
#if GRIMOIRELIB
	public
#else
	internal
#endif
	abstract class EbnfExpression
	{
		public abstract bool IsNonTerminal { get; }
		public abstract ICollection<IList<object>> ToNonTerminal(EbnfDocument doc,CfGrammar cfg);
		public virtual void AugmentGrammar(object id,CfGrammar cfg) {}
		public void SetPositionInfo(int line,int column,long position)
		{
			Line = line;
			Column = column;
			Position = position;
		}
		public int Line { get; private set; }
		public int Column { get; private set; }
		public long Position { get; private set; }
	}
#if GRIMOIRELIB
	public
#else
	internal
#endif
	abstract class EbnfUnaryExpression : EbnfExpression
	{
		public EbnfExpression Expression { get; set; } = null;
		public override void AugmentGrammar(object id,CfGrammar cfg)
		{
			if (null != Expression)
				Expression.AugmentGrammar(id,cfg);
		}
	}
#if GRIMOIRELIB
	public
#else
	internal
#endif
	abstract class EbnfBinaryExpression : EbnfExpression
	{
		public EbnfExpression Left { get; set; } = null;
		public EbnfExpression Right { get; set; } = null;

		public override void AugmentGrammar(object id,CfGrammar cfg)
		{
			if (null != Left)
				Left.AugmentGrammar(id,cfg);
			if (null != Right)
				Right.AugmentGrammar(id,cfg);
		}
	}
#if GRIMOIRELIB
	public
#else
	internal
#endif
	class EbnfRefExpression : EbnfExpression
	{
		public EbnfRefExpression(object id)
		{
			Id = id;
		}
		public EbnfRefExpression() { }
		public object Id { get; set; } = null;
		public override ICollection<IList<object>> ToNonTerminal(EbnfDocument doc,CfGrammar cfg)
		{
			var l = new List<object>(1);
			l.Add(Id);
			var result = new List<IList<object>>(1);
			result.Add(l);
			return result;
		}
		
		public override bool IsNonTerminal => true;
		public override string ToString()
		{
			return Convert.ToString(Id ?? "");
		}
		public bool Equals(EbnfRefExpression rhs)
		{
			if (ReferenceEquals(rhs, this)) return true;
			if (null == rhs) return false;
			return Equals(Id, rhs.Id);
		}
		public override bool Equals(object obj)
		{
			return Equals(obj as EbnfRefExpression);
		}
		public override int GetHashCode()
		{
			return (null != Id) ? Id.GetHashCode() : 0;
		}
	}

#if GRIMOIRELIB
	public
#else
	internal
#endif
	class EbnfOptionalExpression :EbnfUnaryExpression
	{
		public EbnfOptionalExpression(EbnfExpression expression)
		{
			Expression = expression;
		}
		public override bool IsNonTerminal => true;
		public override ICollection<IList<object>> ToNonTerminal(EbnfDocument doc,CfGrammar cfg)
		{
			var result = new List<IList<object>>();
			if(null!=Expression)
			{
				foreach (var l in Expression.ToNonTerminal(doc,cfg))
					result.Add(new List<object>(l));
				// TODO: this *should* add a null but doing so puts an epsilon in a bad place in the document
				// so we ignore it. This *might* create problems if an optional expression is the only expression 
				// in a production. Figure out what is happening.
				result.Add(new List<object>(new object[] { /*null*/ }));
			}
			return result;
		}
		
		public override string ToString()
		{
			if(null!=Expression)
				return string.Concat("[ ", Expression.ToString(), " ]");
			return "";
		}
		public bool Equals(EbnfOptionalExpression rhs)
		{
			if (ReferenceEquals(rhs, this)) return true;
			if (null == rhs) return false;
			return Equals(Expression, rhs.Expression);
		}
		public override bool Equals(object obj)
		{
			return Equals(obj as EbnfOptionalExpression);
		}
		public override int GetHashCode()
		{
			return (null != Expression) ? Expression.GetHashCode() : 0;
		}
		
	}
#if GRIMOIRELIB
	public
#else
	internal
#endif
	class EbnfRepeatExpression : EbnfUnaryExpression
	{
		public EbnfRepeatExpression(EbnfExpression expression)
		{
			Expression = expression;
		}
		public override bool IsNonTerminal => true;
		
		object _ListId(CfGrammar cfg,object id)
		{
			if (null == id) return null;
			Type t = id.GetType();
			if (typeof(object) == t) return new object();
			if (typeof(int) == t)
			{
				while (true)
				{
					id = ((int)id) + 1;
					if (!cfg.Symbols.Contains(id))
						return id;
				}
			}
			else if (typeof(string) == t)
			{
				var s = id as string + "list";
				var i = 2;
				while (cfg.Symbols.Contains(s))
				{
					s = string.Concat(id, "list", i);
					++i;
				}
				return s;
			}

			throw new NotSupportedException("The type of symbol is not supported for this operation.");
		}
		public override ICollection<IList<object>> ToNonTerminal(EbnfDocument doc,CfGrammar cfg)
		{
			object _listId = _ListId(cfg,"implicit");
			IDictionary<string, object> attrs = new Dictionary<string,object>();
			attrs.Add("collapse", true);
			cfg.Attributes.Add(_listId, attrs);
			var expr = new EbnfOrExpression(new EbnfOrExpression(new EbnfConcatExpression(new EbnfRefExpression(_listId),Expression),Expression), null);
			foreach(var nt in expr.ToNonTerminal(doc,cfg))
			{
				CfgRule r = new CfgRule();
				r.Left = _listId;
				foreach (var s in nt)
				{
					if (1 < r.Right.Count && null == s)
						continue;
					r.Right.Add(s);
				}
				if(!cfg.Rules.Contains(r))
					cfg.Rules.Add(r);
			}
			return new List<IList<object>>(new IList<object>[] { new List<object>(new object[] { _listId })});
		}
		public override string ToString()
		{
			if (null != Expression)
				return string.Concat("{ ", Expression.ToString(), " }");
			return "";
		}
		public bool Equals(EbnfRepeatExpression rhs)
		{
			if (ReferenceEquals(rhs, this)) return true;
			if (null == rhs) return false;
			return Equals(Expression, rhs.Expression);
		}
		public override bool Equals(object obj)
		{
			return Equals(obj as EbnfRepeatExpression);
		}
		public override int GetHashCode()
		{
			return (null != Expression) ? Expression.GetHashCode() : 0;
		}

	}
#if GRIMOIRELIB
	public
#else
	internal
#endif

	class EbnfOrExpression : EbnfBinaryExpression
	{
		public EbnfOrExpression(EbnfExpression left, EbnfExpression right)
		{
			Left = left;
			Right = right;
		}
		public EbnfOrExpression() { }
		public override ICollection<IList<object>> ToNonTerminal(EbnfDocument doc,CfGrammar cfg)
		{
			var result = new List<IList<object>>();
			if (null != Left)
				foreach (var l in Left.ToNonTerminal(doc,cfg))
					result.Add(new List<object>(l.NonNulls()));
			else
				result.Add(new List<object>(new object[] { null }));
			if (null != Right)
				foreach (var l in Right.ToNonTerminal(doc,cfg))
					result.Add(new List<object>(l.NonNulls()));
			else
				result.Add(new List<object>(new object[] { null }));
			return result;
		}
		public override bool IsNonTerminal => true;
		
		public override string ToString()
		{
			return string.Concat(null != Left ? Left.ToString() : "", " | ", null != Right ? Right.ToString() : "");
		}
		public bool Equals(EbnfOrExpression rhs)
		{
			if (ReferenceEquals(rhs, this)) return true;
			if (null == rhs) return false;
			return (Equals(Left, rhs.Left) && Equals(Right, rhs.Right)) ||
				(Equals(Right, rhs.Left) && Equals(Left, rhs.Right));
		}
		public override bool Equals(object obj)
		{
			return Equals(obj as EbnfOrExpression);
		}
		public override int GetHashCode()
		{
			return ((null != Left) ? Left.GetHashCode() : 0) ^ ((null != Right) ? Left.GetHashCode() : 0);
		}
	}
#if GRIMOIRELIB
	public
#else
	internal
#endif
	class EbnfConcatExpression : EbnfBinaryExpression
	{
		public EbnfConcatExpression(EbnfExpression left, EbnfExpression right)
		{
			Left = left;
			Right = right;
		}
		public EbnfConcatExpression() { }
		public override ICollection<IList<object>> ToNonTerminal(EbnfDocument doc,CfGrammar cfg)
		{
			
			var result = new List<IList<object>>();
			if (null == Left && null == Right)
				return result;
			if (null == Left) return Right.ToNonTerminal(doc,cfg);
			else if (null == Right) return Left.ToNonTerminal(doc,cfg);
			foreach (var l in Left.ToNonTerminal(doc,cfg))
			{
				foreach (var r in Right.ToNonTerminal(doc,cfg))
				{
					var n = new List<object>();
					foreach (var s in l)
					{
						if (null != s)
							n.Add(s);
					}
					foreach (var s in r)
					{
						if (null != r)
							n.Add(s);
					}
					result.Add(n);
				}
			}
			return result;
		}
		public override bool IsNonTerminal => true;

		public override string ToString()
		{
			string left = null, right = null;
			var o = Left as EbnfOrExpression;
			if (null != o)
				left = string.Concat("(", Left.ToString(), ")");
			else
				left = (null != Left) ? Left.ToString() : "";
			o = Right as EbnfOrExpression;
			if (null != o)
				right = string.Concat("(", Right.ToString(), ")");
			else
				right = (null != Right) ? Right.ToString() : "";

			return string.Concat(left, " ", right);
		}
		public bool Equals(EbnfConcatExpression rhs)
		{
			if (ReferenceEquals(rhs, this)) return true;
			if (null == rhs) return false;
			return Equals(Left, rhs.Left) && Equals(Right, rhs.Right);
		}
		public override bool Equals(object obj)
		{
			return Equals(obj as EbnfConcatExpression);
		}
		public override int GetHashCode()
		{
			return ((null != Left) ? Left.GetHashCode() : 0) ^ ((null != Right) ? Left.GetHashCode() : 0);
		}
	}

#if GRIMOIRELIB
	public
#else
	internal
#endif
	class EbnfRegexExpression : EbnfExpression
	{
		public EbnfRegexExpression(string regex) { Regex = regex; }
		public EbnfRegexExpression() { }

		public string Regex { get; set; }
		public override ICollection<IList<object>> ToNonTerminal(EbnfDocument doc,CfGrammar cfg)
		{
			var id = doc.GetIdForExpression(this);
			if (null == id)
			{
				id = doc.GetUniqueId();
				doc.Productions.Add(id,new EbnfProduction(this));
			}
			var l = new List<object>(1);
			l.Add(id);
			var result = new List<IList<object>>(1);
			result.Add(l);
			return result;
		}
		public override bool IsNonTerminal => false;
		
		public override string ToString()
		{
			return string.Concat("\'", Regex, "\'");
		}
		public bool Equals(EbnfRegexExpression rhs) 
		{
			if (ReferenceEquals(rhs, this)) return true;
			if (null == rhs) return false;
			return Equals(Regex, rhs.Regex);
		}
		public override bool Equals(object obj)
		{
			return Equals(obj as EbnfRegexExpression);
		}
		public override int GetHashCode()
		{
			return (null!=Regex)?Regex.GetHashCode():0;
		}
	}

#if GRIMOIRELIB
	public
#else
	internal
#endif
	class EbnfLiteralExpression : EbnfExpression
	{
		public EbnfLiteralExpression(string literal) { Literal = literal; }
		public EbnfLiteralExpression() { }
		public string Literal { get; set; }
		public override ICollection<IList<object>> ToNonTerminal(EbnfDocument doc,CfGrammar cfg)
		{
			var id = doc.GetIdForExpression(this);
			if (null == id)
			{
				id = doc.GetUniqueId();
				doc.Productions.Add(id, new EbnfProduction(this));
			}
			var l = new List<object>(1);
			l.Add(id);
			var result = new List<IList<object>>(1);
			result.Add(l);
			return result;
		}
		
		public override bool IsNonTerminal => false;
		public override string ToString()
		{
			return string.Concat("\"", Literal.Replace("\"", "\\\""), "\"");
		}
		public bool Equals(EbnfLiteralExpression rhs)
		{
			if (ReferenceEquals(rhs, this)) return true;
			if (null == rhs) return false;
			return Equals(Literal, rhs.Literal);
		}
		public override bool Equals(object obj)
		{
			return Equals(obj as EbnfLiteralExpression);
		}
		public override int GetHashCode()
		{
			return (null != Literal) ? Literal.GetHashCode() : 0;
		}
	}

}
