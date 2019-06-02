using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
// Classes for representing a Context Free Grammar (CFG)
namespace Grimoire
{
	using CS = CSharpUtility;
	using LL1ParseTable = IDictionary<int, IDictionary<int, (int RuleId, int Left, int[] Right)>>;
	
	#region Error/Warning Support
#if GRIMOIRELIB
	public
#else
	internal
#endif
	enum CfgErrorLevel
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
	sealed class CfgMessage
	{
		public CfgMessage(CfgErrorLevel errorLevel, int errorCode, string message)
		{
			ErrorLevel = errorLevel;
			ErrorCode = errorCode;
			Message = message;
		}
		public CfgErrorLevel ErrorLevel { get; private set; }
		public int ErrorCode { get; private set; }
		public string Message { get; private set; }
		public override string ToString()
		{
			return string.Format("{0}: {1} code {2}",
				ErrorLevel, Message, ErrorCode
				);
		}
	}
#if GRIMOIRELIB
	public
#else
	internal
#endif
	sealed class CfgException : Exception
	{
		public IList<CfgMessage> Messages { get; }
		public CfgException(string message, int errorCode = -1) :
			this(new CfgMessage[] { new CfgMessage(CfgErrorLevel.Error, errorCode, message) })
		{ }
		static string _FindMessage(IEnumerable<CfgMessage> messages)
		{
			var l = new List<CfgMessage>(messages);
			if (null == messages) return "";
			int c = 0;
			foreach (var m in l)
			{
				if (CfgErrorLevel.Error == m.ErrorLevel)
				{
					if (1 == l.Count)
						return m.ToString();
					return string.Concat(m, " (multiple messages)");
				}
				++c;
			}
			foreach (var m in messages)
				return m.ToString();
			return "";
		}
		public CfgException(IEnumerable<CfgMessage> messages) : base(_FindMessage(messages))
		{
			Messages = new List<CfgMessage>(messages);
		}
		public static void ThrowIfErrors(IEnumerable<CfgMessage> messages)
		{
			if (null == messages) return;
			foreach (var m in messages)
				if (CfgErrorLevel.Error == m.ErrorLevel)
					throw new CfgException(messages);
		}
	}
	#endregion

	#region CfgRule
#if GRIMOIRELIB
	public
#else
	internal
#endif
	/// <summary>
	/// A rule takes the form E → A B C 
	/// Multiple rules with the same lhs (E above) make up a non-terminal
	/// </summary>
	/// <remarks>Epsilon is represented by null.</remarks>
	class CfgRule : IEquatable<CfgRule>, ICloneable
	{
		public CfgRule() { }
		public CfgRule(object left, object right1, params object[] rightN) 
		{
			Left = left;
			Right.Add(right1);
			if (null != rightN)
				foreach (var sym in rightN)
					Right.Add(sym);
		}
		public bool IsSingleEpsilon {
			get { return null == Right || 0 == Right.Count || (1 == Right.Count && null == Right[0]); }
		}
		
		public object Left { get; set; } = null;
		public IList<object> Right { get; } = new List<object>();

		public bool Equals(CfgRule rhs)
		{
			if (ReferenceEquals(rhs, this))
				return true;
			if (ReferenceEquals(rhs, null))
				return false;
			if (!Equals(Left, rhs.Left))
				return false;
			var c = Right.Count;
			if (rhs.Right.Count != c)
				return false;
			for (var i = 0; i < c; ++i)
				if (!Equals(Right[i], rhs.Right[i]))
					return false;
			return true;
		}
		public override bool Equals(object obj)
		{
			if (ReferenceEquals(obj, this))
				return true;
			return Equals(obj as CfgRule);
		}
		public override int GetHashCode()
		{
			var result = Left.GetHashCode();
			var c = Right.Count;
			for (var i = 0; i < c; ++i)
			{
				var sym = Right[i];
				if (null != sym)
					result ^= sym.GetHashCode();
			}
			return result;
		}
		public override string ToString()
		{
			var sb = new StringBuilder();
			sb.Append(Left);
			sb.Append(" ->");
			foreach (var sym in Right)
			{
				sb.Append(' ');
				if (null == sym)
					sb.Append("<epsilon>");
				else
					sb.Append(sym);
			}
			return sb.ToString();
		}
		public CfgRule Clone()
		{
			return new CfgRule(Left, Right);
		}
		object ICloneable.Clone() => Clone();
		public static bool operator ==(CfgRule lhs, CfgRule rhs)
		{
			if (ReferenceEquals(lhs, rhs)) return true;
			if (ReferenceEquals(null, lhs)) return false;
			return lhs.Equals(rhs);
		}
		public static bool operator !=(CfgRule lhs, CfgRule rhs)
		{
			if (ReferenceEquals(lhs, rhs)) return false;
			if (ReferenceEquals(null, lhs)) return true;
			return !lhs.Equals(rhs);
		}

		static IEnumerable<object> _EnumLefts(IEnumerable<CfgRule> rules)
		{
			foreach (var rule in rules)
				yield return rule.Left;
		}
		public static ICollection<object> GetLefts(IEnumerable<CfgRule> rules)
		=> _EnumLefts(rules).AsCollection();
	}
	#endregion

	/// <summary>
	/// A grammar takes the form of 
	/// S → N
	/// N → V = E
	/// N → E
	/// E → V
	/// V → x
	/// V → *
	/// 
	/// Multiple rules make up a grammar
	/// </summary>
#if GRIMOIRELIB
	public
#else
	internal
#endif
	class CfGrammar : IEquatable<CfGrammar>, ICloneable ,ISymbolResolver{

		public IDictionary<object, IDictionary<string, object>> Attributes { get; } = new Dictionary<object, IDictionary<string, object>>();

		public IList<CfgRule> Rules { get; } = new List<CfgRule>();

