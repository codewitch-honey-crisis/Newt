using System;
using System.Collections.Generic;
using System.Text;

namespace Grimoire
{
	/// <summary>
	/// Represents a Context-Free Grammar, or CFG, which is a series of rules.
	/// </summary>
	/// <remarks>This class implements value semantics</remarks>
#if GRIMOIRELIB || NEWT
	public
#else
	internal
#endif
	partial class Cfg : IEquatable<Cfg>, ICloneable, ISymbolResolver
	{
		/// <summary>
		/// Indicates sets of attributes by non-terminal that may further specify or otherwise modify parsing
		/// </summary>
		public IDictionary<string, IDictionary<string, object>> AttributeSets { get; } = new Dictionary<string, IDictionary<string, object>>();
		/// <summary>
		/// The rules that make up the grammar
		/// </summary>
		public IList<CfgRule> Rules { get; } = new List<CfgRule>();

		/// <summary>
		/// Gets or sets the starting non-terminal of the grammar
		/// </summary>
		/// <remarks>This property employs the "start" grammar attribute.</remarks>
		public string StartSymbol {
			get {
				foreach (var attrs in AttributeSets)
				{
					object b;
					if (attrs.Value.TryGetValue("start", out b) && b is bool && (bool)b)
						return attrs.Key;
				}
				if (0 < Rules.Count)
					return Rules[0].Left;
				return null;
			}
			set {
				if (!IsNonTerminal(value))
					throw new ArgumentException("The value must be a non-terminal and present in the grammar.");

				foreach (var a in AttributeSets)
					a.Value.Remove("start");
				IDictionary<string, object> attrs;
				if (!AttributeSets.TryGetValue(value, out attrs))
				{
					attrs = new Dictionary<string, object>();
					AttributeSets.Add(value, attrs);
				}
				attrs.Add("start", true);
			}
		}
		/// <summary>
		/// Returns a string representation of the grammar.
		/// </summary>
		/// <returns>A string containing a series of rules of the form A -> b C</returns>
		/// <remarks>This string is not suitable for comparison. It does not contain the grammar attributes.</remarks>
		public override string ToString()
		{
			var sb = new StringBuilder();
			var ic = Rules.Count;
			for (var i = 0; i < ic; ++i)
				sb.AppendLine(Rules[i].ToString());
			return sb.ToString();
		}
		// retrieves a unique symbol id that can represent 
		// a transformed symbol. does not alter the grammar
		public string GetTransformId(string symbol)
		{
			var names = new HashSet<string>(_EnumSymbols());
			var s = string.Concat(symbol, "`");
			if (!names.Contains(s))
				return s;
			var i = 2;
			s = string.Concat(symbol, "`", i);
			while (names.Contains(s))
			{
				++i;
				s = string.Concat(symbol, "`", i);
			}
			return s;
		}
		public string GetUniqueId(string id)
		{
			var names = new HashSet<string>(_EnumSymbols());
			var s = id;
			if (!names.Contains(s))
				return s;
			var i = 2;
			s = string.Concat(id, i.ToString());
			while (names.Contains(s))
			{
				++i;
				s = string.Concat(id, i.ToString());
			}
			return s;
		}
		IEnumerable<string> _EnumNonTerminals()
		{
			var visited = new HashSet<string>();
			var ic = Rules.Count;
			for (var i = 0; i < ic; ++i)
			{
				var left = Rules[i].Left;
				if (visited.Add(left))
					yield return left;
			}
			visited.Clear();
		}
		IEnumerable<string> _EnumTerminals()
		{
			var visited = new HashSet<string>();
			var ic = Rules.Count;
			for (var i = 0; i < ic; ++i)
			{
				var left = Rules[i].Left;
				visited.Add(left);
			}
			for (var i = 0; i < ic; ++i)
			{
				var rule = Rules[i];
				var jc = rule.Right.Count;
				for (var j = 0; j < jc; ++j)
				{
					var s = rule.Right[j];
					if (visited.Add(s))
						yield return s;
				}
			}
			// some terminals won't be listed in the rules.
			// hidden terminals aren't listed in the grammar.
			// what we do is go through the attribute sets and
			// yield any that are not in the rules. The rationale
			// is that the non-terminals must have appeared on the left
			// at some point so these attributes must be assigned to 
			// terminals. It's not exactly straightforward, but it works
			foreach (var sattrs in AttributeSets)
			{
				if (visited.Add(sattrs.Key))
					yield return sattrs.Key;
			}
			yield return "#EOS";
			yield return "#ERROR";
			visited.Clear();
		}
		IEnumerable<string> _EnumSymbols()
		{
			// we always return nonterminals first
			return _EnumNonTerminals().Concat(_EnumTerminals());
		}
		/// <summary>
		/// Provides a read-only list of all non-terminals in the grammar.
		/// </summary>
		public IList<string> NonTerminals { get { return _EnumNonTerminals().AsList(); } }