		public CfGrammar Clone()
		{
			var result = new CfGrammar();
			foreach(var sattrs in Attributes)
			{
				var d = new Dictionary<string, object>();
				result.Attributes.Add(sattrs.Key, d);
				foreach(var attr in sattrs.Value)
					d.Add(attr.Key, attr.Value);
			}
			foreach(var rule in Rules)
				result.Rules.Add(rule.Clone());
			return result;
		}
		object ICloneable.Clone() => Clone();
		public IDictionary<IList<object>,IList<CfgRule>> FillFirstFirstConflictsGroupByPrefix(IDictionary<IList<object>, IList<CfgRule>> result=null)
		{
			if (null == result)
				result = new Dictionary<IList<object>, IList<CfgRule>>(OrderedCollectionEqualityComparer<object>.Default);

			var flatRules = new Dictionary<IList<object>, CfgRule>();
			foreach (var rule in Rules)
			{
				var flatRule = new List<object>();
				flatRule.Add(rule.Left);
				flatRule.AddRange(rule.Right);
				flatRules.Add(flatRule, rule);
			}
			var groupsStart = new Dictionary<IList<object>, IList<IList<object>>>(OrderedCollectionEqualityComparer<object>.Default);
			foreach (var flatRule in flatRules)
			{
				foreach (var flatRuleCmp in flatRules)
				{
					var common = new IList<object>[] { flatRule.Key, flatRuleCmp.Key }.GetCommonPrefix();
					if (0 == common.Count)
						continue;
					IList<IList<object>> list;
					if (!groupsStart.TryGetValue(common, out list))
					{
						list = new List<IList<object>>();
						groupsStart.Add(common, list);
					}
					if(!list.Contains(flatRuleCmp.Key,OrderedCollectionEqualityComparer<object>.Default))
						list.Add(flatRuleCmp.Key);

				}
			}
			foreach (var group in groupsStart)
			{
				if (1 < group.Value.Count)
				{
					var ruleList = new List<CfgRule>();
					foreach (var list in group.Value)
						ruleList.Add(flatRules[list]);

					result.Add(group.Key, ruleList);
				}
			}
			return result;
		}
		static IList<IList<object>> _FillConflictGroupFollows(IList<object> groupKey, IEnumerable<CfgRule> rules, IList<IList<object>> result = null)
		{
			if (null == result)
				result = new List<IList<object>>();
			foreach (var rule in rules)
			{
				var l = new List<object>(rule.Right.SubRange(groupKey.Count - 1));
				result.Add(l);
			}
			return result;
		}
		public void LeftFactor()
		{
			var conflictGroups = FillFirstFirstConflictsGroupByPrefix();
			foreach(var conflictGroup in conflictGroups)
			{
				var newId = _LeftFactorId(conflictGroup.Key[0]); // "rule" becomes "rulepart"
				var follows = _FillConflictGroupFollows(conflictGroup.Key, conflictGroup.Value);
				// create our follows rules
				foreach (var follow in follows) {
					var l = follow;
					if (0 == l.Count)
						l = new List<object>(new object[] { null });
					var newRule = new CfgRule(newId,null);
					newRule.Right.Clear();
					newRule.Right.AddRange(l);
					if(!Rules.Contains(newRule))
						Rules.Add(newRule);
				}
				CfgRule first = null;
				foreach(var rule in conflictGroup.Value)
				{
					if (null == first)
						first = rule;
					else
						Rules.Remove(rule);
				}
				int i = Rules.IndexOf(first);
				var rr = new CfgRule(first.Left, null);
				Rules[i] = rr;
				rr.Right.Clear();
				rr.Right.AddRange(conflictGroup.Key.SubRange(1).Concat(new object[] { newId }));
				var attrs = new Dictionary<string, object>();
				attrs.Add("collapse",true);
				Attributes.Add(newId,attrs);
			}
		}
		public IList<CfgMessage> PrepareLL1(bool throwIfErrors = true)
		{
			var result = new List<CfgMessage>();
			CfGrammar old = this;
			// if 10 times doesn't sort out this grammar it's not LL(1)
			// the math is such that we don't know unless we try
			// and the tries can go on forever.
			for (int i = 0; i < 10; ++i)
			{
				var c = AnalyzeLL1Conflicts();
				//if (0 < c.FirstFollows.Count)
				//	result.AddRange(EliminateFirstFollowsConflicts());
				if (IsIndirectlyLeftRecursive || IsDirectlyLeftRecursive)
					result.AddRange(EliminateLeftRecursion());
				c = AnalyzeLL1Conflicts();
				if (0 < c.FirstFirsts.Count)
					result.AddRange(EliminateFirstFirstConflicts());
				result.AddRange(EliminateUnderivableRules());
				c = AnalyzeLL1Conflicts();
				if (0 == c.FirstFirsts.Count && 0 == c.FirstFollows.Count && !IsDirectlyLeftRecursive && !IsIndirectlyLeftRecursive)
					break;
				if (old.Equals(this))
					break;
				old = this.Clone();
			}
			if(IsDirectlyLeftRecursive)
				result.Add(new CfgMessage(CfgErrorLevel.Error, 10, "Grammar is unresolvably directly left recursive and cannot be parsed with an LL parser."));
			if(IsIndirectlyLeftRecursive)
				result.Add(new CfgMessage(CfgErrorLevel.Warning, 10, "Grammar appears unresolvalbly and indirectly left recursive and possibly cannot be parsed with an LL parser."));
			var conflicts = AnalyzeLL1Conflicts();
			foreach (var f in conflicts.FirstFollows)
				result.Add(new CfgMessage(CfgErrorLevel.Error, 11, string.Concat("Grammar has an unresolvable first-follows conflict on ", f.Key)));
			foreach (var f in conflicts.FirstFirsts)
				result.Add(new CfgMessage(CfgErrorLevel.Error, 12, string.Concat("Grammar has an unresolvable first-first conflict between ", f.Key, " and ",f.Value)));
			if(throwIfErrors)
			{
				foreach(var m in result)
					if (CfgErrorLevel.Error == m.ErrorLevel)
						throw new CfgException(result);
			}
			return result;
		}
		
		IEnumerable<object> _EnumSymbols()
		{
			HashSet<object> seen = new HashSet<object>();
			var ic = Rules.Count;
			for(var i = 0;i<ic;++i)
			{
				var rule = Rules[i];
				if (seen.Add(rule.Left))
					yield return rule.Left;
				var jc = rule.Right.Count;
				for(var j=0;j<jc;++j)
				{
					var sym = rule.Right[j];
					if (null != sym)
					{
						if (seen.Add(sym))
							yield return sym;
					}
				}
			}
			foreach (var sym in Attributes.Keys)
				if (seen.Add(sym))
					yield return sym;

		}
		public IList<object> FillSymbols(IList<object> result=null)
		{
			if(null==result)
				result = new List<object>();
			var ic = Rules.Count;
			for (var i = 0; i < ic; ++i)
			{
				var rule = Rules[i];
				if (!result.Contains(rule.Left))
					result.Add(rule.Left);
				var jc = rule.Right.Count;
				for (var j = 0; j < jc; ++j)
				{
					var sym = rule.Right[j];
					if (null != sym)
					{
						if (!result.Contains(sym))
							result.Add(sym);
					}
				}
			}
			foreach (var sym in Attributes.Keys)
				if (!result.Contains(sym))
					result.Add(sym);
			return result;
		}
		public IList<object> Symbols { get =>_EnumSymbols().AsList(); }

		IEnumerable<object> _EnumNonTerminals()
		{
			HashSet<object> seen = new HashSet<object>();
			var ic = Rules.Count;
			for (var i = 0; i < ic; ++i)
			{
				var rule = Rules[i];
				if (seen.Add(rule.Left))
					yield return rule.Left;
			}
			
		}
		public IList<object> FillNonTerminals(IList<object> result = null)
		{
			if (null == result)
				result = new List<object>();
			var ic = Rules.Count;
			for (var i = 0; i < ic; ++i)
			{
				var rule = Rules[i];
				if (!result.Contains(rule.Left))
					result.Add(rule.Left);
			}
			return result;
		}
		public bool IsNonTerminal(object sym)
		{
			var ic = Rules.Count;
			for (var i = 0; i < ic; ++i)
			{
				var rule = Rules[i];
				if (Equals(sym, rule.Left))
					return true;
			}
			return false;
		}
		public IList<object> NonTerminals { get => _EnumNonTerminals().AsList(); }

		IEnumerable<object> _EnumTerminals()
		{
			foreach (var sym in _EnumSymbols())
				if (!IsNonTerminal(sym))
					yield return sym;
		}
		public IList<object> FillTerminals(IList<object> result=null)
		{
			if (null == result)
			{
				result = new List<object>();
				foreach (var sym in _EnumSymbols())
					if (!IsNonTerminal(sym))
						result.Add(sym);
				return result;
			}
			foreach (var sym in _EnumSymbols())
				if (!IsNonTerminal(sym))
					if (!result.Contains(sym))
						result.Add(sym);
			return result;
		}
		public IList<object> Terminals { get => _EnumTerminals().AsList(); }
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
		public int GetSymbolId(object symbol)
		{
			if (null == symbol) return -3;
			var i = 0;
			foreach (var sym in _EnumSymbols())
			{
				if (Equals(symbol, sym))
					return i;
				++i;
			}
			return -1;
		}
		public object GetSymbolById(int id)
		{
			if (-2 == id)
				return ErrorSymbol;
			if (-1 == id)
				return EosSymbol;
			return _EnumSymbols().GetAt(id);
		}
		public CfgRule GetRuleById(int id)
		{
			var rule = Rules.GetAt(id);
			return rule;
		}
		public int GetRuleId(CfgRule rule)
		{
			if (null != rule)
				return Rules.IndexOf(rule);
			
			return -1;
		}
		object _eosSymbol;

		public object EosSymbol {
			get {
				if (null == _eosSymbol)
				{
					if (0 < Rules.Count)
					{
						
						Type t = CfgRule.GetLefts(Rules).InferElementType();
						if (typeof(object) == t)
						{
							_eosSymbol = new object();
							return _eosSymbol;
						}
						else if (typeof(int) == t)
							return -1;
						else if (typeof(string) == t)
							return "$";
						
					}
				}
				return _eosSymbol;
			}
			set {
				_eosSymbol = value;
			}

		}
		object _errorSymbol;
		public object ErrorSymbol {
			get {
				if (null == _errorSymbol)
				{
					if (0 < Rules.Count)
					{

						Type t = CfgRule.GetLefts(Rules).InferElementType();
						if (typeof(object) == t)
						{
							_errorSymbol = new object();
							return _errorSymbol;
						}
						else if (typeof(int) == t)
							return -2;
						else if (typeof(string) == t)
							return "#ERROR";

					}
				}
				return _errorSymbol;
			}
			set {
				_errorSymbol = value;
			}

		}
		public IDictionary<CfgRule, ICollection<object>> FillFirsts(IDictionary<CfgRule, ICollection<object>> result = null)
		{
			if (null == result)
				result = new Dictionary<CfgRule, ICollection<object>>();
			foreach (var rule in Rules)
			{
				if (0 < rule.Right.Count) // sanity check
				{
					ICollection<object> col;
					if (!result.TryGetValue(rule, out col))
					{
						col = new List<object>();
						result.Add(rule, col);
					}
					col.Add(rule.Right[0]);
				}
			}
			var done = false;
			while (!done)
			{
				done = true;
				foreach (var res in result)
				{
					foreach (var sym in new List<object>(res.Value))
					{
						// found a non-terminal, more work to do
						if (_EnumNonTerminals().Contains(sym))
						{
							done = false;
							res.Value.Remove(sym);
							foreach (var res2 in result)
							{
								if (Equals(res2.Key.Left,sym))
								{
									foreach (var sym2 in res2.Value)
										if (!Equals(sym, sym2) && !res.Value.Contains(sym2))
											res.Value.Add(sym2);
								}
							}
						}
					}
				}
			}
			return result;
		}
		object _TransformId(object id)
		{
			if (null == id) return null;
			Type t = id.GetType();
			if (typeof(object) == t) return new object();
			if (typeof(int) == t)
			{
				while (true)
				{
					id = ((int)id) + 1;
					if (!Symbols.Contains(id))
						return id;
				}
			}
			else if (typeof(string) == t)
			{
				while (true)
				{
					id = ((string)id) + "`";
					if (!Symbols.Contains(id))
						return id;
				}
			}
			
			throw new NotSupportedException("The type of symbol is not supported for this operation.");
		}
		object _LeftFactorId(object id)
		{
			if (null == id) return null;
			Type t = id.GetType();
			if (typeof(object) == t) return new object();
			if (typeof(int) == t)
			{
				while (true)
				{
					id = ((int)id) + 1;
					if (!_EnumSymbols().Contains(id))
						return id;
				}
			}
			else if (typeof(string) == t)
			{
				var s = id as string + "`";
				var i = 2;
				while (_EnumSymbols().Contains(s))
				{
					s = string.Concat(id, "`", i);
					++i;
				}
				return s;
			}

			throw new NotSupportedException("The type of symbol is not supported for this operation.");
		}
		public IDictionary<object, ICollection<object>> FillFollows(IDictionary<CfgRule, ICollection<object>> firsts = null, IDictionary<object, ICollection<object>> result = null)
		{
			if (null == result)
				result = new Dictionary<object, ICollection<object>>();
			if (null == firsts)
				firsts = FillFirsts();
			// create an augmented grammar - add rule {start} -> StartSymbol $
			var ss = StartSymbol;
			var start = new CfgRule(_TransformId(ss), ss, EosSymbol);
			var augmented = new CfgRule[] { start }.Concat(Rules);
			foreach (var rule in augmented)
			{
				object prev = null;
				foreach (var sym in rule.Right)
				{
					// if prev is a nonterminal
					if (IsNonTerminal(prev))
					{
						if (!IsNonTerminal(sym))
						{
							ICollection<object> col;
							if (!result.TryGetValue(prev, out col))
							{
								col = new List<object>();
								result.Add(prev, col);
							}
							if (!col.Contains(sym))
								col.Add(sym);
						}
						else
						{
							foreach (var first in firsts)
							{
								if (Equals(first.Key.Left, sym))
								{
									ICollection<object> col;
									if (!result.TryGetValue(prev, out col))
									{
										col = new List<object>();
										result.Add(prev, col);
									}
									foreach (var fs in first.Value)
									{
										if (null == fs) // epsilon
										{
											if (!col.Contains(rule.Left))
												col.Add(rule.Left);
										}
										else
										{
											if (!col.Contains(fs))
												col.Add(fs);
										}
									}
								}
							}
						}
					}
					prev = sym;
				}
				if (null != prev && IsNonTerminal(prev))
				{
					ICollection<object> col;
					if (!result.TryGetValue(prev, out col))
					{
						col = new List<object>();
						result.Add(prev, col);
					}
					if (!col.Contains(rule.Left))
						col.Add(rule.Left);
				}
			}
			var done = false;
			while (!done)
			{
				done = true;
				foreach (var res in result)
				{
					foreach (var sym in new List<object>(res.Value))
					{
						if (IsNonTerminal(sym))
						{
							done = false;
							res.Value.Remove(sym);

							ICollection<object> fsc;
							if (result.TryGetValue(sym, out fsc))
							{
								foreach (var fs in fsc)
									if (!res.Value.Contains(fs))
										res.Value.Add(fs);

							}
							else if (!Equals(sym, ss)) { // if this isn't the start symbol, WTH happened?!
								// FillFollows is probably broken
								System.Diagnostics.Debugger.Break();
							}

						}
					}
				}
			}
			return result;
		}
		