		/// <summary>
		/// Provides a read-only list of all terminals in the grammar.
		/// </summary>
		public IList<string> Terminals { get { return _EnumNonTerminals().AsList(); } }

		/// <summary>
		/// Provides a read-only list of all symbols in the grammar.
		/// </summary>
		public IList<string> Symbols { get { return _EnumSymbols().AsList(); } }

		public bool IsNonTerminal(string symbol)
		{
			if (string.IsNullOrEmpty(symbol))
				return false;
			return _EnumNonTerminals().Contains(symbol);
		}
		public IDictionary<string, ICollection<string>> FillFirsts(IDictionary<string, ICollection<string>> result = null)
		{
			if (null == result)
				result = new Dictionary<string, ICollection<string>>();
			foreach (var t in _EnumTerminals())
				if (!Equals("#ERROR", t))
					result.Add(t, new HashSet<string>(new string[] { t }));
			var ic = Rules.Count;
			ICollection<string> col;
			for (var i = 0; i < ic; ++i)
			{
				var rule = Rules[i];
				if (!result.TryGetValue(rule.Left, out col))
				{
					col = new HashSet<string>();
					result.Add(rule.Left, col);
				}
				if (rule.IsNil)
				{
					if (!col.Contains(null))
						col.Add(null);
				}
				else if (!col.Contains(rule.Right[0]))
					col.Add(rule.Right[0]);
			}
			var done = false;
			while (!done)
			{
				done = true;
				foreach (var first in result)
				{
					foreach (var s in new List<string>(first.Value))
					{
						if (IsNonTerminal(s))
						{
							done = false;
							first.Value.Remove(s);
							foreach (var f in result[s])
								if (!first.Value.Contains(f))
									first.Value.Add(f);
						}
					}
				}
			}
			return result;
		}
		public IDictionary<CfgRule, ICollection<string>> FillPredict(IDictionary<string, ICollection<string>> firsts = null, IDictionary<string, ICollection<string>> follows = null, IDictionary<CfgRule, ICollection<string>> result = null)
		{
			if (null == result)
				result = new Dictionary<CfgRule, ICollection<string>>();
			if (null == firsts)
				firsts = FillFirsts();

			if (null == follows)
				follows = FillFollows();

			var ic = Rules.Count;
			for (var i = 0; i < ic; ++i)
			{
				var rule = Rules[i];
				var col = new HashSet<string>();
				if (!rule.IsNil)
				{
					var s = rule.Right[0];
					foreach (var first in firsts[s])
						col.Add(first);
				} else
				{
					foreach (var follow in follows[rule.Left])
						col.Add(follow);
				}
				if (0 < col.Count)
					result.Add(rule, col);
			}
			return result;
		}
		public IDictionary<CfgRule, ICollection<CfgRule>> FillFirstFirstConflicts(IDictionary<string, ICollection<string>> firsts=null, IDictionary<CfgRule, ICollection<CfgRule>> result = null)
		{
			if (null == result)
				result = new Dictionary<CfgRule, ICollection<CfgRule>>();
			if (null == firsts)
				firsts = FillFirsts();
			foreach (var nt in _EnumNonTerminals())
			{
				var rules = FillNonTerminalRules(nt);
				foreach (var rule in rules)
				{
					if (!rule.IsNil)
					{
						foreach (var rule2 in rules)
						{
							if (!ReferenceEquals(rule,rule2) && !rule2.IsNil)
							{
								foreach(var x in firsts[rule2.Right[0]])
								{
									if (null != x)
									{
										if(firsts[rule.Right[0]].Contains(x))
										{
											ICollection<CfgRule> col;
											if (result.TryGetValue(rule2, out col) && col.Contains(rule))
												continue;
											if (!result.TryGetValue(rule,out col))
											{
												col = new HashSet<CfgRule>();
												result.Add(rule, col);
											}
											col.Add(rule2);
											break;
										}
									}
								}
							}
						}
					}
				}
			}
			return result;
		}
		public IDictionary<string, ICollection<CfgRule>> FillFirstFollowsConflicts(IDictionary<string, ICollection<string>> firsts = null, IDictionary<string, ICollection<string>> follows = null,IDictionary<string, ICollection<CfgRule>> result = null)
		{
			if (null == result)
				result = new Dictionary<string, ICollection<CfgRule>>();
			if (null == firsts)
				firsts = FillFirsts();
			if (null == follows)
				follows = FillFollows(firsts);
			var predict = FillPredict(firsts, follows);
			foreach (var nt in _EnumNonTerminals())
			{
				ICollection<string> col;
				if(follows.TryGetValue(nt,out col))
				{
					if(IsNillable(nt))
					{
						foreach(var p in predict)
						{
							if (!p.Key.IsNil)
							{
								if (Equals(p.Key.Left, nt))
								{
									foreach (var ff in p.Value)
									{
										if(col.Contains(ff))
										{
											ICollection<CfgRule> ccol;
											if(!result.TryGetValue(nt,out ccol))
											{
												ccol = new HashSet<CfgRule>();
												result.Add(nt, ccol);
											}
											ccol.Add(p.Key);
											break;
										}
									}
								}
							}
						}
					}
				}
			}
			return result;
		}

		public IList<CfgMessage> EliminateFirstFirstConflicts()
		{
			var result = new List<CfgMessage>();
			foreach (var nt in new List<string>(_EnumNonTerminals()))
			{
				var rules = FillNonTerminalRules(nt);
				var rights = new List<IList<string>>();
				foreach (var rule in rules)
					rights.Add(rule.Right);
				while (true)
				{
					var pfx = rights.GetLongestCommonPrefix();
					if (pfx.IsNullOrEmpty())
						break;
					// obv first first conflict
					var nnt = GetTransformId(nt);

					var suffixes = new List<IList<string>>();
					foreach (var rule in rules)
					{
						if (rule.Right.StartsWith(pfx))
						{
							rights.Remove(rule.Right);
							suffixes.Add(new List<string>(rule.Right.SubRange(pfx.Count)));
							result.Add(new CfgMessage(CfgErrorLevel.Message, -1, string.Format("Removing rule {0} because it is part of a first-first conflict.", rule)));
							Rules.Remove(rule);
						}
					}

					var newRule = new CfgRule(nt);
					newRule.Right.AddRange(pfx);
					newRule.Right.Add(nnt);
					result.Add(new CfgMessage(CfgErrorLevel.Message, -1, string.Format("Adding rule {0} to resolve first-first conflict.", newRule)));
					if (!Rules.Contains(newRule))
						Rules.Add(newRule);
					foreach (var suffix in suffixes)
					{
						newRule = new CfgRule(nnt);
						newRule.Right.AddRange(suffix);
						result.Add(new CfgMessage(CfgErrorLevel.Message, -1, string.Format("Adding rule {0} to resolve first-first conflict.", newRule)));
						if (!Rules.Contains(newRule))
							Rules.Add(newRule);
					}
					var attrs = new Dictionary<string, object>();
					attrs.Add("collapse", true);
					AttributeSets.Add(nnt, attrs);

				}
			}
			return result;
		}
		public IList<CfgMessage> EliminateFirstFollowsConflicts()
		{
			var result = new List<CfgMessage>();
			var firsts = FillFirsts();
			var follows = FillFollows();
			foreach(var nt in _EnumNonTerminals())
			{
				var x = firsts[nt];
				ICollection<string> y;
				if(follows.TryGetValue(nt, out y))
				{
					if (IsNillable(nt))
					{
						foreach (var yy in y)
						{
							if (x.Contains(yy))
							{
								break;
							}
						}
					}
				}
			}
			return result;
		}
		public IList<CfgMessage> EliminateLeftRecursion()
		{
			var result = new List<CfgMessage>();
			var ic = Rules.Count;
			for (var i = 0; i < ic; ++i)
			{
				var rule = Rules[i];
				if (rule.IsDirectlyLeftRecursive)
				{
					result.Add(new CfgMessage(CfgErrorLevel.Message, -1, string.Format("Removing rule {0} because it is directly left recursive.", rule)));
					Rules.Remove(rule);
					var newId = GetTransformId(rule.Left);

					var col = new List<string>();
					var c = rule.Right.Count;
					for (var j = 1; j < c; ++j)
						col.Add(rule.Right[j]);
					col.Add(newId);
					var d = new Dictionary<string, object>();
					AttributeSets.Add(newId, d);
					d.Add("collapse", true);
					var newRule = new CfgRule(newId);
					newRule.Right.AddRange(col);
					result.Add(new CfgMessage(CfgErrorLevel.Message, -1, string.Format("Adding rule {1} to replace rule {0}", rule,newRule)));
					if (!Rules.Contains(newRule))
						Rules.Add(newRule);
					var rr = new CfgRule(newId);
					result.Add(new CfgMessage(CfgErrorLevel.Message, -1, string.Format("Adding rule {1} to replace rule {0}", rule, rr)));
					if (!Rules.Contains(rr))
						Rules.Add(rr);
					foreach (var r in Rules)
					{
						if (Equals(r.Left, rule.Left))
						{
							if (!r.IsDirectlyLeftRecursive)
							{
								r.Right.Add(newId);
							}
						}
					}
				}
			}
			return result;
		}
		public IList<CfgMessage> PrepareLL1(bool throwIfErrors=true)
		{
			var result = new List<CfgMessage>();
			Cfg old = this;
			// if 10 times doesn't sort out this grammar it's not LL(1)
			// the math is such that we don't know unless we try
			// and the tries can go on forever.
			for (int i = 0; i < 10; ++i)
			{
				var fcc = FillFirstFollowsConflicts();
				if (0 < fcc.Count)
					result.AddRange(EliminateFirstFollowsConflicts());
				if (IsDirectlyLeftRecursive)
					result.AddRange(EliminateLeftRecursion());
				var cc = FillFirstFirstConflicts();
				if (0 < cc.Count)
					result.AddRange(EliminateFirstFirstConflicts());
				//result.AddRange(EliminateUnderivableRules());
				fcc = FillFirstFollowsConflicts();
				cc = FillFirstFirstConflicts();
				if (0 == cc.Count && 0 == fcc.Count && !IsDirectlyLeftRecursive)
					break;
				if (old.Equals(this))
					break;
				old = Clone();
			}
			if (IsDirectlyLeftRecursive)
				result.Add(new CfgMessage(CfgErrorLevel.Error, 10, "Grammar is unresolvably directly left recursive and cannot be parsed with an LL parser."));
			var fc= FillFirstFollowsConflicts();
			foreach (var f in fc)
				result.Add(new CfgMessage(CfgErrorLevel.Error, 11, string.Concat("Grammar has an unresolvable first-follows conflict on ", f.Key)));
			var c = FillFirstFirstConflicts();
			foreach (var f in c)
				foreach (var ff in f.Value)
					result.Add(new CfgMessage(CfgErrorLevel.Error, 12, string.Concat("Grammar has an unresolvable first-first conflict between ", f.Key, " and ", ff)));
			if (throwIfErrors)
			{
				foreach (var m in result)
					if (CfgErrorLevel.Error == m.ErrorLevel)
						throw new CfgException(result);
			}
			return result;
		}
		public bool IsNillable(string nonTerminal)
		{
			foreach (var rule in FillNonTerminalRules(nonTerminal))
				if (rule.IsNil)
					return true;
			return false;
		}
		public IList<CfgRule> FillNonTerminalRules(string nonTerminal, IList<CfgRule> result = null)
		{
			if (null == result) result = new List<CfgRule>();
			var ic = Rules.Count;
			for (var i = 0; i < ic; ++i)
			{
				var rule = Rules[i];
				if (Equals(rule.Left, nonTerminal))
					result.Add(rule);
			}
			return result;
		}
		/// <summary>
		/// Retrieves an integer identifier for a symbol suitable for use by parse tables and parsers.
		/// </summary>
		/// <param name="symbol">The symbol to retrieve the identifier for.</param>
		/// <returns>An integer that can be used to refer to the symbol, or less than zero to indicate the symbol was not present in the CFG.</returns>
		public int GetSymbolId(string symbol)
		{
			return _EnumSymbols().IndexOf(symbol);
		}
		/// <summary>
		/// Computes the FOLLOWS sets for the grammar
		/// </summary>
		/// <param name="firsts">The precomputed firsts sets. These will be computed if this is null.</param>
		/// <param name="result">The result to fill. If null, a new dictionary will be created.</param>
		/// <returns>The result, filled with FOLLOWS sets grouped by non-terminal</returns>
		public IDictionary<string,ICollection<string>> FillFollows(IDictionary<string,ICollection<string>> firsts=null,IDictionary<string,ICollection<string>> result = null)
		{
			if (null == result)
				result = new Dictionary<string, ICollection<string>>();
			var done = false;
			if(null==firsts)
				firsts = FillFirsts();
			var ss = StartSymbol;
			// augment the grammar S' -> S #EOS
			CfgRule start = new CfgRule(GetTransformId(ss),ss, "#EOS");
			foreach(var rule in new CfgRule[] {start}.Concat(Rules))
			{
				if (rule.IsNil)
				{
					ICollection<string> col;
					if (!result.TryGetValue(rule.Left, out col))
					{
						col = new HashSet<string>();
						result.Add(rule.Left, col);
					}
					if (!col.Contains(rule.Left))
						col.Add(rule.Left);
				}
				else
				{
					var ic = rule.Right.Count;
					for (var i = 1; i < ic; ++i)
					{
						var prev = rule.Right[i - 1];
						var sym = rule.Right[i];
						if (IsNonTerminal(prev))
						{
							ICollection<string> col;
							if (!result.TryGetValue(prev, out col))
							{
								col = new HashSet<string>();
								result.Add(prev, col);
							}
							foreach (var s in firsts[sym])
							{
								// we'll need the following symbol's follows
								if (null == s)
								{
									if (!col.Contains(sym))
										col.Add(sym);
								}
								else if (!col.Contains(s))
									col.Add(s);
							}
						}
					}
					var last = rule.Right[ic - 1];
					if (IsNonTerminal(last))
					{
						ICollection<string> col;
						if (!result.TryGetValue(last, out col))
						{
							col = new HashSet<string>();
							result.Add(last, col);
						}
						if (!col.Contains(rule.Left))
							col.Add(rule.Left);
					}
				}
			}
			done = false;
			const int LIMIT = 10000;
			var il = 0;
			while (!done)
			{
				done = true;
				foreach (var kvp in result)
				{
					foreach (var s in new List<string>(kvp.Value))
					{
						if (IsNonTerminal(s))
						{
							kvp.Value.Remove(s);
							ICollection<string> col;
							if (result.TryGetValue(s, out col))
							{
								foreach (var sss in col)
									if (!kvp.Value.Contains(sss))
									{
										kvp.Value.Add(sss);
										done = false;
									}
							}
							
						}
					}
				}
				++il;
				if (LIMIT<il)
					throw new CfgException("Left recursion detected in grammar - follows limit exceeded.",16);
			}
			return result;
		}
		public bool IsDirectlyLeftRecursive {
			get {
				var ic = Rules.Count;
				for(var i = 0;i<ic;++i)
					if (Rules[i].IsDirectlyLeftRecursive)
						return true;
				return false;
			}
		}
		/// <summary>
		/// Reports whether the specified non-terminal symbol is nillable
		/// </summary>
		/// <param name="symbol">The non-terminal to check</param>
		/// <returns>True if the symbol is a non-terminal and if it has a rule that takes the form of A -> ε, otherwise false.</returns>
		public bool IsNillable(object symbol)
		{
			var ic = Rules.Count;
			for(var i = 0;i<ic;++i)
			{
				var rule = Rules[i];
				if (Equals(symbol, rule.Left) && rule.IsNil)
					return true;
			}
			return false;
		}
		