		public IList<CfgRule> FillRulesForNonTerminal(object nonTerminal, IList<CfgRule> result = null)
		{
			if (null == result)
				result = new List<CfgRule>();
			var ic = Rules.Count;
			for(var i = 0;i<ic;++i)
			{ 
				var rule = Rules[i];
				if(Equals(rule.Left,nonTerminal))
					result.Add(rule);
			}
			return result;
		}
		void _FactorDirectRecursion(CfgRule rule)
		{
			Rules.Remove(rule);
			var newId = _TransformId(rule.Left);

			var col = new List<object>();
			var c = rule.Right.Count;
			for (int i = 1; i < c; ++i)
				col.Add(rule.Right[i]);
			col.Add(newId);
			var d = new Dictionary<string, object>();
			Attributes.Add(newId, d);
			d.Add("collapse", true);
			var newRule = new CfgRule(newId, null);
			newRule.Right.Clear();
			newRule.Right.AddRange(col);
			if(!Rules.Contains(newRule))
				Rules.Add(newRule);
			var rr = new CfgRule(newId, null);
			if(!Rules.Contains(rr))
				Rules.Add(rr);


			foreach (var r in Rules)
			{
				if (Equals(r.Left, rule.Left))
				{
					if (!_IsLeftRecursive(r, null))
					{
						r.Right.Add(newId);
					}
				}
			}
		}
		public IList<CfgMessage> EliminateUnderivableRules()
		{
			var result = new List<CfgMessage>();
			var ss = StartSymbol;
			if (null == ss)
			{
				result.Add(new CfgMessage(CfgErrorLevel.Warning, 9, "No start symbol set. Nothing done."));
				return result;
			}
			var closure = FillClosure(ss);
			foreach (var rule in new List<CfgRule>(Rules))
				if (!closure.Contains(rule.Left))
				{
					Rules.Remove(rule);
					result.Add(new CfgMessage(CfgErrorLevel.Message, 7, string.Concat("Rule ", rule, " removed because it was not referenced.")));
				}
			return result;
		}
		public IList<CfgMessage> EliminateLeftRecursion()
		{
			var result = new List<CfgMessage>();
			// https://www.cs.bgu.ac.il/~comp171/wiki.files/ps5.pdf
			var done = false;
			while (!done)
			{
				done = true;
				foreach (var rule in new List<CfgRule>(Rules))
				{

					if (_IsLeftRecursive(rule, null/*new HashSet<CfgRule>()*/))
					{
						result.Add(new CfgMessage(CfgErrorLevel.Message, 7,string.Concat("Rule ", rule, " modified because it was directly left recursive.")));
						_FactorDirectRecursion(rule);
						done = false;
						break;
					}
					else if (_IsIndirectlyLeftRecursive(rule))
					{
						/*
						result.Add(new CfgMessage(CfgErrorLevel.Message, 7, string.Concat("Rule ", rule, " modified because it was indirectly left recursive.")));
						Rules.Remove(rule);
						var ic = rule.Right.Count;
						var append = new List<object>(ic - 1);
						for (var i = 1; i < ic; ++i)
							append.Add(rule.Right[i]);
						// do indirect left recursion elimination.
						// first make it directly left recursive.
						var dstRules = FillRulesForNonTerminal(rule.Right[0]);
						foreach (var drule in dstRules)
						{
							var newRule = new CfgRule(rule.Left,null);
							newRule.Right.Clear();
							// now add the stuff from the dst rule;
							newRule.Right.AddRange(drule.Right);
							newRule.Right.AddRange(append);
							if(!Rules.Contains(newRule))
								Rules.Add(newRule);
							done = false;
							var nt = _TransformId(rule.Left);
							var allRules = FillRulesForNonTerminal(rule.Left);
							foreach (var ar in allRules)
							{
								// Section 2.3, 3.2
								// TODO: This needs lots more testing
								if (0 == ar.Right.Count)
									ar.Right.Add(null);
								if (!Equals(ar.Right[0], rule.Left))
								{
									var nar = new CfgRule(rule.Left,null);
									nar.Right.Clear();
									nar.Right.AddRange(ar.Right);
									nar.Right.Add(nt);
									if(!Rules.Contains(nar))
										Rules.Add(nar);
									Rules.Remove(ar);

								} else
								{
									ar.Right.RemoveAt(0);
									ar.Left = nt;
									ar.Right.Add(nt);
									var nr2 = new CfgRule(nt, null);
									if (!Rules.Contains(nr2)) 
										Rules.Add(nr2);
								}
								//}
							
							}

							result.AddRange(EliminateUnderivableRules());
						
						break;
							
						}
						
	*/
					}
					if (!done)
						break;
				}
			}
			return result;
		}
		public IList<CfgRule> FillReferencesToSymbol(object symbol, IList<CfgRule> result=null)
		{
			if (null == result)
				result = new List<CfgRule>();
			var ic = Rules.Count;
			for(var i = 0;i <ic;++i)
			{
				var rule = Rules[i];
				if (rule.Right.Contains(symbol))
					if (!result.Contains(rule))
						result.Add(rule);
			}
			return result;
		}
		public IList<object> FillClosure(object symbol,IList<object> result = null)
		{
			if (null == result)
				result = new List<object>();
			else if (result.Contains(symbol))
				return result;
			result.Add(symbol);
			if (!IsNonTerminal(symbol))
				return result;
			foreach (var rule in FillNonTerminalRules(symbol))
				foreach(var sym in rule.Right)
					FillClosure(sym, result);
			return result;
		}
		public IList<object> FillDescendants(object symbol, IList<object> result = null)
		{
			if (null == result)
				result = new List<object>();
			if (!IsNonTerminal(symbol))
				return result;
			foreach (var rule in FillNonTerminalRules(symbol))
				foreach (var sym in rule.Right)
					FillClosure(sym, result);
			return result;
		}
		public IList<object> FillLeftDescendants(object symbol, IList<object> result = null)
		{
			if (null == result)
				result = new List<object>();
			if (!IsNonTerminal(symbol))
				return result;
			foreach (var rule in FillNonTerminalRules(symbol))
				if(0<rule.Right.Count && null!=rule.Right[0])
					FillClosure(rule.Right[0], result);
			return result;
		}
		bool _IsIndirectlyLeftRecursive(CfgRule rule)
		{
			if (_IsLeftRecursive(rule, null))
				return false;
			if (FillLeftDescendants(rule.Left).Contains(rule.Left))
				return true;
			return false;
		}
		bool _IsLeftRecursive(CfgRule rule, ICollection<CfgRule> visited)
		{
			if (null == visited)
			{
				if (0 < rule.Right.Count)
				{
					return Equals(rule.Left, rule.Right[0]);
				}
				return false;
			}
			if (!visited.Contains(rule))
				visited.Add(rule);
			else
				return true;

			if (0 < rule.Right.Count)
			{
				if (Equals(rule.Left,rule.Right[0]))
					return true;
				foreach (var r in Rules)
				{
					if (Equals(r.Left, rule.Right[0]))
						if (_IsLeftRecursive(r, visited))
							return true;
				}
			}
			return false;
		}
		/// <summary>
		/// Indicates whether a symbol is nillable
		/// </summary>
		/// <param name="sym">The symbol to examine</param>
		/// <returns>True if the symbol is nonterminal and derives epsilon. Otherwise, false.</returns>
		public bool IsNillable(object sym)
		{
			if (!IsNonTerminal(sym))
				return false;

			var ic = Rules.Count;
			for(var i = 0;i<ic;++i)
			{
				var rule = Rules[i];
				if (Equals(sym,rule.Left))
				{
					if (rule.IsSingleEpsilon)
						return true;
				}
			}
			return false;
		}
		public IList<CfgRule> FillNonTerminalRules(object nonTerminal,IList<CfgRule> result=null)
		{
			if (null == result) result = new List<CfgRule>();
			var ic = Rules.Count;
			for(var i = 0;i<ic;++i)
			{
				var rule = Rules[i];
				if (Equals(rule.Left, nonTerminal))
					result.Add(rule);
			}
			return result;
		}
		