		/// <summary>
		/// Builds a table an LL(1) parser can use to parse input.
		/// </summary>
		/// <param name="predict">The computed prediction table, or null to compute it.</param>
		/// <returns>A dictionary based parse table suitable for use by an LL(1) parser.</returns>
		public IDictionary<int, IDictionary<int, (int Left, int[] Right)>> ToLL1ParseTable(IDictionary<CfgRule, ICollection<string>> predict = null)
		{
			var result = new Dictionary<int, IDictionary<int, (int Left, int[] Right)>>();
			if (null == predict) predict = FillPredict();
			foreach (var nt in _EnumNonTerminals())
			{
				var d = new Dictionary<int, (int Left, int[] Right)>();
				foreach (var pre in predict)
				{
					if (Equals(nt, pre.Key.Left))
					{
						foreach (var s in pre.Value)
						{
							var right = new int[pre.Key.Right.Count];
							for (var i = 0; i < right.Length; i++)
								right[i] = GetSymbolId(pre.Key.Right[i]);
							(int Left, int[] Right) ir = (GetSymbolId(pre.Key.Left), right);
							var sid = GetSymbolId(s);
							(int Left, int[] Right) ir2;
							if (d.TryGetValue(sid,out ir2))
							{
								throw new Exception(string.Format("Conflict between {0} and {1}", _ToStringIntRule(ir2), _ToStringIntRule(ir)));
							} else
								d.Add(GetSymbolId(s), ir);
						}
					}
				}
				result.Add(GetSymbolId(nt), d);
			}
			return result;
		}
		string _ToStringIntRule((int Left, int[] Right) rule)
		{
			var sb = new StringBuilder();
			sb.Append(GetSymbolById(rule.Left));
			sb.Append(" ->");
			for(var i = 0;i<rule.Right.Length;i++)
			{
				sb.Append(" ");
				sb.Append(GetSymbolById(rule.Right[i]));
			}
			return sb.ToString();
		}
		public string GetSymbolById(int symbolId)
		{
			if(-1<symbolId && symbolId<_EnumSymbols().Count())
				return _EnumSymbols().GetAt(symbolId);
			return null;
		}
		static string _MakeSafeCsv(string field)
		{
			if(-1<field.IndexOfAny(new char[] { ',', '\"', '\n', '\r' }))
				return string.Concat("\"", field.Replace("\"", "\"\""), "\"");
			return field;
		}
		public string ToLL1Csv(IDictionary<int, IDictionary<int, (int Left, int[] Right)>> parseTable=null)
		{
			if (null == parseTable)
				parseTable = ToLL1ParseTable();
			var sb = new StringBuilder();
			sb.Append("LL(1) Parse Table");
			foreach(var t in _EnumTerminals())
			{
				if(!Equals("#ERROR",t))
				{
					sb.Append(",");
					sb.Append(_MakeSafeCsv(t));
				}
			}
			sb.AppendLine();
			foreach(var nt in _EnumNonTerminals())
			{
				sb.Append(_MakeSafeCsv(nt));
				foreach(var t in _EnumTerminals())
				{
					if (!Equals("#ERROR", t))
					{
						sb.Append(",");
						IDictionary<int, (int Left, int[] Right)> d;
						(int Left, int[] Right) ir;
						if (parseTable.TryGetValue(GetSymbolId(nt), out d) && d.TryGetValue(GetSymbolId(t), out ir))
						{
							sb.Append(_MakeSafeCsv(GetSymbolById(ir.Left)));
							sb.Append(" ->");
							for (var i = 0; i < ir.Right.Length; i++)
							{
								sb.Append(" ");
								sb.Append(_MakeSafeCsv(GetSymbolById(ir.Right[i])));
							}
						}
					}
				}
				sb.AppendLine();
			}
			return sb.ToString();
		}
		#region Value Semantics
		/// <summary>
		/// Indicates whether the CFG is exactly equivelant to the specified CFG
		/// </summary>
		/// <param name="rhs">The CFG to compare</param>
		/// <returns>True if the CFGs are equal, otherwise false.</returns>
		public bool Equals(Cfg rhs)
		{
			if (!CollectionUtility.Equals(this.Rules, rhs.Rules))
				return false;
			if (AttributeSets.Count != rhs.AttributeSets.Count)
				return false;
			foreach(var attrs in AttributeSets)
			{
				IDictionary<string, object> d;
				if(!rhs.AttributeSets.TryGetValue(attrs.Key,out d))
				{
					if (d.Count != attrs.Value.Count)
						return false;
					foreach(var attr in attrs.Value)
					{
						object o;
						if(!d.TryGetValue(attr.Key, out o) || !Equals(o,attr.Value))
							return false;
					}
				}
			}
			return true;
		}
		/// <summary>
		/// Indicates whether the CFG is exactly equivelant to the specified CFG
		/// </summary>
		/// <param name="obj">The CFG to compare</param>
		/// <returns>True if the CFGs are equal, otherwise false.</returns>
		public override bool Equals(object obj)
		{
			return Equals(obj as Cfg);
		}
		/// <summary>
		/// Gets a hashcode that represents this CFG
		/// </summary>
		/// <returns>A hashcode that represents this CFG</returns>
		public override int GetHashCode()
		{
			var result = CollectionUtility.GetHashCode(Rules);
			foreach (var attrs in AttributeSets)
			{
				result ^= attrs.Key.GetHashCode();
				foreach(var attr in attrs.Value)
				{
					result ^= attr.Key.GetHashCode();
					if (null != attr.Value)
						result ^= attr.Value.GetHashCode();
				}
			}
			return result;
		}
		/// <summary>
		/// Indicates whether the two CFGs are exactly equivelent
		/// </summary>
		/// <param name="lhs">The first CFG to compare</param>
		/// <param name="rhs">The second CFG to compare</param>
		/// <returns>True if the CFGs are equal, otherwise false</returns>
		public static bool operator==(Cfg lhs,Cfg rhs)
		{
			if (ReferenceEquals(lhs, rhs)) return true;
			if (ReferenceEquals(lhs, null) || ReferenceEquals(rhs,null)) return false;
			return lhs.Equals(rhs);
		}
		/// <summary>
		/// Indicates whether the two CFGs are not equal
		/// </summary>
		/// <param name="lhs">The first CFG to compare</param>
		/// <param name="rhs">The second CFG to compare</param>
		/// <returns>True if the CFGs are not equal, or false if they are equal</returns>
		public static bool operator !=(Cfg lhs, Cfg rhs)
		{
			if (ReferenceEquals(lhs, rhs)) return false;
			if (ReferenceEquals(lhs, null) || ReferenceEquals(rhs, null)) return true;
			return !lhs.Equals(rhs);
		}

		/// <summary>
		/// Performs a deep clone of the CFG
		/// </summary>
		/// <returns>A new CFG equal to this CFG</returns>
		public Cfg Clone()
		{
			var result = new Cfg();
			var ic = Rules.Count;
			for(var i = 0;i<ic;++i)
				result.Rules.Add(Rules[i].Clone());
			foreach(var attrs in AttributeSets)
			{
				var d = new Dictionary<string, object>();
				result.AttributeSets.Add(attrs.Key, d);
				foreach(var attr in attrs.Value)
					d.Add(attr.Key, attr.Value);
			}
			return result;
		}
		object ICloneable.Clone() { return Clone();  }
		#endregion
	}
}