		public bool IsDirectlyLeftRecursive {
			get {
				var c = Rules.Count;
				for(var i = 0;i<c;++i)
					if (_IsLeftRecursive(Rules[i],null)) return true;
				return false;
			}
		}
		public bool IsIndirectlyLeftRecursive {
			get {
				var c = Rules.Count;
				for (var i = 0; i < c; ++i)
					if (_IsLeftRecursive(Rules[i],new HashSet<CfgRule>())) return true;
				return false;
			}
		}
		
		public LL1ParseTable ToLL1ParseTable()
		{
			LL1ParseTable result;
			CfgException.ThrowIfErrors(TryToLL1ParseTable(out result));
			return result;
		}
		public IList<CfgMessage> TryToLL1ParseTable(out LL1ParseTable result)
		{
			var msgs = new List<CfgMessage>();
			if (IsDirectlyLeftRecursive)
				msgs.Add(new CfgMessage(CfgErrorLevel.Error,1,"Grammar is left recursive"));
			
			result = new Dictionary<int, IDictionary<int, (int Id, int Left, int[] Right)>>();

			var firsts = FillFirsts();
			var follows = FillFollows(firsts);
			IDictionary<int, (int Id, int Left, int[] Right)> d;
			var ruleId = 0;
			foreach (var rule in Rules)
			{
				ruleId = GetRuleId(rule);
				var left = GetSymbolId(rule.Left);
				if (!result.TryGetValue(left, out d))
				{
					d = new Dictionary<int, (int Id, int Left, int[] Right)>();
					
					result.Add(left, d);
				}
				ICollection<object> lt;
				if (firsts.TryGetValue(rule, out lt))
				{
					foreach (var t in lt)
					{
						if (null != t)
						{
							// this is where any LL(1) first-first conflict will happen
							var right = new int[rule.Right.Count];
							for(var i =0;i<right.Length;i++)
								right[i] = GetSymbolId(rule.Right[i]);
							var tid = GetSymbolId(t);
							(int Id, int Left, int[] Right) prevRule;
							if (d.TryGetValue(tid,out prevRule))
							{
								if (prevRule.Id != ruleId)
								{
									msgs.Add(new CfgMessage(CfgErrorLevel.Error, 2, string.Format("FIRST-FIRST conflict between:\r\n{0}\r\n{1}",GetRuleById(prevRule.Id),rule)));
								}
							} else
								d.Add(GetSymbolId(t), (Id: ruleId,Left: left,Right: right));
						}
						else
						{
							ICollection<object> flt;
							if (follows.TryGetValue(rule.Left, out flt))
							{
								var right = new int[rule.Right.Count];
								for (var i = 0; i < right.Length; i++)
									right[i] = GetSymbolId(rule.Right[i]);
								// first-follows conflict if throw
								foreach (var ft in flt)
								{
									var ftid = GetSymbolId(ft);
									(int Id, int Left, int[] Right) prevRule;
									if (d.TryGetValue(ftid,out prevRule))
									{
										if (prevRule.Id != ruleId)
										{
											msgs.Add(new CfgMessage(CfgErrorLevel.Error, 3, string.Format("FIRST-FOLLOWS conflict between:\r\n{0}\r\n{1}", GetRuleById(prevRule.Id), rule)));
										}
									} else
										d.Add(GetSymbolId(ft), (Id: ruleId, Left: left, Right: right));
								}
							}
						}
					}
				}
			}
			return msgs;
		}
		public (IList<KeyValuePair<CfgRule, CfgRule>> FirstFirsts, IList<KeyValuePair<object, CfgRule>> FirstFollows) AnalyzeLL1Conflicts()
		{
			var ffirstc = new List<KeyValuePair<CfgRule, CfgRule>>();
			var ffollowsc = new List<KeyValuePair<object, CfgRule>>();

			var firsts = FillFirsts();
			var follows = FillFollows(firsts);

			foreach (var nt in NonTerminals)
			{
				ICollection<object> ccol;
				if (follows.TryGetValue(nt, out ccol))
					if (IsNillable(nt))
						foreach (var first in firsts)
							if (Equals(first.Key.Left, nt))
								foreach (var ff in first.Value)
									if (ccol.Contains(ff))
									{
										var kvp = new KeyValuePair<object, CfgRule>(nt, first.Key);
										if (!ffollowsc.Contains(kvp))
											ffollowsc.Add(kvp);
									}


			}
			foreach (var nt in NonTerminals)
			{
				var rules = FillNonTerminalRules(nt);
				foreach (var rule in rules)
				{
					//bool found = false;
					foreach (var rule2 in rules)
					{
						if (!ReferenceEquals(rule, rule2))
						{
							var fx = 0<rule.Right.Count?rule.Right[0]:null;
							var fy = 0<rule2.Right.Count?rule2.Right[0]:null;
							var ffx = FillFirsts(fx, firsts);
							var ffy = FillFirsts(fy, firsts);
							foreach(var f in ffx)
							{
								if(ffy.Contains(f))
								{
									var kvp = new KeyValuePair<CfgRule, CfgRule>(rule, rule2);
									// below should have been caught by FIRST-FOLLOWS if it was an error
									if (!kvp.Key.IsSingleEpsilon && !kvp.Value.IsSingleEpsilon)
									{
										var kvp2 = new KeyValuePair<CfgRule, CfgRule>(rule2, rule);
										if (!ffirstc.Contains(kvp) && !ffirstc.Contains(kvp2))
											ffirstc.Add(kvp);
									}
									break;
								}
							}
						}
						
					}
				}
			}
			return (FirstFirsts: ffirstc, FirstFollows: ffollowsc);
		}
		public ICollection<object> FillFirsts(object sym, IDictionary<CfgRule,ICollection<object>> firsts=null, ICollection<object> result=null)
		{
			if (null == result)
				result = new List<object>();
			if (null == firsts)
				firsts = FillFirsts();
			if (IsNonTerminal(sym))
			{
				foreach(var kvp in firsts)
				{
					if(Equals(kvp.Key.Left,sym))
					{
						foreach (var f in kvp.Value)
							if (!result.Contains(f))
								result.Add(f);
					}
				}
			}
			else return new object[] { sym };
			return result;
		}
		public IList<CfgMessage> EliminateFirstFirstConflicts(IList<KeyValuePair<CfgRule,CfgRule>> firstFirstConflicts=null)
		{
			var result = new List<CfgMessage>();
			if (null == firstFirstConflicts)
				firstFirstConflicts = AnalyzeLL1Conflicts().FirstFirsts;
			var cd = new Dictionary<CfgRule, ICollection<CfgRule>>();
			// group first first conflicts
			foreach (var c in firstFirstConflicts)
			{
				ICollection<CfgRule> col;
				if(!cd.TryGetValue(c.Key,out col))
				{
					col = new List<CfgRule>();
					cd.Add(c.Key, col);
				}
				if (!col.Contains(c.Value))
					col.Add(c.Value);
			}
			foreach(var nt in new List<object>(_EnumNonTerminals()))
			{
				var rules = FillNonTerminalRules(nt);
				var rights = new List<IList<object>>();
				foreach (var rule in rules)
					rights.Add(rule.Right);
				while (true)
				{
					var pfx = rights.GetLongestCommonPrefix();
					if (pfx.IsNullOrEmpty())
						break;
					// obv first first conflict
					var nnt = _LeftFactorId(nt);
					
					var suffixes = new List<IList<object>>();
					result.Add(new CfgMessage(CfgErrorLevel.Message, 7, string.Concat("Modifying symbol ", nt)));
					foreach (var rule in rules)
					{
						if (rule.Right.StartsWith(pfx))
						{
							rights.Remove(rule.Right);
							suffixes.Add(new List<object>(rule.Right.SubRange(pfx.Count)));
							Rules.Remove(rule);
						}
					}
					
					var newRule = new CfgRule(nt, null);
					newRule.Right.Clear();
					newRule.Right.AddRange(pfx);
					newRule.Right.Add(nnt);
					if(!Rules.Contains(newRule))
						Rules.Add(newRule);
					foreach (var suffix in suffixes)
					{
						newRule = new CfgRule(nnt, null);
						newRule.Right.Clear();
						newRule.Right.AddRange(suffix);
						if(!Rules.Contains(newRule))
							Rules.Add(newRule);
					}
					var attrs = new Dictionary<string, object>();
					attrs.Add("collapse", true);
					Attributes.Add(nnt, attrs);
					
				}
			}
			return result;
		}
		/// <summary>
		/// Eliminates first-follows conflicts, while trying to preserve rules in the original grammar.
		/// </summary>
		/// <remarks>Using this method in the alternative may lead to first-first conflicts later.</remarks>
		/// <param name="firstFollowsConflicts">The list of first-follows conflicts to work on, or null to recompute.</param>
		public IList<CfgMessage> EliminateFirstFollowsConflicts(IList<KeyValuePair<object,CfgRule>> firstFollowsConflicts=null)
		{
			var result = new List<CfgMessage>();
			// more info: https://www.cs.bgu.ac.il/~comp171/wiki.files/ps5.pdf
			// Section 2.1, 3.1
			if (null == firstFollowsConflicts)
				firstFollowsConflicts = AnalyzeLL1Conflicts().FirstFollows;
			foreach(var c in firstFollowsConflicts)
			{
				// ε-elimination
				// these check for rules of the form B -> <epsilon> which cause conflicts
				// it then performs factoring on the epsilon to remove the rule and thus
				// make the nonterminal non-nillable				
				CfgRule target = null;
				foreach(var rule in FillNonTerminalRules(c.Key))
					if (rule.IsSingleEpsilon)
					{
						target = rule;
						break;
					}
				if (null!=target)
				{
					result.Add(new CfgMessage(CfgErrorLevel.Message, 7, string.Concat("Modifying symbol ", target.Left)));
					// remove the empty.
					Rules.Remove(target);
					// get all rules that reference the taget
					var rrules = FillReferencesToSymbol(target.Left);

					// get every possible derivation of the target.
					foreach (var trule in FillRulesForNonTerminal(target.Left))
					{
						// for each rule that references to the target.
						foreach(var rrule in rrules)
						{
							result.Add(new CfgMessage(CfgErrorLevel.Message, 6, string.Concat("Modifying rule ", rrule)));
							// now add a new cloned rule but where B 
							// is replaced by nothing
							// such that A -> B a d becomes A -> a d
							var newRule = new CfgRule(rrule.Left,null);
							newRule.Right.Clear();
							var l = rrule.Right.Replace(target.Left, new object[0]);
							newRule.Right.AddRange(l);
							if(!Rules.Contains(newRule))
								Rules.Add(newRule);
						}
					}
				}
			}
			return result;
		}
		/// <summary>
		/// Eliminates first-follows conflicts, eliminating rules in the original grammar as needed.
		/// </summary>
		/// <param name="firstFollowsConflicts">The list of first-follows conflicts to work on, or null to recompute.</param>
		public IList<CfgMessage> EliminateFirstFollowsConflicts2(IList<KeyValuePair<object, CfgRule>> firstFollowsConflicts = null)
		{
			var result = new List<CfgMessage>();
			// more info: https://www.cs.bgu.ac.il/~comp171/wiki.files/ps5.pdf
			// Section 2.1, 3.1
			if (null == firstFollowsConflicts)
				firstFollowsConflicts = AnalyzeLL1Conflicts().FirstFollows;
			foreach (var c in firstFollowsConflicts)
			{
				// ε-elimination
				// these check for rules of the form B -> <epsilon> which cause conflicts
				// it then performs factoring on the epsilon to remove the rule
				CfgRule target = null;
				foreach (var rule in FillNonTerminalRules(c.Key))
					if (rule.IsSingleEpsilon)
					{
						target = rule;
						break;
					}
				if (null != target)
				{
					result.Add(new CfgMessage(CfgErrorLevel.Warning, 6, string.Concat("Eliminating symbol ", target.Left, " from the grammar.")));
					var trules = FillRulesForNonTerminal(target.Left);

					// get all rules that reference the taget
					var rrules = FillReferencesToSymbol(target.Left);
					// for each rule that references to the target.
					foreach (var rrule in rrules)
					{
						result.Add(new CfgMessage(CfgErrorLevel.Message, 7, string.Concat("Modifying rule ", rrule)));
						// now add a new cloned rule but where B 
						// is replaced by this particular derivation of target
						// such that A -> B a d becomes A -> a d,
						// and A-> a a d    (for B -> a | <epsilon>)
						// get every possible derivation of the target.
						foreach (var trule in trules)
						{
							var newRule = new CfgRule(rrule.Left, null);
							newRule.Right.Clear();
							var l = rrule.Right.Replace(trule.Left, trule.Right).NonNulls();
							newRule.Right.AddRange(l);
							if(!Rules.Contains(newRule))
								Rules.Add(newRule);

						}
						Rules.Remove(rrule);
					}
					foreach (var trule in trules)
						Rules.Remove(trule);
				}
			}
			return result;
		}
		public LLParser ToLL1Parser(FA lexer, ParseContext pc)
		{
			return new RuntimeLL1Parser(this, lexer, pc);
		}
		public string ToCsvLL1(LL1ParseTable parseTable=null)
		{
			if (null == parseTable)
				parseTable = ToLL1ParseTable();
			var sb = new StringBuilder();
			sb.Append("LL(1) Parse Table");
			var ta = new List<object>();
			foreach (var t in _EnumSymbols())
			{
				if (!IsNonTerminal(t))
				{
					sb.Append(", ");

					ta.Add(t);
					sb.Append(t);
				}
			}
			sb.Append(", ");
			sb.Append(EosSymbol);
			ta.Add(EosSymbol);
			sb.AppendLine();
			foreach(var nt in _EnumSymbols())
			{
				if(IsNonTerminal(nt))
				{
					IDictionary<int, (int RuleId, int Left, int[] Right)> d;
					if (parseTable.TryGetValue(GetSymbolId(nt),out d)) {
						sb.Append(nt);
						foreach (var t in ta)
						{
							sb.Append(", ");
							(int RuleId, int Left, int[] Right) rule;
							if(d.TryGetValue(GetSymbolId(t),out rule))
							{
								sb.Append(GetSymbolById(rule.Left));
								sb.Append(" ->");
								foreach(int j in rule.Right)
								{
									sb.Append(" ");
									if (-3 == j)
										sb.Append("<epsilon>");
									else
										sb.Append(GetSymbolById(j));
								}
								
							}
						}
						sb.AppendLine();
					}
					
				}
			}
			return sb.ToString();
		}		
		public static string ToString(IEnumerable<(int Left,int[] Right)> rules,CfGrammar cfg=null)
		{
			var sb = new StringBuilder();
			foreach (var rule in rules)
				sb.AppendLine(ToString(rule,cfg));
			return sb.ToString();
		}
		public static string ToString((int Left,int[] Right) rule, CfGrammar cfg=null)
		{
			var sb = new StringBuilder();
			object s = (null != cfg) ? cfg.GetSymbolById(rule.Left) : rule.Left;
			sb.Append(s);
			sb.Append(" ->");
			for(var i = 0;i<rule.Right.Length;i++)
			{
				var id = rule.Right[i];
				s = null;
				if (null != cfg)
					s = cfg.GetSymbolById(id);
				else if(-3!=id)
					s = id;
				sb.Append(" ");
				if (null == s)
					s = "<epsilon>";
				sb.Append(s);
			}
			return sb.ToString();
		}
		
		
		#region Equality and Identity, and ToString
		public bool Equals(CfGrammar rhs)
		{
			if (ReferenceEquals(rhs, this))
				return true;
			if (ReferenceEquals(rhs, null))
				return false;
			var c = Rules.Count;
			if (c != rhs.Rules.Count) return false;
			for(var i = 0;i<c;++i)
				if (!Rules[i].Equals(rhs.Rules[i]))
					return false;
			if (Attributes.Count != rhs.Attributes.Count)
				return false;
			foreach (var sattrs in Attributes)
			{
				var lattrs = sattrs.Value;
				IDictionary<string,object> rattrs;
				if (!rhs.Attributes.TryGetValue(sattrs.Key, out rattrs))
					return false;
				if (lattrs.Count != rattrs.Count) return false;
				foreach (var attr in lattrs)
				{
					var lval = attr.Value;
					object rval;
					if (!rattrs.TryGetValue(attr.Key, out rval))
						return false;
					if (!Equals(lval, rval))
						return false;
				}
			}
			return true;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(obj, this))
				return true;
			return Equals(obj as CfGrammar);
		}
		public override int GetHashCode()
		{
			// don't bother hashing attributes. Takes too long and is not likely to improve perf
			var result = 0;
			var c = Rules.Count;
			for (var i = 0; i < c; ++i)
				result ^= Rules[i].GetHashCode();
			return result;
		}
		public override string ToString()
		{
			var sb = new StringBuilder();
			foreach (var rule in Rules)
				sb.AppendLine(rule.ToString());
			return sb.ToString();
		}
		public static bool operator ==(CfGrammar lhs, CfGrammar rhs)
		{
			if (ReferenceEquals(lhs, rhs)) return true;
			if (ReferenceEquals(null, lhs)) return false;
			return lhs.Equals(rhs);
		}
		public static bool operator !=(CfGrammar lhs, CfGrammar rhs)
		{
			if (ReferenceEquals(lhs, rhs)) return false;
			if (ReferenceEquals(null, lhs)) return true;
			return !lhs.Equals(rhs);
		}
		#endregion

		class _IntRuleComparer : IEqualityComparer<(int RuleId, int Left, int[] Right)>,IEqualityComparer<(int Left, int[] Right)>
		{
			public static readonly _IntRuleComparer Default = new _IntRuleComparer();
			public bool Equals((int RuleId, int Left, int[] Right) x, (int RuleId, int Left, int[] Right) y)
			{
				return x.RuleId == y.RuleId &&
					x.Left == y.Left &&
					CollectionUtility.Equals(x.Right, y.Right);
			}

			public bool Equals((int Left, int[] Right) x, (int Left, int[] Right) y)
			{
				return x.Left==y.Left && CollectionUtility.Equals(x.Right, y.Right);
			}

			public int GetHashCode((int RuleId, int Left, int[] Right) obj)
			{
				return obj.RuleId ^ obj.Left ^ CollectionUtility.GetHashCode(obj.Right);
			}

			public int GetHashCode((int Left, int[] Right) obj)
			{
				return obj.Left ^ CollectionUtility.GetHashCode(obj.Right);
			}
		}
		static bool _IsValidIdentifier(string identifier)
		{
			if (string.IsNullOrEmpty(identifier)) return false;
			if (!char.IsLetter(identifier[0]) && '_' != identifier[0])
				return false;
			
			for(var i = 1;i<identifier.Length;i++)
			{
				char ch = identifier[i];
				if (!char.IsLetterOrDigit(ch) && '_' != ch && '-' != ch)
					return false;
			}
			return true;
			
		}
		public void WriteCSharpSymbolConstantsTo(TextWriter writer,string modifiers)
		{
			if (null == modifiers)
				modifiers = "";
			var names = new HashSet<string>();

			writer.WriteLine(string.Concat("\tconst int ERROR=-3;"));
			names.Add("ERROR");
			foreach (var sym in _EnumSymbols())
			{
				var sid = Convert.ToString(sym);
				if (_IsValidIdentifier(sid))
				{
					string s;
					if (!string.IsNullOrEmpty(modifiers))
						s = string.Concat("\t",modifiers, " const int ");
					else
						s = string.Concat("\t",modifiers, "const int ");
					
					var id = GetSymbolId(sym);
					s = string.Concat(s, CS.CreateEscapedIdentifier(sid.Replace('-', '_')), " = ");
					s = _GetUniqueName(names,s);
					names.Add(s);
					s = string.Concat(s, id.ToString(), ";");
					writer.WriteLine(s);
					
				}
			}
			
		}
		static string _GetUniqueName(ICollection<string> names,string name)
		{
			var i = 2;
			var s = name;
			while (names.Contains(s))
			{
				s = string.Concat(name, i.ToString());
				++i;
			}
			return s;
		}

		public void WriteCSharpLL1ParseTableCreateExpressionTo(TextWriter writer, LL1ParseTable parseTable = null)
		{
			// (int RuleId, int Left, int[] Right)[][]
			if (null == parseTable)
				parseTable = ToLL1ParseTable();
			var terms = new List<int>();
			foreach (var term in _EnumTerminals())
				terms.Add(GetSymbolId(term));

			writer.WriteLine("new (int RuleId, int Left, int[] Right)[][] {");
			var delim = "";
			var pkeys = new List<int>(parseTable.Keys);
			pkeys.Sort();
			var pcount = pkeys[pkeys.Count - 1] + 1;
			for (var i = 0;i<pcount;++i)
			{
				writer.Write("\t");
				writer.Write(delim);

				IDictionary<int, (int RuleId, int Left, int[] Right)> d;
				if (!parseTable.TryGetValue(i,out d))
					writer.WriteLine("null");
				else
				{
					writer.WriteLine("new (int RuleId, int Left, int[] Right)[] {");
					var tkeys = new List<int>(terms);
					tkeys.Sort();
					var tcount = tkeys[terms.Count - 1] + 1;
					var delim2 = "\t\t";
					for (var j=0;j<tcount;++j)
					{
						writer.Write(delim2);
						(int RuleId, int Left, int[] Right) t;
						if (!d.TryGetValue(j, out t))
							writer.WriteLine("(-1,-2,null)");
						else
						{
							writer.Write(string.Concat("(", t.RuleId.ToString(), ", ", t.Left.ToString()));
							writer.Write(", new int[] ");
							writer.Write(CollectionUtility.ToString(t.Right));
							writer.WriteLine(")");
						}
						delim2 = "\t\t,";
					}
					if (d.Keys.Contains(-1))
					{
						writer.Write(delim2);
						var t = d[-1];
						writer.Write(string.Concat("(", t.RuleId.ToString(), ", ", t.Left.ToString()));
						writer.Write(", new int[] ");
						writer.Write(CollectionUtility.ToString(t.Right));
						writer.WriteLine(")");
					}
					else
					{
						writer.Write(delim2);

						writer.WriteLine("(-1,-2,null)");
					}
					writer.WriteLine("\t\t}");
				}
				delim = ",";
			}
			if (pkeys.Contains(-1))
				System.Diagnostics.Debugger.Break();
			writer.Write("}");
		}


		public void WriteCSharpTableDrivenLL1ParserClassTo(TextWriter writer, string name, string modifiers = null, FA lexer = null,LL1ParseTable parseTable=null)
		{
			if (string.IsNullOrEmpty(name))
				name = "Parser";
			if (!string.IsNullOrEmpty(modifiers))
				writer.Write(string.Concat(modifiers, " "));
			writer.Write(string.Concat("partial class ", name, " : Grimoire.TableDrivenLL1Parser"));
			writer.WriteLine(" {");
			writer.WriteLine(string.Concat("\tpublic ", name, "(Grimoire.ParseContext parseContext=null) : base(_ParseTable,_StartingConfiguration,_LexTable,_Symbols,_SubstitutionsAndHiddenTerminals,_BlockEnds,_CollapsedNonTerminals,_TerminalTypes,parseContext) { }"));
			WriteCSharpSymbolConstantsTo(writer, "public");
			writer.WriteLine("\tstatic readonly object[] _Symbols = {");
			foreach (var sym in _EnumSymbols())
			{
				writer.Write("\t\t");
				CS.WriteCSharpLiteralTo(writer, sym);
				writer.WriteLine(",");
			}
			writer.Write("\t\t");
			CS.WriteCSharpLiteralTo(writer, EosSymbol);
			writer.WriteLine();
			writer.WriteLine("\t\t};");
			writer.Write("\tstatic readonly (int RuleId, int Left, int[] Right)[][] _ParseTable = ");
			WriteCSharpLL1ParseTableCreateExpressionTo(writer,parseTable);
			writer.WriteLine(";");
			writer.WriteLine();

			writer.Write("\tstatic readonly int[] _SubstitutionsAndHiddenTerminals = new int[] { ");
			foreach (var sym in _EnumSymbols())
			{
				IDictionary<string, object> attrs;
				if (Attributes.TryGetValue(sym, out attrs))
				{
					if ((bool)attrs.TryGetValue("hidden", false))
						writer.Write("-2");
					else
					{
						object sub = attrs.TryGetValue("substitute", null);
						if (null != sub)
							writer.Write(GetSymbolId(sub));
						else
							writer.Write(GetSymbolId(sym));
					}
				}
				else
					writer.Write(GetSymbolId(sym));
				writer.Write(", ");
			}
			writer.WriteLine("-1 };");

			writer.Write("\tstatic readonly (int SymbolId,bool IsNonTerminal) _StartingConfiguration = (");
			var startId = 0;
			var isNonTerminal = true;
			foreach (var sym in _EnumSymbols())
			{
				IDictionary<string, object> attrs;
				if (Attributes.TryGetValue(sym, out attrs))
				{
					if ((bool)attrs.TryGetValue("start", false))
					{
						startId = GetSymbolId(sym);
						if (!IsNonTerminal(sym))
							isNonTerminal = false;
						break;
					}
				}
			}
			writer.Write(string.Concat(startId, ", "));
			CS.WriteCSharpLiteralTo(writer, isNonTerminal);
			writer.WriteLine(");");

			writer.WriteLine("\tstatic readonly string[] _BlockEnds = new string[] { ");
			var delim = "\t";
			foreach (var sym in _EnumSymbols())
			{
				writer.Write(delim);
				IDictionary<string, object> attrs;
				if (Attributes.TryGetValue(sym, out attrs))
				{
					var be = attrs.TryGetValue("blockEnd", null) as string;
					CS.WriteCSharpLiteralTo(writer, be);
					writer.WriteLine();
				}
				else
					writer.WriteLine("null");
				delim = "\t,";
			}
			writer.WriteLine("};");

			writer.WriteLine("\tstatic readonly System.Type[] _TerminalTypes = new System.Type[] { ");
			delim = "\t";
			foreach (var sym in _EnumSymbols())
			{
				writer.Write(delim);
				IDictionary<string, object> attrs;


				if (Attributes.TryGetValue(sym, out attrs))
				{
					object o;

					if (attrs.TryGetValue("type", out o) && !string.IsNullOrEmpty(o as string) && !IsNonTerminal(sym))
					{
						var id = GetSymbolId(sym);

						Type t = null;
						var s = o as string;
						if (!string.IsNullOrEmpty(s))
							t = ParserUtility.ResolveType(s);
						else
							t = o as Type;
						if (null == t)
							throw new InvalidOperationException(string.Concat("Invalid type \"", o, "\"."));
						writer.Write("typeof(");
						writer.Write(t.FullName);
						writer.WriteLine(")");
					}
					else
						writer.Write("null");

					writer.WriteLine();
				}
				else
					writer.WriteLine("null");
				delim = "\t,";
			}
			writer.WriteLine("};");

			writer.WriteLine("\tstatic readonly int[] _CollapsedNonTerminals = new int[] { ");
			delim = "";
			foreach (var sym in _EnumSymbols())
			{
				writer.Write(delim);
				IDictionary<string, object> attrs;
				if (Attributes.TryGetValue(sym, out attrs))
				{
					object be;
					if (attrs.TryGetValue("collapse", out be) && (be is bool) && (bool)be)
						writer.Write(-3);
					else
						writer.Write(-1);
				}
				else
					writer.Write("-1");
				delim = ",";
			}
			writer.WriteLine("};");
			if (null != lexer)
			{
				writer.Write("\tstatic readonly (int Accept, ((char First, char Last)[] Ranges, int Destination)[] Transitions, int[] PossibleAccepts)[] _LexTable = ");
				FA.WriteCSharpDfaTable2CreationExpressionTo(writer, lexer.ToDfaTable2<int>());
				writer.WriteLine(";");
			}
			writer.WriteLine("}");

		}
		
		
	}
	
}
