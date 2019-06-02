// by codewitch honey crisis
// article https://www.codeproject.com/Articles/1280690/A-Regular-Expression-Engine-in-Csharp

// CODEDOM option - Include System.CodeDom code generation support
// comment the below to disable CodeDom support 
// if enabled under .NET Core this requires the 
// System.CodeDom nuget package 
//#define CODEDOM
namespace Grimoire
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using System.Text;
#if CODEDOM
	using System.CodeDom;
#endif


	/// <summary>
	/// Represents a state in a finite state machine.
	/// </summary>	
	/// <remarks>This class is essentially a regular expression engine and code generator.</remarks>
#if GRIMOIRELIB
	public
#else
	internal
#endif
	sealed partial class FA : ICloneable
	{
		/// <summary>
		/// The Accepting Symbol. If this null, the state does not accept.
		/// </summary>
		/// <remarks>For code to be generated from this, it must be an intrinsic type.</remarks>
		public object AcceptingSymbol { get; set; } = null;
		/// <summary>
		/// An arbitrary value associated with this state
		/// </summary>
		/// <remarks>No code is generated for this.</remarks>
		public object Tag { get; set; } = null;
		/// <summary>
		/// The input transitions.
		/// </summary>
		public IDictionary<char, FA> Transitions { get; } = new _TrnsDic();
		/// <summary>
		/// The transitions on epsilon.
		/// </summary>
		public IList<FA> EpsilonTransitions { get; } = new List<FA>();
		/// <summary>
		/// Constructs a new instance of an FA state
		/// </summary>
		public FA() { }
		/// <summary>
		/// Constructs a new instance of an FA state with the specified parameters
		/// </summary>
		/// <param name="accepting">The symbol that this state returns, or null if the state is not accepting.</param>
		/// <param name="tag">The arbitrary tag associated with this symbol, if any.</param>
		/// <remarks>For code generation to function, <paramref name="accepting"/> should be an intrinsic type.</remarks>
		public FA(object accepting, object tag = null)
		{
			AcceptingSymbol = accepting;
			Tag = tag;
		}
		/// <summary>
		/// Clones a state 
		/// </summary>
		public FA Clone()
		{
			return Clone(FillClosure());
		}
		object ICloneable.Clone() { return Clone(); }
		/// <summary>
		/// Clones a state given its closure
		/// </summary>
		/// <param name="closure">The closure of the state to clone</param>
		/// <returns>A new state that is a deep clone of the passed in state.</returns>
		public static FA Clone(IEnumerable<FA> closure)
		{
			IList<FA> lclosure = closure.AsList();
			var newClosure = new FA[lclosure.Count];
			var c = newClosure.Length;
			int i;
			for (i = 0; i < newClosure.Length; i++)
				newClosure[i] = new FA();
			i = 0;
			foreach (var fa in lclosure)
			{
				foreach (var trns in (IDictionary<FA, ICollection<char>>)fa.Transitions)
					((IDictionary<FA, ICollection<char>>)newClosure[i].Transitions).Add(newClosure[lclosure.IndexOf(trns.Key)], new HashSet<char>(trns.Value));
				foreach (var efa in fa.EpsilonTransitions)
					newClosure[i].EpsilonTransitions.Add(newClosure[lclosure.IndexOf(efa)]);
				newClosure[i].AcceptingSymbol = fa.AcceptingSymbol;
				newClosure[i].Tag = fa.Tag;
				++i;
			}
			return newClosure[0];
		}
		private string _RegexEscape(char ch)
		{
			switch (ch)
			{
				case '\r':
					return @"\r";
				case '\n':
					return @"\n";
				case '\t':
					return @"\t";
				case '\v':
					return @"\v";
				case '\f':
					return @"\f";
				case '\0':
					return @"\0";
				case '\a':
					return @"\a";
				case '\b':
					return @"\b";
				case '[':
					return @"\[";
				case ']':
					return @"\]";
				case '-':
					return @"\-";
				case '^':
					return @"\^";
				case '.':
					return @"\.";
				case '{':
					return @"\{";
				case '}':
					return @"\}";
				case '(':
					return @"\(";
				case ')':
					return @"\)";

				default:
					if (!char.IsLetterOrDigit(ch) && !char.IsSeparator(ch) && !char.IsPunctuation(ch) && !char.IsSymbol(ch))
						return string.Concat("\\u", unchecked((ushort)ch).ToString("x4"));
					return ch.ToString();
			}
		}
		void _ToString(StringBuilder sb, IList<FA> visited, IList<KeyValuePair<FA, FA>> loopMarkers)
		{
			if (null == visited)
				visited = new List<FA>();
			else if (visited.Contains(this))
				return;
			visited.Add(this);
			foreach (var lp in loopMarkers)
			{
				if (this.FillEpsilonClosure().Contains(lp.Key))
				{
					sb.Append("(");
				}
			}
			var states = new List<FA>();
			FillEpsilonClosure(states);
			var d = new Dictionary<FA, IList<KeyValuePair<char, char>>>();
			foreach (var fa in states)
			{
				fa.FillInputTransitionRangesGroupedByState(d);
			}
			string delim = "";
			if (1 < d.Count)
				sb.Append('(');

			var sb2 = new StringBuilder();

			foreach (var tr in d)
			{
				sb.Append(delim);
				sb2.Clear();
				int i = 0, j = 0;
				foreach (var rg in tr.Value)
				{

					sb2.Append(_RegexEscape(rg.Key));
					if (rg.Key + 1 == rg.Value || rg.Key == rg.Value - 1)
					{
						sb2.Append(_RegexEscape(rg.Value));
						++i;
					}
					else if (rg.Key != rg.Value)
					{
						sb2.Append('-');
						sb2.Append(_RegexEscape(rg.Value));
						++i;

					}
					++i;
					++j;
				}
				if (1 != i)
				{
					sb.Append('[');
					sb.Append(sb2.ToString());
					sb.Append(']');
				}
				else
					sb.Append(sb2.ToString());
				var visited2 = new List<FA>(visited);

				/*foreach (var lp in loopMarkers)
				{
					if (lp.Key == tr.Key)
						sb.Append("(");
				}*/

				tr.Key._ToString(sb, visited2, loopMarkers);
				foreach (var lp in loopMarkers)
				{
					if (lp.Value == tr.Key)
					{
						visited2 = new List<FA>(visited);
						lp.Key._ToString(sb, visited2, loopMarkers);
						sb.Append(")*");
					}
				}
				delim = "|";
			}
			if (1 < d.Count)
				sb.Append(')');
		}
		/// <summary>
		/// Returns the regular expression that represents the FA
		/// </summary>
		/// <returns>A regular expression</returns>
		/// <remarks>DOES NOT WORK YET. Mostly for debugging. This algorithm is naive and only supports |, *, (), and [] which can make expressions very large, and unreasonable for more complicated expressions with hundreds or sometimes even dozens of states.</remarks>
		public override string ToString()
		{
			var sb = new StringBuilder();
			_ToString(sb, null, FillLoops(FillClosure()));
			return sb.ToString();
		}

		IEnumerable<FA> _EnumClosure(ICollection<FA> visited)
		{
			if (0 < visited.Count)
			{
				foreach (var fa in visited)
					yield return fa;
				yield break;
			}
			visited.Add(this);
			yield return this;
			foreach (var trns in Transitions)
				foreach (var ffa in trns.Value._EnumClosure2(visited))
					yield return ffa;
			foreach (var fa in EpsilonTransitions)
				foreach (var ffa in fa._EnumClosure2(visited))
					yield return ffa;
		}
		IEnumerable<FA> _EnumClosure2(ICollection<FA> visited)
		{
			if (visited.Contains(this))
				yield break;
			visited.Add(this);
			yield return this;
			foreach (var trns in Transitions)
				foreach (var ffa in trns.Value._EnumClosure2(visited))
					yield return ffa;
			foreach (var fa in EpsilonTransitions)
				foreach (var ffa in fa._EnumClosure2(visited))
					yield return ffa;

		}
		/// <summary>
		/// Lazy enumeration of the closure
		/// </summary>
		/// <remarks>Usually, <see cref="FA.FillClosure(IList{FA})"/> will be faster. The exception is when a large closure only needs to be partially enumerated. This is wrapped with a <see cref="System.Collections.Generic.IList{FA}"/>for convenience but the list doesn't have the performance properties of a standard list.</remarks>
		public IList<FA> Closure {
			get {
				return _EnumClosure(new List<FA>()).AsList();
			}
		}
		/// <summary>
		/// Fills a list with a list of all states reachable from this state, including itself.
		/// </summary>
		/// <param name="result">The list to fill. If null, the list will be created.</param>
		/// <returns>The list specified in <paramref name="result"/> or the new list if <paramref name="result"/> was null.</returns>
		public IList<FA> FillClosure(IList<FA> result = null)
		{
			if (null == result)
				result = new List<FA>();
			else if (result.Contains(this))
				return result;
			result.Add(this);
			foreach (var trns in (IDictionary<FA, ICollection<char>>)Transitions)
				trns.Key.FillClosure(result);
			foreach (var fa in EpsilonTransitions)
				fa.FillClosure(result);
			return result;
		}
		/// <summary>
		/// Fills a list with a list of all states reachable from this state on an epsilon transition, including itself.
		/// </summary>
		/// <param name="result">The list to fill. If null, the list will be created.</param>
		/// <returns>The list specified in <paramref name="result"/> or the new list if <paramref name="result"/> was null.</returns>
		public IList<FA> FillEpsilonClosure(IList<FA> result = null)
		{
			if (null == result)
				result = new List<FA>();
			else if (result.Contains(this))
				return result;
			result.Add(this);
			foreach (var fa in EpsilonTransitions)
				fa.FillEpsilonClosure(result);
			return result;
		}
		IEnumerable<FA> _EnumEpsilonClosure(IList<FA> visited)
		{
			if (visited.Contains(this))
				yield break;
			visited.Add(this);
			yield return this;
			foreach (var fa in EpsilonTransitions)
				foreach (var ffa in fa._EnumEpsilonClosure(visited))
					yield return ffa;
		}
		/// <summary>
		/// Lazy enumerates the epsilon closure.
		/// </summary>
		public IList<FA> EpsilonClosure {
			get {
				return _EnumEpsilonClosure(new List<FA>()).AsList();
			}
		}
		/// <summary>
		/// Fills a list with all states reachable from the specified states on an epsilon transition, including themselves.
		/// </summary>
		/// <param name="result">The list to fill. If null, the list will be created.</param>
		/// <returns>The list specified in <paramref name="result"/> or the new list if <paramref name="result"/> was null.</returns>
		public static IList<FA> FillEpsilonClosure(IEnumerable<FA> states, IList<FA> result = null)
		{
			if (null == result)
				result = new List<FA>();
			foreach (var fa in states)
				fa.FillEpsilonClosure(result);
			return result;
		}
		/// <summary>
		/// Returns true if the state has exactly one epsilon transition and no input transitions.
		/// </summary>
		public bool IsNeutral {
			get {
				return null == AcceptingSymbol && 0 == Transitions.Count && 1 == EpsilonTransitions.Count;
			}
		}
		/// <summary>
		/// Fills a list with all neutral states reachable from the specified state, if necessary including itself.
		/// </summary>
		/// <param name="result">The list to fill. If null, the list will be created.</param>
		/// <returns>The list specified in <paramref name="result"/> or the new list if <paramref name="result"/> was null.</returns>
		public IList<FA> FillNeutrals(IList<FA> result = null)
		{
			return FillNeutrals(FillClosure(), result);
		}
		/// <summary>
		/// Fills a list with all neutral states from the specified closure.
		/// </summary>
		/// <param name="closure">The closure to use.</param>
		/// <param name="result">The list to fill. If null, the list will be created.</param>
		/// <returns>The list specified in <paramref name="result"/> or the new list if <paramref name="result"/> was null.</returns>
		public static IList<FA> FillNeutrals(IEnumerable<FA> closure, IList<FA> result = null)
		{
			if (null == closure)
				throw new ArgumentNullException(nameof(closure));
			if (null == result) result = new List<FA>();
			foreach (var fa in closure)
				if (fa.IsNeutral)
					result.Add(fa);
			return result;
		}
		static IEnumerable<FA> _EnumNeutrals(IEnumerable<FA> closure)
		{
			foreach (var fa in closure)
				if (null != fa.AcceptingSymbol)
					yield return fa;
		}
		/// <summary>
		/// Lazy enumerates the neutral states
		/// </summary>
		public IList<FA> Neutrals {
			get {
				return _EnumNeutrals(FillClosure()).AsList();
			}
		}
		/// <summary>
		/// Returns true if the state has no transitions.
		/// </summary>
		public bool IsFinal {
			get {
				return 0 == Transitions.Count && 0 == EpsilonTransitions.Count;
			}
		}
		/// <summary>
		/// Fills a list with all final states reachable from this state.
		/// </summary>
		/// <param name="result">The list to fill. If null, a list is created.</param>
		/// <returns>The list specified in <paramref name="result"/> or the new list that was filled.</returns>
		public IList<FA> FillFinals(IList<FA> result = null)
		{
			return FillFinals(FillClosure(), result);
		}
		/// <summary>
		/// Fills a list with all final states in the specified closure.
		/// </summary>
		/// <param name="closure">The closure to use. Alternately, a collection of states to filter for finals.</param>
		/// <param name="result">The list to fill. If null, a list is created.</param>
		/// <returns>The list specified in <paramref name="result"/> or the new list that was filled.</returns>
		public static IList<FA> FillFinals(IEnumerable<FA> closure, IList<FA> result = null)
		{
			if (null == closure)
				throw new ArgumentNullException(nameof(closure));
			if (null == result) result = new List<FA>();
			foreach (var fa in closure)
				if (fa.IsFinal)
					result.Add(fa);
			return result;
		}
		static IEnumerable<FA> _EnumFinals(IEnumerable<FA> closure)
		{
			foreach (var fa in closure)
				if (null != fa.AcceptingSymbol)
					yield return fa;
		}
		/// <summary>
		/// Lazy enumerates the final states.
		/// </summary>
		public IList<FA> Finals {
			get {
				return _EnumFinals(FillClosure()).AsList();
			}
		}
		/// <summary>
		/// Returns true if the state is an accepting state
		/// </summary>
		public bool IsAccepting { get { return null != AcceptingSymbol; } }
		/// <summary>
		/// Fills a list with all accepting states reachable from this state.
		/// </summary>
		/// <param name="result">The list to fill</param>
		/// <returns>Either <paramref name="result"/> or the new list filled with the accepting states</returns>
		public IList<FA> FillAccepting(IList<FA> result = null)
		{
			return FillAccepting(FillClosure(), result);
		}
		/// <summary>
		/// Fills a list with all accepting states reachable from the specified closure.
		/// </summary>
		/// <param name="closure">The closure of all states, or alternatively, a list of states to filter for accepting states.</param>
		/// <param name="result">The list to fill</param>
		/// <returns>Either <paramref name="result"/> or the new list filled with the accepting states</returns>
		public static IList<FA> FillAccepting(IEnumerable<FA> closure, IList<FA> result = null)
		{
			if (null == closure)
				throw new ArgumentNullException(nameof(closure));
			if (null == result) result = new List<FA>();
			foreach (var fa in closure)
				if (null != fa.AcceptingSymbol)
					result.Add(fa);
			return result;
		}
		static IEnumerable<FA> _EnumAccepting(IEnumerable<FA> closure)
		{
			foreach (var fa in closure)
				if (null != fa.AcceptingSymbol)
					yield return fa;
		}
		/// <summary>
		/// Lazy enumerates the accepting states
		/// </summary>
		public IList<FA> Accepting {
			get {
				return _EnumAccepting(FillClosure()).AsList();
			}
		}

		/// <summary>
		/// Reports if any of the specified states is an accepting state.
		/// </summary>
		/// <param name="states">The states to check</param>
		/// <returns>True if one or more of of the states in <paramref name="states"/> is an accepting state</returns>
		public static bool IsAnyAccepting(IEnumerable<FA> states)
		{
			foreach (var fa in states)
				if (fa.IsAccepting)
					return true;
			return false;
		}

		/// <summary>
		/// Fills a list with all accepting symbols reachable from this state.
		/// </summary>
		/// <param name="result">The list to fill, or null, to create a new list.</param>
		/// <returns>Either <paramref name="result"/> or the new list, filled with the accepting symbols</returns>
		public IList<object> FillAcceptingSymbols(IList<object> result = null)
		{
			return FillAcceptingSymbols(FillClosure(), result);
		}
		private static IEnumerable<object> _EnumAcceptingSymbols(IEnumerable<FA> closure)
		{
			foreach (var fa in closure)
				if (null != fa.AcceptingSymbol)
					yield return fa.AcceptingSymbol;
		}
		/// <summary>
		/// Lazy enumerates the accepting symbols
		/// </summary>
		public IList<object> AcceptingSymbols {
			get {
				return _EnumAcceptingSymbols(FillClosure()).AsList();
			}
		}
		/// <summary>
		/// Fills a list with all accepting symbols reachable from the specified closure.
		/// </summary>
		/// <param name="closure">The closure of all states, or alternatively, a collection of states from which to retrieve accepting symbols.</param>
		/// <param name="result">The list to fill</param>
		/// <returns>Either <paramref name="result"/> or the new list with the reachable accepting symbols</returns>
		public static IList<object> FillAcceptingSymbols(IEnumerable<FA> closure, IList<object> result = null)
		{
			if (null == result) result = new List<object>();
			foreach (var fa in closure)
				if (null != fa.AcceptingSymbol && !result.Contains(fa.AcceptingSymbol))
					result.Add(fa.AcceptingSymbol);
			return result;
		}
		/// <summary>
		/// Returns true if the machine contains no epsilon transitions
		/// </summary>
		public bool IsDfa {
			get {
				return _IsDfa(FillClosure());
			}
		}
		/// <summary>
		/// Returns true if this state is the start point of one or more loops
		/// </summary>
		public bool IsLoop {
			get {
				foreach (var dst in Transitions.Values)
				{
					if (dst.Closure.Contains(this))
						return true;
				}
				foreach (var dst in EpsilonTransitions)
				{
					if (dst.Closure.Contains(this))
						return true;
				}
				return false;
			}
		}
		/// <summary>
		/// Returns true if the machine matches exactly one string.
		/// </summary>
		public bool IsLiteral {
			get {
				if (IsLoop)
					return false;
				if (IsNeutral)
				{
					var fa = EpsilonTransitions[0];
					return fa.IsLiteral || fa.IsAccepting;
				}
				if (Transitions.Count == 1)
				{
					var fa = Transitions.Values.First();
					return fa.IsLiteral || fa.IsAccepting;
				}
				return false;
			}
		}

		static bool _IsDfa(IEnumerable<FA> closure)
		{
			if (null == closure) throw new ArgumentNullException(nameof(closure));
			foreach (var fa in closure)
			{
				if (0 != fa.EpsilonTransitions.Count)
					return false;
			}
			return true;
		}
		/// <summary>
		/// Tries to read the next match from the specied <see cref="Grimoire.ParseContext"/>, with capture
		/// </summary>
		/// <param name="pc">The parse context</param>
		/// <returns>True if the read was successful, otherwise false</returns>
		/// <remarks>The capture buffer will contain all characters consumed. After reading, the current character will either be the character immediately following the match, or the error character.</remarks>
		public bool TryRead(ParseContext pc)
		{
			pc.EnsureStarted();
			IList<FA> states = FillEpsilonClosure();
			bool isAccepting = IsAnyAccepting(states);
			if (-1 == pc.Current)
				return isAccepting; // accept an empty string?
			var next = new List<FA>(Math.Max(states.Count, 8));
			while (0 < states.Count)
			{
				int ch = pc.Current;
				next.Clear();
				foreach (var s in states)
				{
					FA n;
					char k = (char)ch;
					if (s.Transitions.TryGetValue(k, out n))
					{
						if (!next.Contains(n))
							next.Add(n);
					}
				}
				if (0 == next.Count)
					break;
				states = FillEpsilonClosure(next, null);
				isAccepting = IsAnyAccepting(states);
				pc.CaptureCurrent();
				if (-1 == pc.Advance())
					return isAccepting;
			}
			pc.Advance();
			return isAccepting;
		}
		/// <summary>
		/// Tries to skip the next match from the specied <see cref="Grimoire.ParseContext"/> with no capture
		/// </summary>
		/// <param name="pc">The parse context</param>
		/// <returns>True if the skip was successful, otherwise false</returns>
		/// <remarks>There is no mechanism for error recovery when using this method.</remarks>
		public bool TrySkip(ParseContext pc)
		{
			pc.EnsureStarted();
			IList<FA> states = FillEpsilonClosure();
			bool isAccepting = IsAnyAccepting(states);
			if (-1 == pc.Current)
				return isAccepting; // accept an empty string?
			var next = new List<FA>(Math.Max(states.Count, 8));
			while (0 < states.Count)
			{
				int ch = pc.Current;
				next.Clear();
				foreach (var s in states)
				{
					FA n;
					char k = (char)ch;
					if (s.Transitions.TryGetValue(k, out n))
					{
						if (!next.Contains(n))
							next.Add(n);
					}
				}
				if (0 == next.Count)
					break;
				states = FillEpsilonClosure(next, null);
				isAccepting = IsAnyAccepting(states);
				if (-1 == pc.Advance())
					return isAccepting;
			}
			pc.Advance();
			return isAccepting; // && 0==states.Count
		}
		static object _GetAcceptingSymbol(IEnumerable<FA> states, bool firstOnly = false)
		{
			var result = new List<object>();
			foreach (var fa in states)
			{
				if (null != fa.AcceptingSymbol)
				{
					var l = fa.AcceptingSymbol as IList<object>;
					if (null != l)
					{
						foreach (var s in l)
						{
							if (!result.Contains(s))
								result.Add(s);
							if (firstOnly)
								break;
						}
					}
					else
					{
						if (!result.Contains(fa.AcceptingSymbol))
							result.Add(fa.AcceptingSymbol);
					}
				}
			}
			switch (result.Count)
			{
				case 0:
					return null;
				case 1:
					return result[0];
				default:
					return (firstOnly) ? result.First() : result;
			}
		}
		static void _ExpectingDfat((KeyValuePair<char, char>[] Ranges, int Destination)[] trns, ParseContext pc)
		{
			var ranges = new List<KeyValuePair<char, char>>();
			for (int i = 0; i < trns.Length; ++i)
			{
				var trn = trns[i];
				for (int j = 0; j < trn.Item1.Length; ++j)
					ranges.Add(trn.Item1[j]);
			}
			pc.Expecting(_ExpandRanges(ranges).Convert<int>().ToArray());
		}
		static int _GetDfatTransition((KeyValuePair<char, char>[] Ranges, int Destination)[]trns, char ch)
		{
			for (int i = 0; i < trns.Length; ++i)
			{
				var trn = trns[i];
				for (int j = 0; j < trn.Item1.Length; ++j)
				{
					var rg = trn.Ranges[j];
					if (ch >= rg.Key && ch <= rg.Value)
						return trn.Destination;
				}
			}
			return -1; // no state
		}
		/// <summary>
		/// Lexes the the next token from the specifed <see cref="Grimoire.ParseContext"/>
		/// </summary>
		/// <param name="dfaTable">The DFA table to use for lexing</param>
		/// <param name="pc">The parse context to use</param>
		/// <param name="sb">The <see cref="System.Text.StringBuilder"/> that holds the result. If null, a new <see cref="System.Text.StringBuilder"/> will be created. Otherwise the passed in <see cref="System.Text.StringBuilder"/> will be cleared.
		/// <returns>A <see cref="System.Collections.Generic.KeyValuePair" /> that contains the accepting symbol(s) and the captured string.</returns>
		/// <remarks>The capture buffer is not affected.</remarks>
		public static KeyValuePair<object, string> Lex((object Accept, (KeyValuePair<char, char>[] Ranges, int Destination)[])[] dfaTable, ParseContext pc, StringBuilder sb = null)
			=> Lex<object>(dfaTable, pc, sb);
		/// <summary>
		/// Lexes the the next token from the specifed <see cref="Grimoire.ParseContext"/>
		/// </summary>
		/// <typeparam name="TAccept">The type of the accepting symbols</typeparam>
		/// <param name="dfaTable">The DFA table to use for lexing</param>
		/// <param name="pc">The parse context to use</param>
		/// <param name="sb">The <see cref="System.Text.StringBuilder"/> that holds the result. If null, a new <see cref="System.Text.StringBuilder"/> will be created. Otherwise the passed in <see cref="System.Text.StringBuilder"/> will be cleared.
		/// <returns>A <see cref="System.Collections.Generic.KeyValuePair" /> that contains the accepting symbol(s) and the captured string.</returns>
		/// <remarks>The capture buffer is not affected.</remarks>
		public static KeyValuePair<TAccept, string> Lex<TAccept>((TAccept Accept, (KeyValuePair<char, char>[] Ranges, int Destination)[])[] dfaTable, ParseContext pc, StringBuilder sb = null)
		{
			if (null == sb)
				sb = new StringBuilder();
			else
				sb.Clear();
			pc.EnsureStarted();
			int state = 0;
			var dfaEntry = dfaTable[state];
			object acc = dfaEntry.Item1;
			if (-1 == pc.Current)
			{
				if (null == acc)
					_ExpectingDfat(dfaEntry.Item2, pc);
				return new KeyValuePair<TAccept, string>(dfaEntry.Item1, sb.ToString());
			}
			while (true)
			{
				var ns = _GetDfatTransition(dfaEntry.Item2, (char)pc.Current);
				if (-1 == ns)
				{
					if (null == dfaEntry.Item1)
						_ExpectingDfat(dfaEntry.Item2, pc);

					if (typeof(TAccept)==typeof(int) || typeof(TAccept)==typeof(short) || typeof(TAccept)==typeof(long) || typeof(TAccept)==typeof(sbyte))
					{
						if (-1L == Convert.ToInt64(dfaEntry.Item1))
						{

							_ExpectingDfat(dfaEntry.Item2, pc);
						}
					}
					
					return new KeyValuePair<TAccept, string>(dfaEntry.Item1, sb.ToString());
				}
				state = ns;
				dfaEntry = dfaTable[state];
				if (-1 != pc.Current)
					sb.Append((char)pc.Current);
				if (-1 == pc.Advance())
				{
					if (null == dfaEntry.Item1)
						_ExpectingDfat(dfaEntry.Item2, pc);
					if (typeof(TAccept) == typeof(int) || typeof(TAccept) == typeof(short) || typeof(TAccept) == typeof(long) || typeof(TAccept) == typeof(sbyte))
					{
						if (-1L == Convert.ToInt64(dfaEntry.Item1))
							_ExpectingDfat(dfaEntry.Item2, pc);
					}
					return new KeyValuePair<TAccept, string>(dfaEntry.Item1, sb.ToString());
				}
			}
		}
		
		/// <summary>
		/// Lexes the the next token from the specifed <see cref="Grimoire.ParseContext"/>
		/// </summary>
		/// <param name="pc">The parse context to use</param>
		/// <param name="sb">The <see cref="System.Text.StringBuilder"/> that holds the result. If null, a new <see cref="System.Text.StringBuilder"/> will be created. Otherwise the passed in <see cref="System.Text.StringBuilder"/> will be cleared.
		/// <returns>A <see cref="System.Collections.Generic.KeyValuePair" /> that contains the accepting symbol(s) and the captured string.</returns>
		/// <remarks>The capture buffer is not affected.</remarks>
		[DebuggerHidden()]
		public KeyValuePair<object, string> Lex(ParseContext pc, StringBuilder sb = null)
		{
			if (null == sb)
				sb = new StringBuilder();
			else
				sb.Clear();
			pc.EnsureStarted();
			IList<FA> states = FillEpsilonClosure();
			bool isAccepting = IsAnyAccepting(states);
			if (-1 == pc.Current)
			{
				if (!isAccepting)
				{
					var ex = new List<int>();
					foreach (var s in states)
					{
						foreach (char ch in s.Transitions.Keys)
							ex.Add(ch);
					}
					pc.Expecting(ex.ToArray());
				}
				return new KeyValuePair<object, string>(_GetAcceptingSymbol(states, true), sb.ToString()); // accept an empty string?
			}
			var next = new List<FA>(Math.Max(states.Count, 8));
			while (0 < states.Count)
			{
				int ch = pc.Current;
				next.Clear();
				foreach (var s in states)
				{
					FA n;
					char k = (char)ch;
					if (s.Transitions.TryGetValue(k, out n))
					{
						if (!next.Contains(n))
							next.Add(n);
					}
				}
				if (0 == next.Count)
					break;
				sb.Append((char)pc.Current);
				states = FillEpsilonClosure(next, null);
				isAccepting = IsAnyAccepting(states);
				if (-1 == pc.Advance())
				{
					if (!isAccepting)
					{
						var ex = new List<int>();
						foreach (var s in states)
						{
							foreach (char ch2 in s.Transitions.Keys)
								ex.Add(ch2);
						}
						pc.Expecting(ex.ToArray());
					}
					return new KeyValuePair<object, string>(_GetAcceptingSymbol(states, true), sb.ToString());
				}
			}
			if (isAccepting)
			{
				return new KeyValuePair<object, string>(_GetAcceptingSymbol(states, true), sb.ToString()); // && 0==states.Count
			}
			else
			{
				var ex = new List<int>();
				foreach (var s in states)
				{
					foreach (char ch2 in s.Transitions.Keys)
						ex.Add(ch2);
				}
				pc.Expecting(ex.ToArray());
				throw new Exception("Failure in runtime lexer.");
				
			}
		}
		/// <summary>
		/// Attempts to lex the next token from the specified <see cref="Grimoire.ParseContext"/>, with capture
		/// </summary>
		/// <param name="pc">The parse context</param>
		/// <param name="result">The <see cref="System.Collections.KeyValuePair"/> that contains the token</param>
		/// <returns>True if successful, otherwise false. The capture contains the consumed input.</returns>
		/// <remarks>On success, the cursor is advanced past the token. On error, the current character is over the error.</remarks>
		public bool TryLex(ParseContext pc, out KeyValuePair<object, string> result)
		{
			int l = pc.CaptureBuffer.Length;
			pc.EnsureStarted();
			IList<FA> states = FillEpsilonClosure();
			bool isAccepting = IsAnyAccepting(states);
			if (-1 == pc.Current)
			{
				if (!isAccepting)
				{
					result = default(KeyValuePair<object, string>);
					return false;
				}
				result = new KeyValuePair<object, string>(_GetAcceptingSymbol(states, true), pc.GetCapture(l)); // accept an empty string?
				return true;
			}
			var next = new List<FA>(Math.Max(states.Count, 8));
			while (0 < states.Count)
			{
				int ch = pc.Current;
				next.Clear();
				foreach (var s in states)
				{
					FA n;
					char k = (char)ch;
					if (s.Transitions.TryGetValue(k, out n))
					{
						if (!next.Contains(n))
							next.Add(n);
					}
				}
				if (0 == next.Count)
					break;
				pc.CaptureCurrent();
				states = FillEpsilonClosure(next, null);
				isAccepting = IsAnyAccepting(states);
				if (-1 == pc.Advance())
				{
					if (!isAccepting)
					{
						result = default(KeyValuePair<object, string>);
						return false;
					}
					result = new KeyValuePair<object, string>(_GetAcceptingSymbol(states, true), pc.GetCapture(l));
					return true;
				}
			}
			result = new KeyValuePair<object, string>(_GetAcceptingSymbol(states, true), pc.GetCapture(l)); // && 0==states.Count
			return true;
		}
		/// <summary>
		/// Moves from the current <paramref name="states"/> to the next set of states based on <paramref name="input"/>
		/// </summary>
		/// <param name="states">The states to move from</param>
		/// <param name="input">The input to move on</param>
		/// <returns>The states moved to</returns>
		public static IList<FA> Move(IEnumerable<FA> states, char input)
		{
			var fas = FillEpsilonClosure(states);
			int fac = fas.Count;
			var result = new List<FA>();
			for (int i = 0; i < fac; ++i)
			{
				var fa = fas[i];
				FA dst;
				if (fa.Transitions.TryGetValue(input, out dst))
					if (!result.Contains(dst))
						result.Add(dst);
			}
			return result;
		}
		/// <summary>
		/// Writes a Lex method in C# 
		/// </summary>
		/// <param name="writer">The <see cref="TextWriter"/> with which to emit the code</param>
		/// <param name="name">The name of FSM. This name will be appended to the method name.</param>
		/// <param name="access">The access level of the generated function. Can be "public", "private", "internal" or "" or null</param>
		public void WriteCSharpLexMethodTo(TextWriter writer, string name, string access = null)
		{
			_WriteLexMethodTo(FillClosure(), writer, name, access, false);
		}
		/// <summary>
		/// Writes a Lex method in C#
		/// </summary>
		/// <param name="closure">The closure of all states</param>
		/// <param name="writer">The <see cref="TextWriter"/> with which to emit the code</param>
		/// <param name="name">The name of FSM. This name will be appended to the method name.</param>
		/// <param name="access">The access level of the generated function. Can be "public", "private", "internal" or "" or null</param>
		public static void WriteCSharpLexMethodTo(IEnumerable<FA> closure, TextWriter writer, string name, string access = null)
		{
			_WriteLexMethodTo(closure, writer, name, access, false);
		}

		/// <summary>
		/// Writes a TryLex method in C#
		/// </summary>
		/// <param name="writer">The <see cref="TextWriter"/> with which to emit the code</param>
		/// <param name="name">The name of FSM. This name will be appended to the method name.</param>
		/// <param name="access">The access level of the generated function. Can be "public", "private", "internal" or "" or null</param>
		public void WriteCSharpTryLexMethodTo(TextWriter writer, string name, string access = null)
		{
			_WriteLexMethodTo(FillClosure(), writer, name, access, true);
		}
		/// <summary>
		/// Writes a TryLex method in C#
		/// </summary>
		/// <param name="closure">The closure of all states</param>
		/// <param name="writer">The <see cref="TextWriter"/> with which to emit the code</param>
		/// <param name="name">The name of FSM. This name will be appended to the method name.</param>
		/// <param name="access">The access level of the generated function. Can be "public", "private", "internal" or "" or null</param>
		public static void WriteTryLexMethodTo(IEnumerable<FA> closure, TextWriter writer, string name, string access = null)
		{
			_WriteLexMethodTo(closure, writer, name, access, true);
		}
		public static void WriteCSharpDfaTableCreationExpressionTo(TextWriter writer, (object Accept, (KeyValuePair<char, char>[] Ranges, int Destination)[])[] dfaTable)
			=> WriteCSharpDfaTableCreationExpressionTo<object>(writer,dfaTable);
		public static void WriteCSharpDfaTableCreationExpressionTo<TAccept>(TextWriter writer,(TAccept Accept, (KeyValuePair<char, char>[] Ranges, int Destination)[])[] dfaTable)
		{
			writer.WriteLine(string.Concat("new (",typeof(TAccept).FullName," Accept, (System.Collections.Generic.KeyValuePair<char, char>[] Ranges, int Destination)[])[] {"));
			var delim = "";
			for (int i = 0; i < dfaTable.Length; ++i)
			{
				writer.Write(delim);
				var dfaEntry = dfaTable[i];
				writer.Write("\t");
				writer.WriteLine("(");
				writer.Write("\t\t");
				CSharpUtility.WriteCSharpLiteralTo(writer, dfaEntry.Item1);
				writer.WriteLine(",");
				writer.WriteLine("\t\tnew (System.Collections.Generic.KeyValuePair<char,char>[] Ranges,int Destination)[] {");
				var delim2 = "";
				for (int j = 0; j < dfaEntry.Item2.Length; ++j)
				{
					writer.Write(delim2);
					var trns = dfaEntry.Item2[j];
					writer.WriteLine("\t\t\t(");
					writer.WriteLine("\t\t\t\tnew System.Collections.Generic.KeyValuePair<char,char>[] {");
					var delim3 = "";
					for (int k = 0; k < trns.Item1.Length; ++k)
					{
						writer.Write(delim3);
						var rg = trns.Item1[k];
						writer.Write("\t\t\t\t\tnew System.Collections.Generic.KeyValuePair<char,char>(");
						CSharpUtility.WriteCSharpCharTo(writer, rg.Key);
						writer.Write(",");
						CSharpUtility.WriteCSharpCharTo(writer, rg.Value);
						writer.WriteLine(")");
						delim3 = ",";
					}
					writer.WriteLine("\t\t\t\t\t},");
					writer.Write("\t\t\t\t");
					writer.WriteLine(trns.Item2.ToString());
					writer.WriteLine("\t\t\t\t)");
					delim2 = ",";
				}
				writer.Write("}");
				writer.Write(")");
				delim = ",";
			}
			writer.WriteLine("}");
		}
#if CODEDOM
		static CodeExpression _MakeBinOps(IEnumerable exprs, CodeBinaryOperatorType type)
		{
			var result = new CodeBinaryOperatorExpression();
			foreach (CodeExpression expr in exprs)
			{
				result.Operator = type;
				if (null == result.Left)
				{
					result.Left = expr;
					continue;
				}
				if (null == result.Right)
				{
					result.Right = expr;
					continue;
				}
				result = new CodeBinaryOperatorExpression(result, type, expr);
			}
			if (null == result.Right)
				return result.Left;
			return result;
		}
		public static CodeArrayCreateExpression GenerateDfaTableCreationExpression(Tuple<object, Tuple<KeyValuePair<char, char>[], int>[]>[] dfaTable)
			=> GenerateDfaTableCreationExpression<object>(dfaTable);


		public static CodeArrayCreateExpression GenerateDfaTableCreationExpression<TAccept>(Tuple<TAccept, Tuple<KeyValuePair<char, char>[], int>[]>[] dfaTable)
		{
			var result = new CodeArrayCreateExpression();
			var rgtype = new CodeTypeReference(typeof(KeyValuePair<,>));
			rgtype.TypeArguments.Add(new CodeTypeReference(typeof(char)));
			rgtype.TypeArguments.Add(new CodeTypeReference(typeof(char)));
			var trntype = new CodeTypeReference(typeof(Tuple<,>));
			trntype.TypeArguments.Add(new CodeTypeReference(rgtype, 1));
			trntype.TypeArguments.Add(new CodeTypeReference(typeof(int)));
			var setype = new CodeTypeReference(typeof(Tuple<,>));
			setype.TypeArguments.Add(new CodeTypeReference(typeof(TAccept)));
			setype.TypeArguments.Add(new CodeTypeReference(trntype, 1));
			result.CreateType = setype;
			for (int i = 0; i < dfaTable.Length; ++i)
			{
				var dfaEntry = dfaTable[i];
				var ta = new CodeArrayCreateExpression(trntype);
				for (int j = 0; j < dfaEntry.Item2.Length; ++j)
				{
					var trn = dfaEntry.Item2[j];
					var ra = new CodeArrayCreateExpression(rgtype);
					for (int k = 0; k < trn.Item1.Length; ++k)
					{
						var rg = trn.Item1[k];
						ra.Initializers.Add(new CodeObjectCreateExpression(rgtype, new CodePrimitiveExpression(rg.Key), new CodePrimitiveExpression(rg.Value)));
					}
					ta.Initializers.Add(
						new CodeObjectCreateExpression(trntype, ra,
							new CodePrimitiveExpression(trn.Item2))
						);
				}
				result.Initializers.Add(new CodeObjectCreateExpression(
					setype, new CodePrimitiveExpression(dfaEntry.Item1),
					ta
					));
			}
			return result;
		}
		public CodeMemberMethod GenerateLexMethod(string name, MemberAttributes attributes)
		{
			return _GenerateLexMethod(FillClosure(), name, attributes, false);
		}
		public static CodeMemberMethod GenerateLexMethod(IEnumerable<FA> closure, string name, MemberAttributes attributes)
		{
			return _GenerateLexMethod(closure, name, attributes, false);
		}
		public CodeMemberMethod GenerateTryLexMethod(string name, MemberAttributes attributes)
		{
			return _GenerateLexMethod(FillClosure(), name, attributes, true);
		}
		public static CodeMemberMethod GenerateTryLexMethod(IEnumerable<FA> closure, string name, MemberAttributes attributes)
		{
			return _GenerateLexMethod(closure, name, attributes, true);
		}
		static CodeMemberMethod _GenerateLexMethod(IEnumerable<FA> closure, string name, MemberAttributes attributes, bool tryLex)
		{
			var result = new CodeMemberMethod();
			result.Attributes = attributes | MemberAttributes.Static;
			FA fa;
			fa = closure.First();
			if (!_IsDfa(closure))
			{
				fa = fa.ToDfa();
				fa.TrimDuplicates();
				closure = fa.FillClosure();
			}
			var al = FillAcceptingSymbols(closure, null);
			for (int j = 0; j < al.Count; ++j)
			{
				var e = al[j] as IEnumerable;
				if (al[j] is string) e = null;
				if (null != e)
				{
					al[j] = e.First();
				}
			}
			Type t = al.InferElementType();
			var cet = new CodeTypeReference(t);
			var kvpt = new CodeTypeReference(typeof(KeyValuePair<,>));
			kvpt.TypeArguments.Add(t);
			kvpt.TypeArguments.Add(typeof(string));
			if (!tryLex)
			{
				result.ReturnType = kvpt;
				result.Name = string.Concat("Lex", name);
				var pcp = new CodeParameterDeclarationExpression(typeof(ParseContext), "pc");
				result.Parameters.Add(pcp);
				var sbp = new CodeParameterDeclarationExpression(typeof(StringBuilder), "sb");
				result.Parameters.Add(sbp);
			}

			else
			{
				result.ReturnType = new CodeTypeReference(typeof(bool));
				result.Name = string.Concat("TryLex", name);
				var pcp = new CodeParameterDeclarationExpression(typeof(ParseContext), "pc");
				result.Parameters.Add(pcp);
				var resp = new CodeParameterDeclarationExpression(kvpt, "result");
				resp.Direction = FieldDirection.Out;
				result.Parameters.Add(resp);
			}
			var ld = new CodeVariableDeclarationStatement(typeof(int), "l");
			var lr = new CodeVariableReferenceExpression(ld.Name);
			var pcr = new CodeArgumentReferenceExpression(result.Parameters[0].Name);
			var stmts = result.Statements;
			stmts.Add(new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(pcr, "EnsureStarted")));
			if (!tryLex)
			{
				var ccs = new CodeConditionStatement(
					new CodeBinaryOperatorExpression(
						new CodePrimitiveExpression(null),
						CodeBinaryOperatorType.IdentityEquality,
						new CodeArgumentReferenceExpression(result.Parameters[1].Name)
						)
					);
				ccs.TrueStatements.Add(
						new CodeAssignStatement(
							new CodeArgumentReferenceExpression(result.Parameters[1].Name),
							new CodeObjectCreateExpression(new CodeTypeReference(typeof(StringBuilder)))
						)
					);
				ccs.FalseStatements.Add(
					new CodeMethodInvokeExpression(
						new CodeMethodReferenceExpression(
								new CodeArgumentReferenceExpression(result.Parameters[1].Name),
								"Clear"
							)
						)
					);
				stmts.Add(ccs);
			}
			else
			{
				ld.InitExpression = new CodePropertyReferenceExpression(new CodePropertyReferenceExpression(pcr, "Capture"), "Length");
				stmts.Add(ld);
			}
			int i = 0;
			foreach (var ffa in closure)
			{
				if (0 != i || ffa.IsLoop)
				{
					stmts.Add(new CodeLabeledStatement("q" + i.ToString(), new CodeCommentStatement("state q" + i.ToString())));
				}
				else
					stmts.Add(new CodeCommentStatement("state q" + i.ToString()));

				var itr = ffa.FillInputTransitionRangesGroupedByState(null);
				var ranges = new List<KeyValuePair<char, char>>();
				foreach (var kvp in itr)
				{
					var pccr = new CodePropertyReferenceExpression(new CodeArgumentReferenceExpression(result.Parameters[0].Name), "Current");
					var cif = new CodeConditionStatement();
					stmts.Add(cif);
					var exprs = new CodeExpressionCollection();
					//w.Write("\tif(");

					foreach (var rg in kvp.Value) // each range in ranges
					{
						ranges.Add(rg);
						if (!rg.Key.Equals(rg.Value))
						{
							exprs.Add(
								new CodeBinaryOperatorExpression(
									new CodeBinaryOperatorExpression(
										pccr,
										CodeBinaryOperatorType.GreaterThanOrEqual,
										new CodePrimitiveExpression((int)rg.Key)
										),
									CodeBinaryOperatorType.BooleanAnd,
									new CodeBinaryOperatorExpression(
										pccr,
										CodeBinaryOperatorType.LessThanOrEqual,
										new CodePrimitiveExpression((int)rg.Value)
										)
									)
								);
						}
						else
						{
							exprs.Add(
								new CodeBinaryOperatorExpression(
									pccr,
									CodeBinaryOperatorType.ValueEquality,
									new CodePrimitiveExpression((int)rg.Key)
									)
								);
						}
					}
					var sbr = new CodeArgumentReferenceExpression(result.Parameters[1].Name);
					cif.Condition = _MakeBinOps(exprs, CodeBinaryOperatorType.BooleanOr);
					if (!tryLex)
					{
						cif.TrueStatements.Add(
							new CodeMethodInvokeExpression(sbr, "Append", new CodeCastExpression(typeof(char), pccr))
							);
					}
					else
					{
						cif.TrueStatements.Add(
						new CodeMethodInvokeExpression(pcr, "CaptureCurrent")
						);

					}
					cif.TrueStatements.Add(
						new CodeMethodInvokeExpression(pcr, "Advance")
						);
					cif.TrueStatements.Add(
						new CodeGotoStatement("q" + closure.IndexOf(kvp.Key))
						);
				}
				if (ffa.IsAccepting)
				{
					var o = ffa.AcceptingSymbol;
					var e = o as IEnumerable;
					if (o is string) e = null;
					if (null != e)
						o = e.First();
					CodeExpression capt;
					if (!tryLex)
					{
						capt = new CodeMethodInvokeExpression(new CodeArgumentReferenceExpression(result.Parameters[1].Name), "ToString");
					}
					else
					{
						capt = new CodeMethodInvokeExpression(pcr, "GetCapture", lr);
					}
					var robj = new CodeObjectCreateExpression(kvpt, new CodePrimitiveExpression(o), capt);

					if (tryLex)
					{
						stmts.Add(new CodeAssignStatement(new CodeArgumentReferenceExpression(result.Parameters[1].Name), robj));
						stmts.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression(true)));
					}
					else
					{
						stmts.Add(new CodeMethodReturnStatement(robj));
					}
				}
				else
				{
					if (!tryLex)
					{
						var es = new CodeMethodInvokeExpression(pcr, "Expecting");
						foreach (char ch in _ExpandRanges(ranges))
							es.Parameters.Add(new CodePrimitiveExpression(ch));
						stmts.Add(es);
						stmts.Add(new CodeMethodReturnStatement(new CodeDefaultValueExpression(kvpt)));
					}
					else
					{
						stmts.Add(new CodeAssignStatement(new CodeArgumentReferenceExpression(result.Parameters[1].Name), new CodeDefaultValueExpression(kvpt)));
						stmts.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression(false)));
					}
				}
				++i;
			}
			return result;
		}
#endif
		static void _WriteLexMethodTo(IEnumerable<FA> closure, TextWriter w, string name, string access, bool tryLex)
		{
			FA fa;
			fa = closure.First();
			if (!_IsDfa(closure))
			{
				fa = fa.ToDfa();
				fa.TrimDuplicates();
				closure = fa.FillClosure();
			}
			var al = FillAcceptingSymbols(closure, null);
			for (int j = 0; j < al.Count; ++j)
			{
				var e = al[j] as IEnumerable;
				if (al[j] is string) e = null;
				if (null != e)
				{
					al[j] = e.First();
				}
			}
			Type t = al.InferElementType();
			if (!string.IsNullOrEmpty(access))
			{
				w.Write(access);
				w.Write(" ");
			}
			if (!tryLex)
				w.Write("System.Collections.Generic.KeyValuePair<{0},string> Lex{1}(Grimoire.ParseContext pc, System.Text.StringBuilder sb=null) ", t.FullName, name);
			else
			{
				w.Write("bool TryLex{1}(Grimoire.ParseContext pc, out System.Collections.Generic.KeyValuePair<{0},string> result) ", t.FullName, name);
			}
			w.WriteLine("{");
			w.WriteLine("\tpc.EnsureStarted();");
			if (!tryLex)
				w.WriteLine("\tif(null==sb) sb = new System.Text.StringBuilder(); else sb.Clear();");
			else
				w.WriteLine("\tint l = pc.CaptureBuffer.Length;");
			int i = 0;
			foreach (var ffa in closure)
			{
				if (0 != i || ffa.IsLoop)
					w.WriteLine("q{0}:", i);
				var itr = ffa.FillInputTransitionRangesGroupedByState(null);
				var ranges = new List<KeyValuePair<char, char>>();
				foreach (var kvp in itr)
				{
					w.Write("\tif(");
					string delim = "";
					foreach (var rg in kvp.Value) // each range in ranges
					{
						ranges.Add(rg);
						w.Write(delim);
						if (!rg.Key.Equals(rg.Value))
						{
							w.Write("(pc.Current>=");
							CSharpUtility.WriteCSharpLiteralTo(w, rg.Key);
							w.Write("&& pc.Current<=");
							CSharpUtility.WriteCSharpLiteralTo(w, rg.Value);
							w.Write(")");
						}
						else
						{
							w.Write("(pc.Current==");
							CSharpUtility.WriteCSharpLiteralTo(w, rg.Key);
							w.Write(")");

						}
						delim = string.Concat("||", Environment.NewLine, "\t\t");
					}
					w.WriteLine(") {");
					if (!tryLex)
						w.WriteLine("\t\tsb.Append((char)pc.Current);");
					else
						w.WriteLine("\t\tpc.CaptureCurrent();");
					w.WriteLine("\t\tpc.Advance();");
					w.WriteLine("\t\tgoto q{0};", closure.IndexOf(kvp.Key));
					w.WriteLine("\t}");

				}
				if (ffa.IsAccepting)
				{
					if (tryLex)
						w.Write("\tresult=");
					else
						w.Write("\treturn ");
					w.Write("new System.Collections.Generic.KeyValuePair<{0},string>(", t.FullName);
					var o = ffa.AcceptingSymbol;
					var e = o as IEnumerable;
					if (o is string) e = null;
					if (null != e)
						o = e.First();
					CSharpUtility.WriteCSharpLiteralTo(w, o);
					if (!tryLex)
						w.WriteLine(",sb.ToString());");
					else
					{
						w.WriteLine(",pc.GetCapture(l));");
						w.WriteLine("\treturn true;");
					}
				}
				else
				{
					if (!tryLex)
					{
						w.Write("\tpc.ThrowExpectingRanges(new int[] {");
						var delim = "";
						var j = 0;
						foreach (var kvp in ranges)
						{
							w.Write(delim);
							if (-1 == kvp.Key)
								w.Write("-1");
							else
								CSharpUtility.WriteCSharpCharTo(w, kvp.Key);
							w.Write(",");
							if (-1 == kvp.Value)
								w.Write("-1");
							else
								CSharpUtility.WriteCSharpCharTo(w, kvp.Value);

							delim = ",";

							if (49 == j)
							{
								j = 0;
								delim = Environment.NewLine + ",";
							}
							++j;
						}
						w.WriteLine("});");
						if (tryLex)
							w.Write("\tresult=");
						else
							w.Write("\treturn ");
						w.WriteLine("default(System.Collections.Generic.KeyValuePair<{0},string>);", t.FullName);
						if (tryLex)
							w.WriteLine("\treturn false;");
					}
					else
					{
						w.WriteLine("\tresult=default(System.Collections.Generic.KeyValuePair<{0},string>);", t.FullName);
						w.WriteLine("\treturn false;");
					}
				}
				++i;
			}
			w.WriteLine("}");
		}
		/// <summary>
		/// Returns a <see cref="IDictionary{FA,IList{KeyValuePair{Char,Char}}}"/>, keyed by state, that contains all of the outgoing local input transitions, expressed as a series of ranges
		/// </summary>
		/// <param name="result">The <see cref="IDictionary{FA,IList{KeyValuePair{Char,Char}}}"/> to fill, or null to create one.</param>
		/// <returns>A <see cref="IDictionary{FA,IList{KeyValuePair{Char,Char}}}"/> containing the result of the query</returns>
		public IDictionary<FA, IList<KeyValuePair<char, char>>> FillInputTransitionRangesGroupedByState(IDictionary<FA, IList<KeyValuePair<char, char>>> result = null)
		{
			if (null == result)
				result = new Dictionary<FA, IList<KeyValuePair<char, char>>>();
			/*foreach (var trns in Transitions)
			{
				IList<KeyValuePair<char, char>> ranges;
				if (result.TryGetValue(trns.Value as FA, out ranges))
				{
					_AddValueToRanges(ranges, trns.Key);
				}
				else
				{
					ranges = new List<KeyValuePair<char, char>>();
					ranges.Add(new KeyValuePair<char, char>(trns.Key, trns.Key));
					result.Add(trns.Value as FA, ranges);
				}
				
			}*/
			// using the optimized dictionary we have little to do here.
			foreach (var trns in (IDictionary<FA, ICollection<char>>)Transitions)
			{
				result.Add(trns.Key, new List<KeyValuePair<char, char>>(_GetRanges(trns.Value)));
			}
			return result;
		}

		/// <summary>
		/// Fills a list with the references to <paramref name="target"/> from the state machine indicated by <paramref name="closure"/>
		/// </summary>
		/// <param name="closure">The set of all states</param>
		/// <param name="target">The state to find the references for</param>
		/// <param name="result">A list to fill with the states that refer to <paramref name="target"/>, or null for a new list to be created</param>
		/// <returns>The new list, or the passed in list, filled with states that refer to <paramref name="target"/></returns>
		public static IList<FA> FillReferences(IEnumerable<FA> closure, FA target, IList<FA> result = null)
		{
			if (null == result)
				result = new List<FA>();
			foreach (var fa in closure)
			{
				if (!result.Contains(fa))
				{
					var found = false;
					foreach (var trns in (IDictionary<FA, ICollection<char>>)fa.Transitions)
					{
						if (trns.Key == target)
						{
							found = true;
							result.Add(fa);
							break;
						}
					}
					if (!found)
					{
						foreach (var efa in fa.EpsilonTransitions)
						{
							if (efa == target)
							{
								result.Add(fa);
								break;
							}
						}
					}
				}
			}
			return result;
		}
		/// <summary>
		/// Indicates if two states are duplicates of each other
		/// </summary>
		/// <param name="rhs">The <see cref="FA"/> to compare</param>
		/// <returns>True if the two states are duplicates, otherwise false.</returns>
		public bool IsDuplicate(FA rhs)
		{
			return null != rhs &&
				AcceptingSymbol == rhs.AcceptingSymbol &&
				_SetComparer.Default.Equals((ICollection<FA>)EpsilonTransitions, rhs.EpsilonTransitions) &&
				_SetComparer.Default.Equals((IDictionary<FA, ICollection<char>>)Transitions, (IDictionary<FA, ICollection<char>>)rhs.Transitions);
		}
		/// <summary>
		/// Fills a <see cref="IDictionary{FA, ICollection{FA}}"/> with all duplicates in this machine, grouped by each duplicate state
		/// </summary>
		/// <param name="result">The <see cref="IDictionary{FA, ICollection{FA}}"/> to fill, or null to create a new one</param>
		/// <returns>The <see cref="IDictionary{FA, ICollection{FA}}"/> containing the duplicates</returns>
		public IDictionary<FA, ICollection<FA>> FillDuplicatesGroupedByState(IDictionary<FA, ICollection<FA>> result)
		{
			return FillDuplicatesGroupedByState(FillClosure(), result);
		}
		/// <summary>
		/// Fills a <see cref="IDictionary{FA, ICollection{FA}}"/> with all duplicates in the closure, grouped by each duplicate state
		/// </summary>
		/// <param name="closure">The closure of all states</param>
		/// <param name="result">The <see cref="IDictionary{FA, ICollection{FA}}"/> to fill, or null to create a new one</param>
		/// <returns>The <see cref="IDictionary{FA, ICollection{FA}}"/> containing the duplicates</returns>
		public static IDictionary<FA, ICollection<FA>> FillDuplicatesGroupedByState(IEnumerable<FA> closure, IDictionary<FA, ICollection<FA>> result)
		{
			if (null == closure)
				throw new ArgumentNullException(nameof(closure));
			if (null == result)
				result = new Dictionary<FA, ICollection<FA>>();
			IList<FA> cl = closure as IList<FA> ?? new List<FA>(closure);
			int c = cl.Count;
			for (int i = 0; i < c; i++)
			{
				var s = cl[i];
				for (int j = i + 1; j < c; j++)
				{
					var cmp = cl[j];
					if (s.IsDuplicate(cmp))
					{
						ICollection<FA> col = new List<FA>();
						if (!result.ContainsKey(s))
							result.Add(s, col);
						else
							col = result[s];
						if (!col.Contains(cmp))
							col.Add(cmp);
					}
				}
			}
			return result;
		}
		/// <summary>
		/// Finds all of the loop points in the specified machine
		/// </summary>
		/// <param name="closure">The set of all states that represent the machine</param>
		/// <param name="result">Null, or a list to fill with <see cref="KeyValuePair{FA,FA}"/> entries</param>
		/// <returns>The list of <see cref="KeyValuePair{FA,FA}"/> entries where Key is the start of the loop, and Value is the end of the loop. Keys may be duplicated.</returns>
		public static IList<KeyValuePair<FA, FA>> FillLoops(IEnumerable<FA> closure, IList<KeyValuePair<FA, FA>> result = null)
		{
			if (null == result)
				result = new List<KeyValuePair<FA, FA>>();
			var cl = closure.AsList();
			var i = 0;
			foreach (var ffa in cl)
			{
				foreach (var rfa in FA.FillReferences(cl, ffa))
				{
					var ri = cl.IndexOf(rfa);
					if (!(ri < i || !ffa.IsLoop))
						result.Add(new KeyValuePair<FA, FA>(ffa, rfa));
				}
				++i;
			}
			return result;
		}
		/// <summary>
		/// Removes duplicate states from this machine
		/// </summary>
		public void TrimDuplicates()
		{
			TrimDuplicates(FillClosure());
		}
		/// <summary>
		/// Removes duplicate states from this machine
		/// </summary>
		/// <param name="closure">The set of all states</param>
		public static void TrimDuplicates(IEnumerable<FA> closure)
		{
			IList<FA> lclosure = new List<FA>(closure);
			var dups = new Dictionary<FA, ICollection<FA>>();
			int oc = 0;
			int c = -1;
			while (c < oc)
			{
				c = lclosure.Count;
				FillDuplicatesGroupedByState(lclosure, dups);
				if (0 < dups.Count)
				{
					foreach (KeyValuePair<FA, ICollection<FA>> de in dups)
					{
						var replacement = de.Key;
						var targets = de.Value;
						for (int i = 0; i < c; ++i)
						{
							var s = lclosure[i];

							var repls = new List<KeyValuePair<FA, FA>>();
							var td = (IDictionary<FA, ICollection<char>>)s.Transitions;
							foreach (var trns in td)
								if (targets.Contains(trns.Key))
									repls.Add(new KeyValuePair<FA, FA>(trns.Key, replacement));
							foreach (var repl in repls)
							{
								var inps = td[repl.Key];
								td.Remove(repl.Key);
								td.Add(repl.Value, inps);
							}

							int lc = s.EpsilonTransitions.Count;
							for (int j = 0; j < lc; ++j)
								if (targets.Contains(s.EpsilonTransitions[j]))
									s.EpsilonTransitions[j] = de.Key;
						}
					}
					dups.Clear();
				}
				else
					break;
				oc = c;
				var f = lclosure[0];
				//lclosure.Clear();
				lclosure = f.FillClosure();
				c = lclosure.Count;
			}
		}

		static IEnumerable<char> _ExpandRange(KeyValuePair<char, char> range)
		{
			if (range.Value < range.Key)
				for (int i = range.Value; i >= range.Key; --i)
					yield return (char)i;
			else
				for (int i = range.Key; i <= range.Value; ++i)
					yield return (char)i;
		}
		static IEnumerable<char> _ExpandRanges(IEnumerable<KeyValuePair<char, char>> ranges)
		{
			foreach (var range in ranges)
				foreach (char ch in _ExpandRange(range))
					yield return ch;
		}
		static IEnumerable<KeyValuePair<char, char>> _GetRanges(IEnumerable<char> sortedString)
		{
			char first = '\0';
			char last = '\0';
			using (IEnumerator<char> e = sortedString.GetEnumerator())
			{
				bool moved = e.MoveNext();
				while (moved)
				{
					first = last = e.Current;
					while ((moved = e.MoveNext()) && (e.Current == last || e.Current == last + 1))
					{
						last = e.Current;
					}
					yield return new KeyValuePair<char, char>(first, last);

				}
			}
		}
		static IEnumerable<KeyValuePair<char, char>> _NotRanges(IEnumerable<KeyValuePair<char, char>> ranges)
		{
			// expects ranges to be normalized
			var last = char.MaxValue;
			using (var e = ranges.GetEnumerator())
			{
				if (!e.MoveNext())
				{
					yield return new KeyValuePair<char, char>(char.MinValue, char.MaxValue);
					yield break;
				}
				if (e.Current.Key > char.MinValue)
				{
					yield return new KeyValuePair<char, char>(char.MinValue, unchecked((char)(e.Current.Key - 1)));
					last = e.Current.Value;
					if (char.MaxValue == last)
						yield break;
				}
				while (e.MoveNext())
				{
					if (char.MaxValue == last)
						yield break;
					if (unchecked((char)(last + 1)) < e.Current.Key)
					{
						yield return new KeyValuePair<char, char>(unchecked((char)(last + 1)), unchecked((char)(e.Current.Key - 1)));
					}
					last = e.Current.Value;
				}
				if (char.MaxValue > last)
				{
					yield return new KeyValuePair<char, char>(unchecked((char)(last + 1)), char.MaxValue);
					// last = char.MaxValue;
				}
			}
		}
		static void _NormalizeRanges(List<KeyValuePair<char, char>> ranges)
		{
			for (int i = 0; i < ranges.Count; ++i)
				if (ranges[i].Key > ranges[i].Value)
					ranges[i] = new KeyValuePair<char, char>(ranges[i].Value, ranges[i].Key);
			ranges.Sort(delegate (KeyValuePair<char, char> left, KeyValuePair<char, char> right)
			{
				return left.Key.CompareTo(right.Key);
			});
			var or = default(KeyValuePair<char, char>);
			for (int i = 1; i < ranges.Count; ++i)
			{
				if (ranges[i - 1].Value >= ranges[i].Key)
				{
					var nr = new KeyValuePair<char, char>(ranges[i - 1].Key, ranges[i].Value);
					ranges[i - 1] = or = nr;
					ranges.RemoveAt(i);
					--i; // compensated for by ++i in for loop
				}
			}
		}
		static char _ReadRangeChar(IEnumerator<char> e)
		{
			char ch;
			if ('\\' != e.Current || !e.MoveNext())
			{
				return e.Current;
			}
			ch = e.Current;
			switch (ch)
			{
				case 't':
					ch = '\t';
					break;
				case 'n':
					ch = '\n';
					break;
				case 'r':
					ch = '\r';
					break;
				case '0':
					ch = '\0';
					break;
				case 'v':
					ch = '\v';
					break;
				case 'f':
					ch = '\f';
					break;
				case 'b':
					ch = '\b';
					break;
				case 'x':
					byte x = _FromHexChar(ch);
					if (!e.MoveNext())
					{
						ch = unchecked((char)x);
						return ch;
					}
					x *= 0x10;
					x += _FromHexChar(e.Current);
					ch = unchecked((char)x);
					break;
				case 'u':
					ushort u = _FromHexChar(ch);
					if (!e.MoveNext())
					{
						ch = unchecked((char)u);
						return ch;
					}
					u *= 0x10;
					u += _FromHexChar(e.Current);
					if (!e.MoveNext())
					{
						ch = unchecked((char)u);
						return ch;
					}
					u *= 0x10;
					u += _FromHexChar(e.Current);
					if (!e.MoveNext())
					{
						ch = unchecked((char)u);
						return ch;
					}
					u *= 0x10;
					u += _FromHexChar(e.Current);
					ch = unchecked((char)u);
					break;
				default: // return itself
					break;
			}
			return ch;
		}
		static byte _FromHexChar(char hex)
		{
			if (':' > hex && '/' < hex)
				return (byte)(hex - '0');
			if ('G' > hex && '@' < hex)
				return (byte)(hex - '7'); // 'A'-10
			if ('g' > hex && '`' < hex)
				return (byte)(hex - 'W'); // 'a'-10
			throw new ArgumentException("The value was not hex.", "hex");
		}
		static IEnumerable<KeyValuePair<char, char>> _ParseRanges(IEnumerable<char> charRanges, bool normalize)
		{
			if (!normalize)
				return _ParseRanges(charRanges);
			else
			{
				var result = new List<KeyValuePair<char, char>>(_ParseRanges(charRanges));
				_NormalizeRanges(result);
				return result;
			}
		}
		static IEnumerable<KeyValuePair<char, char>> _ParseRanges(IEnumerable<char> charRanges)
		{
			using (var e = charRanges.GetEnumerator())
			{
				var skipRead = false;

				while (skipRead || e.MoveNext())
				{
					skipRead = false;
					char first = _ReadRangeChar(e);
					if (e.MoveNext())
					{
						if ('-' == e.Current)
						{
							if (e.MoveNext())
								yield return new KeyValuePair<char, char>(first, _ReadRangeChar(e));
							else
							{
								yield return new KeyValuePair<char, char>('-', '-');
							}
						}
						else
						{
							yield return new KeyValuePair<char, char>(first, first);
							skipRead = true;
							continue;

						}
					}
					else
					{
						yield return new KeyValuePair<char, char>(first, first);
						yield break;
					}
				}
			}
			yield break;
		}

		static void _Literal(FA first, FA final, IEnumerable<char> @string)
		{
			var current = first;
			var l = new List<char>(@string);
			int i;
			for (i = 0; i < l.Count - 1; ++i)
			{
				var fa = new FA();
				current.Transitions.Add(l[i], fa);
				current = fa;
			}
			current.Transitions.Add(l[i], final);

		}
		/// <summary>
		/// Creates a new generalized NFA (Thompson construction) matching the specified literal
		/// </summary>
		/// <param name="accepting">The symbol reported on accept</param>
		/// <param name="string">The string to match</param>
		/// <returns>A new machine that matches the <paramref name="string"/></returns>
		public static FA Literal(object accepting, params char[] @string) => Literal(@string, accepting);
		/// <summary>
		/// Creates a new generalized NFA (Thompson construction) matching the specified literal
		/// </summary>
		/// <param name="string">The string to match</param>
		/// <param name="accepting">The symbol reported on accept</param>
		/// <returns>A new machine that matches the <paramref name="string"/></returns>
		public static FA Literal(IEnumerable<char> @string, object accepting = null)
		{
			var result = new FA();
			if (null == accepting) accepting = result;
			if (@string.IsNullOrEmpty())
			{
				result.AcceptingSymbol = accepting;
				return result;
			}
			var final = new FA();
			final.AcceptingSymbol = accepting;
			_Literal(result, final, @string);
			return result;
		}
		static void _Set(FA first, FA final, IEnumerable<char> inputs)
		{
			foreach (var input in inputs)
				first.Transitions.Add(input, final);
		}
		/// <summary>
		/// Creates a new generalized NFA (Thompson construction) matching the specified set
		/// </summary>
		/// <param name="charRanges">The character ranges to match</param>
		/// <param name="accepting">The symbol reported on accept</param>
		/// <returns>A new machine that matches the <paramref name="charRanges"/></returns>
		public static FA Set(IEnumerable<KeyValuePair<char, char>> charRanges, object accepting = null)
		{
			return FA.Set(_ExpandRanges(charRanges), accepting);
		}
		public static FA Set(object accepting, params char[] inputs) => Set(inputs, accepting);
		/// <summary>
		/// Creates a new generalized NFA (Thompson construction) matching the specified set
		/// </summary>
		/// <param name="inputs">The input characters to match</param>
		/// <param name="accepting">The symbol reported on accept</param>
		/// <returns>A new machine that matches the <paramref name="inputs"/></returns>
		public static FA Set(IEnumerable<char> inputs, object accepting = null)
		{
			var result = new FA();
			if (null == accepting) accepting = result;
			if (inputs.IsNullOrEmpty())
			{
				result.AcceptingSymbol = accepting;
				return result;
			}
			var final = new FA();
			final.AcceptingSymbol = accepting;
			_Set(result, final, inputs);
			return result;
		}
		/// <summary>
		/// Creates a new generalized NFA (Thompson construction) matching the specified union
		/// </summary>
		/// <param name="accepting">The symbol reported on accept</param>
		/// <param name="exprs">The expressions to match</param>
		/// <returns>A new machine that matches the <paramref name="exprs"/></returns>
		public static FA Or(object accepting, params FA[] exprs) => Or(exprs, accepting);
		/// <summary>
		/// Creates a new generalized NFA (Thompson construction) matching the specified union
		/// </summary>
		/// <param name="exprs">The expressions to match</param>
		/// <param name="accepting">The symbol reported on accept</param>
		/// <returns>A new machine that matches the <paramref name="exprs"/></returns>
		public static FA Or(IEnumerable<FA> exprs, object accepting = null)
		{
			var result = new FA();
			if (null == accepting) accepting = result;
			if (exprs.IsNullOrEmpty())
			{
				result.AcceptingSymbol = accepting;
				return result;
			}
			var final = new FA();
			final.AcceptingSymbol = accepting;
			foreach (var fa in exprs)
			{
				if (null != fa)
				{
					foreach (var afa in fa.Accepting)
					{
						afa.AcceptingSymbol = null;
						afa.EpsilonTransitions.Add(final);
					}
					result.EpsilonTransitions.Add(fa);
				}
				else if (!result.EpsilonTransitions.Contains(final))
					result.EpsilonTransitions.Add(final);

			}
			return result;
		}
		/// <summary>
		/// Creates a new generalized NFA (Thompson construction) matching the specified sequence
		/// </summary>
		/// <param name="accepting">The symbol reported on accept</param>
		/// <param name="exprs">The expressions to match</param>
		/// <returns>A new machine that matches the <paramref name="exprs"/></returns>
		public static FA Concat(object accepting, params FA[] exprs) => Concat(exprs, accepting);
		/// <summary>
		/// Creates a new generalized NFA (Thompson construction) matching the specified sequence
		/// </summary>
		/// <param name="exprs">The expressions to match</param>
		/// <param name="accepting">The symbol reported on accept</param>
		/// <returns>A new machine that matches the <paramref name="exprs"/></returns>
		public static FA Concat(IEnumerable<FA> exprs, object accepting = null)
		{
			var result = new FA();
			if (null == accepting) accepting = result;
			if (exprs.IsNullOrEmpty())
			{
				result.AcceptingSymbol = accepting;
				return result;
			}
			FA left = null, right = null;
			foreach (var fa in exprs)
			{
				if (null == left)
				{
					left = fa;
					result = left;
					continue;
				}
				else right = fa;
				foreach (var afa in left.Accepting)
				{
					afa.AcceptingSymbol = null;
					afa.EpsilonTransitions.Add(right);
				}
				left = right;
			}
			foreach (var afa in left.Accepting)
			{
				afa.AcceptingSymbol = accepting;
			}
			return result;
		}
		/// <summary>
		/// Makes the specified machine optional
		/// </summary>
		/// <param name="fa">The machine</param>
		/// <param name="accepting">The symbol to report on accept</param>
		/// <returns>The modified machine</returns>
		public static FA Optional(FA fa, object accepting = null)
		{
			var final = new FA();
			final.AcceptingSymbol = accepting ?? fa;
			foreach (var afa in fa.Accepting)
			{
				afa.AcceptingSymbol = null;
				afa.EpsilonTransitions.Add(final);
			}
			fa.EpsilonTransitions.Add(final);
			return fa;
		}
		public static FA Repeat(FA fa, object accepting)
		{
			return Repeat(fa, 1, 0, accepting);
		}
		/// <summary>
		/// Makes the specified machine repeat one or more times
		/// </summary>
		/// <param name="fa">The machine</param>
		/// <param name="accepting">The symbol to report on accept</param>
		/// <returns>The modified machine</returns>
		public static FA Repeat(FA fa, int min = 0, int max = 0, object accepting = null)
		{
			accepting = accepting ?? fa;
			if (min != 0 && max != 0 && min > max)
				throw new ArgumentOutOfRangeException(nameof(max));
			switch (min)
			{
				case 0:
					switch (max)
					{
						case 0:
							var result = new FA();
							var final = new FA();
							final.AcceptingSymbol = accepting ?? result;
							final.EpsilonTransitions.Add(result);
							foreach (var afa in fa.Accepting)
							{
								afa.AcceptingSymbol = null;
								afa.EpsilonTransitions.Add(final);
							}
							result.EpsilonTransitions.Add(fa);
							result.EpsilonTransitions.Add(final);
							return result;
						case 1:
							return Optional(fa, accepting);
						default:
							var l = new List<FA>();
							fa = Optional(fa);
							l.Add(fa);
							for (int i = 1; i < max; ++i)
							{
								l.Add(fa.Clone());
							}
							return Concat(l, accepting);
					}
				case 1:
					switch (max)
					{
						case 0:
							var result = new FA();
							var final = new FA();
							final.AcceptingSymbol = accepting;
							final.EpsilonTransitions.Add(result);
							foreach (var afa in fa.Accepting)
							{
								afa.AcceptingSymbol = null;
								afa.EpsilonTransitions.Add(final);
							}
							result.EpsilonTransitions.Add(fa);
							return result;
						case 1:
							return fa;
						default:
							return Concat(accepting, fa, Repeat(fa.Clone(), 0, max - 1));
					}
				default:
					switch (max)
					{
						case 0:
							return Concat(accepting, Repeat(fa, min, min, accepting), Repeat(fa, 0, 0, accepting));
						case 1:
							throw new ArgumentOutOfRangeException(nameof(max));
						default:
							if (min == max)
							{
								var l = new List<FA>();
								l.Add(fa);
								for (int i = 1; i < min; ++i)
									l.Add(fa.Clone());
								return Concat(l, accepting);
							}
							return Concat(accepting, Repeat(fa.Clone(), min, min, accepting), Repeat(FA.Optional(fa.Clone()), max - min, max - min, accepting));


					}
			}
			// should never get here
			throw new NotImplementedException();
		}
		/// <summary>
		/// Makes the specified machine repeat zero or more times
		/// </summary>
		/// <param name="fa">The machine</param>
		/// <param name="accepting">The symbol to report on accept</param>
		/// <returns>The modified machine</returns>
		public static FA Kleene(FA fa, object accepting = null)
		{
			var result = new FA();
			var final = new FA();
			final.AcceptingSymbol = accepting ?? result;
			final.EpsilonTransitions.Add(result);
			foreach (var afa in fa.Accepting)
			{
				afa.AcceptingSymbol = null;
				afa.EpsilonTransitions.Add(final);
			}
			fa.EpsilonTransitions.Add(final);
			result.EpsilonTransitions.Add(fa);
			return result;
		}
		/// <summary>
		/// Creates a Lexer from the specified expressions
		/// </summary>
		/// <param name="exprs">The expressions to compose the lexer of. Each expression should produce a token</param>
		/// <param name="createDefaultAcceptConstants">True if the default FA instances that are used for accepting states (when unspecified) should be replaced instead with integer constants. States with explicitly set accepting symbols shouldn't be affected by this unless they use FA states as accepting symbols.</param>
		/// <returns>An FA that will lex the specified expressions</returns>
		public static FA Lexer(IEnumerable<FA> exprs, bool createDefaultAcceptConstants = true)
		{
			var result = new FA();
			int i = 0;
			foreach (var expr in exprs)
			{
				if (createDefaultAcceptConstants)
				{
					foreach (var fa in expr.FillAccepting())
					{
						FA f = null;
						IList<object> l = fa.AcceptingSymbol as IList<object>;
						if (null != l)
						{
							int c = l.Count;
							for (int j = 0; j < c; ++j)
							{
								f = l[j] as FA;
								if (null != f)
								{
									// use shorts if we can
									if (short.MaxValue >= i && short.MinValue <= i)
										l[j] = unchecked((short)i);
									else if (ushort.MaxValue >= i && ushort.MinValue <= i)
										l[i] = unchecked((ushort)i);
									else
										l[j] = i;
								}
							}
						}
						else
						{
							f = fa.AcceptingSymbol as FA;
							if (null != f)
							{
								if (short.MaxValue >= i && short.MinValue <= i)
									fa.AcceptingSymbol = unchecked((short)i);
								else if (ushort.MaxValue >= i && ushort.MinValue <= i)
									fa.AcceptingSymbol = unchecked((ushort)i);
								else
									fa.AcceptingSymbol = i;
							}
						}
					}
					++i;
				}
				result.EpsilonTransitions.Add(expr);

			}
			return result;
		}
		/// <summary>
		/// Creates a Lexer from the specified expressions
		/// </summary>
		/// <param name="exprs">The expressions to compose the lexer of. Each expression should produce a token</param>
		/// <returns>An FA that will lex the specified expressions</returns>
		public static FA Lexer(params FA[] exprs) => Lexer(exprs, true);
		/// <summary>
		/// The options used for rendering dot graphs
		/// </summary>
		/// <remarks>Currently, this is little more than a placeholder for future options like coloring of states</remarks>
#if GRIMOIRELIB
		public
#else
		internal
#endif
		sealed class DotGraphOptions
		{
			/// <summary>
			/// The resolution, in dots-per-inch to render at
			/// </summary>
			public int Dpi = 300;
			/// <summary>
			/// The prefix used for state labels
			/// </summary>
			public string StatePrefix = "q";

			/// <summary>
			/// If non-null, specifies a debug render using the specified input string.
			/// </summary>
			/// <remarks>The debug render is useful for tracking the transitions in a state machine</remarks>
			public IEnumerable<char> DebugString { get; set; }
			/// <summary>
			/// If non-null, specifies the source NFA from which this DFA was derived - used for debug view
			/// </summary>
			public FA DebugSourceNfa = null;
		}

		static void _AppendRangesTo(StringBuilder builder, IEnumerable<KeyValuePair<char, char>> ranges)
		{
			foreach (KeyValuePair<char, char> range in ranges)
				_AppendRangeTo(builder, range);
		}
		public static void _AppendRangeTo(StringBuilder builder, KeyValuePair<char, char> range)
		{
			_AppendRangeCharTo(builder, range.Key);
			if (0 == range.Value.CompareTo(range.Key)) return;
			if (range.Value == range.Key + 1) // spit out 1 length ranges as two chars
			{
				_AppendRangeCharTo(builder, range.Value);
				return;
			}
			builder.Append('-');
			_AppendRangeCharTo(builder, range.Value);
		}
		static void _AppendRangeCharTo(StringBuilder builder, char rangeChar)
		{
			switch (rangeChar)
			{
				case '-':
				case '\\':
					builder.Append('\\');
					builder.Append(rangeChar);
					return;
				case '\t':
					builder.Append("\\t");
					return;
				case '\n':
					builder.Append("\\n");
					return;
				case '\r':
					builder.Append("\\r");
					return;
				case '\0':
					builder.Append("\\0");
					return;
				case '\f':
					builder.Append("\\f");
					return;
				case '\v':
					builder.Append("\\v");
					return;
				case '\b':
					builder.Append("\\b");
					return;
				default:
					if (!char.IsLetterOrDigit(rangeChar) && !char.IsSeparator(rangeChar) && !char.IsPunctuation(rangeChar) && !char.IsSymbol(rangeChar))
					{

						builder.Append("\\u");
						builder.Append(unchecked((ushort)rangeChar).ToString("x4"));

					}
					else
						builder.Append(rangeChar);
					break;
			}
		}
		static bool _TryForwardNeutral(FA fa, out FA result)
		{
			result = fa ?? throw new ArgumentNullException(nameof(fa));
			if (!fa.IsNeutral)
				return false;
			result = fa.EpsilonTransitions[0];
			return fa != result; // false if circular
		}
		static FA _ForwardNeutrals(FA fa)
		{
			var result = fa;
			while (_TryForwardNeutral(result, out result))
				;
			return result;
		}
		/// <summary>
		/// Trims the neutral states from this machine
		/// </summary>
		public void TrimNeutrals() { TrimNeutrals(FillClosure()); }
		/// <summary>
		/// Trims the neutral states from the specified closure
		/// </summary>
		/// <param name="closure">The set of all states</param>
		public static void TrimNeutrals(IEnumerable<FA> closure)
		{
			var cl = new List<FA>(closure);
			foreach (var s in cl)
			{
				var repls = new List<KeyValuePair<FA, FA>>();
				var td = (IDictionary<FA, ICollection<char>>)s.Transitions;
				foreach (var trns in td)
				{
					var fa = trns.Key;
					var fa2 = _ForwardNeutrals(fa);
					if (fa != fa2)
						repls.Add(new KeyValuePair<FA, FA>(fa, fa2));
				}
				foreach (var repl in repls)
				{
					var inps = td[repl.Key];
					td.Remove(repl.Key);
					td.Add(repl.Value, inps);
				}
				var ec = s.EpsilonTransitions.Count;
				for (int j = 0; j < ec; ++j)
					s.EpsilonTransitions[j] = _ForwardNeutrals(s.EpsilonTransitions[j]);
			}
		}
		/// <summary>
		/// Creates a new machine that is the deterministic equivelent of this machine
		/// </summary>
		/// <returns>A DFA</returns>
		public FA ToDfa()
		{
			return ToDfa(Closure);
		}
		/// <summary>
		/// Creates a new machine that is the deterministic equivelent of this machine
		/// </summary>
		/// <returns>A DFA</returns>
		public static FA ToDfa(IEnumerable<FA> closure)
		{

			// The DFA states are keyed by the set of NFA states they represent.
			var dfaMap = new Dictionary<List<FA>, FA>(_SetComparer.Default);

			var unmarked = new HashSet<FA>();

			// compute the epsilon closure of the initial state in the NFA
			var states = new List<FA>();

			closure.First().FillEpsilonClosure(states);

			// create a new state to represent the current set of states. If one 
			// of those states is accepting, set this whole state to be accepting.
			FA dfa = new FA();
			var al = new List<object>();
			foreach (var fa in states)
				if (fa.IsAccepting)
					if (!al.Contains(fa.AcceptingSymbol))
						al.Add(fa.AcceptingSymbol);
			int ac = al.Count;
			if (1 == ac)
				dfa.AcceptingSymbol = al[0];
			else if (1 < ac)
				dfa.AcceptingSymbol = al; // hang on to the multiple symbols
			var tl = new List<object>();
			foreach (var fa in states)
				if (null != fa.Tag)
					if (!tl.Contains(fa.Tag))
						tl.Add(fa.Tag);
			var tcl = tl.Count;
			if (1 == tcl)
				dfa.Tag = tl[0];
			else if (1 < tcl)
				dfa.Tag = tl; // hang on to the multiple tags


			FA result = dfa; // store the initial state for later, so we can return it.

			// add it to the dfa map
			dfaMap.Add(states, dfa);

			// add it to the unmarked states, signalling that we still have work to do.
			unmarked.Add(dfa);
			bool done = false;
			while (!done)
			{
				done = true;
				HashSet<List<FA>> mapKeys = new HashSet<List<FA>>(dfaMap.Keys, _SetComparer.Default);
				foreach (List<FA> mapKey in mapKeys)
				{
					dfa = dfaMap[mapKey];
					if (unmarked.Contains(dfa))
					{
						// when we get here, mapKey represents the epsilon closure of our 
						// current dfa state, which is indicated by kvp.Value

						// build the transition list for the new state by combining the transitions
						// from each of the old states

						// retrieve every possible input for these states
						HashSet<char> inputs = new HashSet<char>();
						foreach (FA state in mapKey)
						{
							var dtrns = (IDictionary<FA, ICollection<char>>)state.Transitions;
							foreach (var trns in dtrns)
							{
								foreach (var inp in trns.Value)
									inputs.Add(inp);
							}

						}

						foreach (var input in inputs)
						{
							var acc = new List<object>();
							var tags = new List<object>();
							List<FA> ns = new List<FA>();
							foreach (var state in mapKey)
							{
								FA dst = null;
								if (state.Transitions.TryGetValue(input, out dst))
								{
									foreach (var d in dst.FillEpsilonClosure())
									{
										if (d.IsAccepting)
											if (!acc.Contains(d.AcceptingSymbol))
												acc.Add(d.AcceptingSymbol);
										if (null != d.Tag)
										{
											if (!tags.Contains(d.Tag))
												tags.Add(d.Tag);
										}
										if (!ns.Contains(d))
											ns.Add(d);
									}
								}
							}

							FA ndfa;
							if (!dfaMap.TryGetValue(ns, out ndfa))
							{
								ndfa = new FA(ns);
								ac = acc.Count;
								if (1 == ac)
									ndfa.AcceptingSymbol = acc[0];
								else if (1 < ac)
									ndfa.AcceptingSymbol = acc;
								else
									ndfa.AcceptingSymbol = null;

								var tc = tags.Count;
								if (1 == tc)
									ndfa.Tag = tags[0];
								else if (1 < tc)
									ndfa.Tag = tags;
								else
									ndfa.Tag = null;

								dfaMap.Add(ns, ndfa);
								unmarked.Add(ndfa);
								done = false;
							}
							dfa.Transitions.Add(input, ndfa);
						}
						unmarked.Remove(dfa);
					}
				}
			}
			return result;
		}
		/// <summary>
		/// Creates a table representing the state transitions for a DFA of this machine
		/// </summary>
		/// <returns>A complex tuple representing the DFA table</returns>
		public (object Accept, (KeyValuePair<char, char>[] Ranges, int Destination)[])[] ToDfaTable()
			=> ToDfaTable<object>();
		/// <summary>
		/// Creates a table representing the state transitions for a DFA of this machine
		/// </summary>
		/// <typeparam name="TAccept">The accepting symbol type</typeparam>
		/// <returns>A complex tuple representing the DFA table</returns>
		public (TAccept Accept, (KeyValuePair<char, char>[] Ranges, int Destination)[])[] ToDfaTable<TAccept>()
			=> ToDfaTable<TAccept>(FillClosure());

		/// <summary>
		/// Creates a table representing the state transitions for a DFA of this machine
		/// </summary>
		/// <param name="closure">The set of all states</param>
		/// <returns>A complex tuple representing the DFA table</returns>
		public static (object Accept, (KeyValuePair<char, char>[] Ranges, int Destination)[])[] ToDfaTable(IEnumerable<FA> closure)
			=> ToDfaTable<object>(closure);
		/// <summary>
		/// Creates a table representing the state transitions for a DFA of this machine
		/// </summary>
		/// <typeparam name="TAccept">The accepting symbol type</typeparam>
		/// <param name="closure">The set of all states</param>
		/// <returns>A complex tuple representing the DFA table</returns>
		public static (TAccept Accept, (KeyValuePair<char, char>[] Ranges, int Destination)[])[] ToDfaTable<TAccept>(IEnumerable<FA> closure)
		{
			if (!_IsDfa(closure))
			{
				var dfa = closure.First().ToDfa();
				dfa.TrimDuplicates();
				closure = dfa.FillClosure();
			}
			var cl = closure.AsList();
			var result = new List<(TAccept Accept, (KeyValuePair<char, char>[] Ranges, int Destination)[])>();
			var i = 0;
			foreach (var ffa in cl)
			{
				var igrpt = new List<(KeyValuePair<char, char>[] Ranges, int Destination)>();
				foreach (var igrp in ffa.FillInputTransitionRangesGroupedByState())
				{
					var ranges = new List<KeyValuePair<char, char>>(igrp.Value);
					var dstId = closure.IndexOf(igrp.Key);
					igrpt.Add((ranges.ToArray(), dstId));
				}
				object asym = _GetAcceptingSymbol(new FA[] { ffa }, true);
				if(null==asym)
				{
					asym = default(TAccept);
					if (typeof(TAccept) == typeof(int) || typeof(TAccept) == typeof(short) || typeof(TAccept) == typeof(long) || typeof(TAccept) == typeof(sbyte))
						asym = -1;
				}
				result.Add(((TAccept)asym, igrpt.ToArray()));
				++i;
			}
			return result.ToArray();
		}
		/// <summary>
		/// Writes a Graphviz dot specification to the specified <see cref="TextWriter"/>
		/// </summary>
		/// <param name="writer">The writer</param>
		/// <param name="options">A <see cref="DotGraphOptions"/> instance with any options, or null to use the defaults</param>
		public void WriteDotTo(TextWriter writer, DotGraphOptions options = null)
		{
			WriteDotTo(FillClosure(), writer, options);
		}
		/// <summary>
		/// Writes a Graphviz dot specification of the specified closure to the specified <see cref="TextWriter"/>
		/// </summary>
		/// <param name="closure">The closure of all states</param>
		/// <param name="writer">The writer</param>
		/// <param name="options">A <see cref="DotGraphOptions"/> instance with any options, or null to use the defaults</param>
		public static void WriteDotTo(IEnumerable<FA> closure, TextWriter writer, DotGraphOptions options = null)
		{
			if (null == options) options = new DotGraphOptions();
			string spfx = null == options.StatePrefix ? "q" : options.StatePrefix;
			writer.WriteLine("digraph FA {");
			writer.WriteLine("rankdir=LR");
			writer.WriteLine("node [shape=circle]");
			var finals = new List<FA>();
			var neutrals = new List<FA>();
			var accepting = FillAccepting(closure, null);
			foreach (var ffa in closure)
			{
				if (ffa.IsFinal && !ffa.IsAccepting)
					finals.Add(ffa);
			}
			IList<FA> fromStates = null;
			IList<FA> toStates = null;
			char tchar = default(char);
			toStates = closure.First().FillEpsilonClosure();
			if (null != options.DebugString)
			{
				foreach (char ch in options.DebugString)
				{
					fromStates = FillEpsilonClosure(toStates, null);
					tchar = ch;
					toStates = FA.Move(fromStates, ch);
					if (0 == toStates.Count)
						break;

				}
			}
			if (null != toStates)
			{
				toStates = FillEpsilonClosure(toStates, null);
			}
			int i = 0;
			foreach (var ffa in closure)
			{
				if (!finals.Contains(ffa))
				{
					if (ffa.IsAccepting)
						accepting.Add(ffa);
					else if (ffa.IsNeutral)
						neutrals.Add(ffa);
				}
				var rngGrps = ffa.FillInputTransitionRangesGroupedByState(null);
				foreach (var rngGrp in rngGrps)
				{
					var di = closure.IndexOf(rngGrp.Key);
					writer.Write(spfx);
					writer.Write(i);
					writer.Write("->");
					writer.Write(spfx);
					writer.Write(di.ToString());
					writer.Write(" [label=\"");
					var sb = new StringBuilder();
					_AppendRangesTo(sb, rngGrp.Value);
					if (sb.Length != 1 || " " == sb.ToString())
					{
						writer.Write('[');
						writer.Write(_EscapeLabel(sb.ToString()));
						writer.Write(']');
					}
					else
						writer.Write(_EscapeLabel(sb.ToString()));
					writer.WriteLine("\"]");
				}
				// do epsilons
				foreach (var fffa in ffa.EpsilonTransitions)
				{
					writer.Write(spfx);
					writer.Write(i);
					writer.Write("->");
					writer.Write(spfx);
					writer.Write(closure.IndexOf(fffa));
					writer.WriteLine(" [style=dashed,color=gray]");
				}


				++i;
			}
			string delim = "";
			i = 0;
			foreach (var ffa in closure)
			{
				writer.Write(spfx);
				writer.Write(i);
				writer.Write(" [");
				if (null != options.DebugString)
				{
					if (null != toStates && toStates.Contains(ffa))
					{
						writer.Write("color=green,");
					}
					if (null != fromStates && fromStates.Contains(ffa) && (null == toStates || !toStates.Contains(ffa)))
					{
						writer.Write("color=darkgreen,");
					}
				}
				writer.Write("label=<");
				writer.Write("<TABLE BORDER=\"0\"><TR><TD>");
				writer.Write(spfx);
				writer.Write("<SUB>");
				writer.Write(i);
				writer.Write("</SUB></TD></TR>");

				if (null != options.DebugString && null != options.DebugSourceNfa && null != ffa.Tag)
				{
					var tags = ffa.Tag as IList<object>;
					if (null != tags || ffa.Tag is FA)
					{
						writer.Write("<TR><TD>{");
						if (null == tags)
						{
							writer.Write(" q<SUB>");
							writer.Write(options.DebugSourceNfa.FillClosure().IndexOf((FA)ffa.Tag).ToString());
							writer.Write("</SUB>");
						}
						else
						{
							delim = "";
							foreach (var tag in tags)
							{
								writer.Write(delim);
								if (tag is FA)
								{
									writer.Write(delim);
									writer.Write(" q<SUB>");
									writer.Write(options.DebugSourceNfa.FillClosure().IndexOf((FA)tag).ToString());
									writer.Write("</SUB>");
									// putting a comma here is what we'd like
									// but it breaks dot no matter how its encoded
									delim = @" ";
								}
							}
						}
						writer.Write(" }</TD></TR>");
					}

				}
				if (null != ffa.AcceptingSymbol)
				{
					var al = ffa.AcceptingSymbol as IList<object>;
					if (null == al)
					{
						writer.Write("<TR><TD>");
						writer.Write(Convert.ToString(ffa.AcceptingSymbol).Replace("\"", "&quot;"));
						writer.Write("</TD></TR>");
					}
					else
					{

						foreach (var o in al)
						{
							writer.Write("<TR><TD>");
							writer.Write(Convert.ToString(o).Replace("\"", "&quot;"));
							writer.Write("</TD></TR>");
						}


					}
				}
				writer.Write("</TABLE>");
				writer.Write(">");
				bool isfinal = false;
				if (accepting.Contains(ffa) || (isfinal = finals.Contains(ffa)))
					writer.Write(",shape=doublecircle");
				if (isfinal || neutrals.Contains(ffa))
				{
					if ((null == fromStates || !fromStates.Contains(ffa)) &&
						(null == toStates || !toStates.Contains(ffa)))
					{
						writer.Write(",color=gray");
					}
				}
				writer.WriteLine("]");
				++i;
			}
			if (0 < accepting.Count)
			{
				foreach (var ntfa in accepting)
				{
					writer.Write(delim);
					writer.Write(spfx);
					writer.Write(closure.IndexOf(ntfa));
					delim = ",";
				}
				writer.WriteLine(" [shape=doublecircle]");
			}
			delim = "";
			if (0 < neutrals.Count)
			{

				foreach (var ntfa in neutrals)
				{
					if ((null == fromStates || !fromStates.Contains(ntfa)) &&
						(null == toStates || !toStates.Contains(ntfa))
						)
					{
						writer.Write(delim);
						writer.Write(spfx);
						writer.Write(closure.IndexOf(ntfa));
						delim = ",";
					}
				}
				writer.WriteLine(" [color=gray]");

				if (null != fromStates)
				{
					foreach (var ntfa in neutrals)
					{
						if (fromStates.Contains(ntfa) && (null == toStates || !toStates.Contains(ntfa)))
						{
							writer.Write(delim);
							writer.Write(spfx);
							writer.Write(closure.IndexOf(ntfa));
							delim = ",";
						}
					}

					writer.WriteLine(" [color=darkgreen]");
				}
				if (null != toStates)
				{
					foreach (var ntfa in neutrals)
					{
						if (toStates.Contains(ntfa))
						{
							writer.Write(delim);
							writer.Write(spfx);
							writer.Write(closure.IndexOf(ntfa));
							delim = ",";
						}
					}
					writer.WriteLine(" [color=green]");
				}


			}
			delim = "";
			if (0 < finals.Count)
			{
				foreach (var ntfa in finals)
				{
					writer.Write(delim);
					writer.Write(spfx);
					writer.Write(closure.IndexOf(ntfa));
					delim = ",";
				}
				writer.WriteLine(" [shape=doublecircle,color=gray]");
			}

			writer.WriteLine("}");

		}
		static string _EscapeLabel(string label)
		{
			if (string.IsNullOrEmpty(label)) return label;

			string result = label.Replace("\\", @"\\");
			result = result.Replace("\"", "\\\"");
			result = result.Replace("\n", "\\n");
			result = result.Replace("\r", "\\r");
			result = result.Replace("\0", "\\0");
			result = result.Replace("\v", "\\v");
			result = result.Replace("\t", "\\t");
			result = result.Replace("\f", "\\f");
			return result;
		}
		/// <summary>
		/// Renders Graphviz output for this machine to the specified file
		/// </summary>
		/// <param name="filename">The output filename. The format to render is indicated by the file extension.</param>
		/// <param name="options">A <see cref="DotGraphOptions"/> instance with any options, or null to use the defaults</param>
		public void RenderToFile(string filename, DotGraphOptions options = null)
		{
			if (null == options)
				options = new DotGraphOptions();
			string args = "-T";
			string ext = Path.GetExtension(filename);
			if (0 == string.Compare(".png", ext, StringComparison.InvariantCultureIgnoreCase))
				args += "png";
			else if (0 == string.Compare(".jpg", ext, StringComparison.InvariantCultureIgnoreCase))
				args += "jpg";
			else if (0 == string.Compare(".bmp", ext, StringComparison.InvariantCultureIgnoreCase))
				args += "bmp";
			else if (0 == string.Compare(".svg", ext, StringComparison.InvariantCultureIgnoreCase))
				args += "svg";
			if (0 < options.Dpi)
				args += " -Gdpi=" + options.Dpi.ToString();

			args += " -o\"" + filename + "\"";

			var psi = new ProcessStartInfo("dot", args)
			{
				CreateNoWindow = true,
				UseShellExecute = false,
				RedirectStandardInput = true
			};
			using (var proc = Process.Start(psi))
			{
				WriteDotTo(proc.StandardInput, options);
				proc.StandardInput.Close();
				proc.WaitForExit();
			}

		}
		/// <summary>
		/// Renders Graphviz output for this machine to a stream
		/// </summary>
		/// <param name="format">The output format. The format to render can be any supported dot output format. See dot command line documation for details.</param>
		/// <param name="options">A <see cref="DotGraphOptions"/> instance with any options, or null to use the defaults</param>
		/// <returns>A stream containing the output. The caller is expected to close the stream when finished.</returns>
		public Stream RenderToStream(string format, bool copy = false, DotGraphOptions options = null)
		{
			if (null == options)
				options = new DotGraphOptions();
			string args = "-T";
			args += string.Concat(" ", format);
			if (0 < options.Dpi)
				args += " -Gdpi=" + options.Dpi.ToString();

			var psi = new ProcessStartInfo("dot", args)
			{
				CreateNoWindow = true,
				UseShellExecute = false,
				RedirectStandardInput = true,
				RedirectStandardOutput = true
			};
			using (var proc = Process.Start(psi))
			{
				WriteDotTo(proc.StandardInput, options);
				proc.StandardInput.Close();
				if (!copy)
					return proc.StandardOutput.BaseStream;
				else
				{
					MemoryStream stm = new MemoryStream();
					proc.StandardOutput.BaseStream.CopyTo(stm);
					proc.StandardOutput.BaseStream.Close();
					proc.WaitForExit();
					return stm;
				}
			}
		}
		public static int _ParseEscape(ParseContext pc)
		{
			if ('\\' != pc.Current)
				return -1;
			if (-1 == pc.Advance())
				return -1;
			switch (pc.Current)
			{
				case 't':
					pc.Advance();
					return '\t';
				case 'n':
					pc.Advance();
					return '\n';
				case 'r':
					pc.Advance();
					return '\r';
				case 'x':
					if (-1 == pc.Advance())
						return 'x';
					byte b = _FromHexChar((char)pc.Current);
					b <<= 4;
					if (-1 == pc.Advance())
						return unchecked((char)b);
					b |= _FromHexChar((char)pc.Current);
					return unchecked((char)b);
				case 'u':
					if (-1 == pc.Advance())
						return 'u';
					ushort u = _FromHexChar((char)pc.Current);
					u <<= 4;
					if (-1 == pc.Advance())
						return unchecked((char)u);
					u |= _FromHexChar((char)pc.Current);
					u <<= 4;
					if (-1 == pc.Advance())
						return unchecked((char)u);
					u |= _FromHexChar((char)pc.Current);
					u <<= 4;
					if (-1 == pc.Advance())
						return unchecked((char)u);
					u |= _FromHexChar((char)pc.Current);
					return unchecked((char)u);
				default:
					int i = pc.Current;
					pc.Advance();
					return (char)i;
			}
		}
		/// <summary>
		/// Parses a regular expresion from the specified string
		/// </summary>
		/// <param name="string">The string</param>
		/// <param name="accepting">The symbol reported when accepting the specified expression</param>
		/// <returns>A new machine that matches the regular expression</returns>
		public static FA Parse(IEnumerable<char> @string, object accepting = null) => Parse(ParseContext.Create(@string), accepting);
		/// <summary>
		/// Parses a regular expresion from the specified <see cref="TextReader"/>
		/// </summary>
		/// <param name="reader">The text reader</param>
		/// <param name="accepting">The symbol reported when accepting the specified expression</param>
		/// <returns>A new machine that matches the regular expression</returns>
		public static FA Parse(TextReader reader, object accepting = null) => Parse(ParseContext.Create(reader), accepting);
		/// <summary>
		/// Parses a regular expresion from the specified <see cref="ParseContext"/>
		/// </summary>
		/// <param name="pc">The parse context</param>
		/// <param name="accepting">The symbol reported when accepting the specified expression</param>
		/// <returns>A new machine that matches the regular expression</returns>
		public static FA Parse(ParseContext pc, object accepting = null)
		{
			var result = new FA();
			if (null == accepting) accepting = result;
			result.AcceptingSymbol = accepting;
			FA f, next;
			int ch;
			pc.EnsureStarted();
			var current = result;
			while (true)
			{
				switch (pc.Current)
				{
					case -1:
						result.TrimNeutrals();
						return result;
					case '.':
						pc.Advance();
						f = current.Accepting.First();

						current = FA.Set(new KeyValuePair<char, char>[] {new KeyValuePair<char, char>(char.MinValue,char.MaxValue) }, accepting);
						switch (pc.Current)
						{
							case '*':
								current = FA.Kleene(current, accepting);
								pc.Advance();
								break;
							case '+':
								current = FA.Repeat(current, accepting);
								pc.Advance();
								break;
							case '?':
								current = FA.Optional(current, accepting);
								pc.Advance();
								break;

						}
						f.AcceptingSymbol = null;
						f.EpsilonTransitions.Add(current);
						break;
					case '\\':
						if (-1 != (ch = _ParseEscape(pc)))
						{
							next = null;
							switch (pc.Current)
							{
								case '*':
									next = new FA();
									next.Transitions.Add((char)ch, new FA(accepting));
									next = FA.Kleene(next, accepting);
									pc.Advance();
									break;
								case '+':
									next = new FA();
									next.Transitions.Add((char)ch, new FA(accepting));
									next = FA.Repeat(next, accepting);
									pc.Advance();
									break;
								case '?':
									next = new FA();
									next.Transitions.Add((char)ch, new FA(accepting));
									next = FA.Optional(next, accepting);
									pc.Advance();
									break;
								default:
									current = current.Finals.First();
									current.AcceptingSymbol = null;
									current.Transitions.Add((char)ch, new FA(accepting));
									break;
							}
							if (null != next)
							{
								current = current.Finals.First();
								current.AcceptingSymbol = null;
								current.EpsilonTransitions.Add(next);
								current = next;
							}
						}
						else
						{
							pc.Expecting(); // throw an error
							return null; // doesn't execute
						}
						break;
					case ')':
						result.TrimNeutrals();
						return result;
					case '(':
						pc.Advance();
						pc.Expecting();
						f = current.Accepting.First();
						current = Parse(pc, accepting);
						pc.Expecting(')');
						pc.Advance();
						switch (pc.Current)
						{
							case '*':
								current = FA.Kleene(current, accepting);
								pc.Advance();
								break;
							case '+':
								current = FA.Repeat(current, accepting);
								pc.Advance();
								break;
							case '?':
								current = FA.Optional(current, accepting);
								pc.Advance();
								break;
						}
						f = FA.Concat(accepting, f, current);
						break;
					case '|':
						if (-1 != pc.Advance())
						{
							current = Parse(pc, accepting);
							result = FA.Or(accepting, result, current);
						}
						else
						{
							current = current.Finals.First();
							result = FA.Optional(result, accepting);
						}
						break;
					case '[':
						pc.ClearCapture();
						pc.Advance();
						pc.Expecting();
						bool not = false;
						if ('^' == pc.Current)
						{
							not = true;
							pc.Advance();
							pc.Expecting();
						}
						pc.TryReadUntil(']', '\\', false);
						pc.Expecting(']');
						pc.Advance();

						var r = (!not && "." == pc.Capture) ?
							new KeyValuePair<char, char>[] { new KeyValuePair<char, char>(char.MinValue, char.MaxValue) } :
							_ParseRanges(pc.Capture, true);
						if (not)
							r = _NotRanges(r);
						f = current.Accepting.First();
						current = FA.Set(r, accepting);
						switch (pc.Current)
						{
							case '*':
								current = FA.Kleene(current, accepting);
								pc.Advance();
								break;
							case '+':
								current = FA.Repeat(current, accepting);
								pc.Advance();
								break;
							case '?':
								current = FA.Optional(current, accepting);
								pc.Advance();
								break;

						}
						f.AcceptingSymbol = null;
						f.EpsilonTransitions.Add(current);
						break;
					default:
						ch = pc.Current;
						pc.Advance();
						next = null;
						switch (pc.Current)
						{
							case '*':
								next = new FA();
								next.Transitions.Add((char)ch, new FA(accepting));
								next = FA.Kleene(next, accepting);
								pc.Advance();
								break;
							case '+':
								next = new FA();
								next.Transitions.Add((char)ch, new FA(accepting));
								next = FA.Repeat(next, accepting);
								pc.Advance();
								break;
							case '?':
								next = new FA();
								next.Transitions.Add((char)ch, new FA(accepting));
								next = FA.Optional(next, accepting);
								pc.Advance();
								break;
							default:
								current = current.Finals.First();
								current.AcceptingSymbol = null;
								current.Transitions.Add((char)ch, new FA(accepting));
								break;
						}
						if (null != next)
						{
							current = current.Accepting.First();
							current.AcceptingSymbol = null;
							current.EpsilonTransitions.Add(next);
							current = next;
						}
						break;
				}
			}
		}
		
		// a dictionary optimized for FA transitions
		class _TrnsDic : IDictionary<char, FA>, IDictionary<FA, ICollection<char>>
		{
			IDictionary<FA, ICollection<char>> _inner = new Dictionary<FA, ICollection<char>>();

			public FA this[char key] {
				get {
					foreach (var trns in _inner)
					{
						if (trns.Value.Contains(key))
							return trns.Key;
					}
					throw new KeyNotFoundException();
				}
				set {
					Remove(key);
					ICollection<char> hs;
					if (_inner.TryGetValue(value, out hs))
					{
						hs.Add(key);
					}
					else
					{
						hs = new HashSet<char>();
						hs.Add(key);
						_inner.Add(value, hs);
					}
				}
			}

			public ICollection<char> Keys {
				get {
					return _EnumKeys().AsCollection();
				}

			}
			IEnumerable<char> _EnumKeys()
			{
				foreach (var trns in _inner)
					foreach (var key in trns.Value)
						yield return key;
			}
			public ICollection<FA> Values { get { return _EnumValues().AsCollection(); } }
			IEnumerable<FA> _EnumValues()
			{
				foreach (var trns in _inner)
					foreach (var key in trns.Value)
						yield return trns.Key;
			}
			public int Count {
				get {
					var result = 0;
					foreach (var trns in _inner)
						result += trns.Value.Count;
					return result;
				}
			}

			ICollection<FA> IDictionary<FA, ICollection<char>>.Keys { get { return _inner.Keys; } }
			ICollection<ICollection<char>> IDictionary<FA, ICollection<char>>.Values { get { return _inner.Values; } }
			int ICollection<KeyValuePair<FA, ICollection<char>>>.Count { get { return _inner.Count; } }
			public bool IsReadOnly { get { return _inner.IsReadOnly; } }

			ICollection<char> IDictionary<FA, ICollection<char>>.this[FA key] { get { return _inner[key]; } set { _inner[key] = value; } }

			public void Add(char key, FA value)
			{
				if (ContainsKey(key))
					throw new InvalidOperationException("The key is already present in the dictionary.");
				ICollection<char> hs;
				if (_inner.TryGetValue(value, out hs))
				{
					hs.Add(key);
				}
				else
				{
					hs = new HashSet<char>();
					hs.Add(key);
					_inner.Add(value, hs);
				}
			}

			public void Add(KeyValuePair<char, FA> item)
			{
				Add(item.Key, item.Value);
			}

			public void Clear()
			{
				_inner.Clear();
			}

			public bool Contains(KeyValuePair<char, FA> item)
			{
				ICollection<char> hs;
				return _inner.TryGetValue(item.Value, out hs) && hs.Contains(item.Key);
			}

			public bool ContainsKey(char key)
			{
				foreach (var trns in _inner)
				{
					if (trns.Value.Contains(key))
						return true;
				}
				return false;
			}

			public void CopyTo(KeyValuePair<char, FA>[] array, int arrayIndex)
			{
				((IEnumerable<KeyValuePair<char, FA>>)this).CopyTo(array, arrayIndex);
			}

			public IEnumerator<KeyValuePair<char, FA>> GetEnumerator()
			{
				foreach (var trns in _inner)
					foreach (var ch in trns.Value)
						yield return new KeyValuePair<char, FA>(ch, trns.Key);
			}

			public bool Remove(char key)
			{
				FA rem = null;
				foreach (var trns in _inner)
				{
					if (trns.Value.Contains(key))
					{
						trns.Value.Remove(key);
						if (0 == trns.Value.Count)
						{
							rem = trns.Key;
							break;
						}
						return true;
					}
				}
				if (null != rem)
				{
					_inner.Remove(rem);
					return true;
				}
				return false;
			}

			public bool Remove(KeyValuePair<char, FA> item)
			{
				ICollection<char> hs;
				if (_inner.TryGetValue(item.Value, out hs))
				{
					if (hs.Contains(item.Key))
					{
						if (1 == hs.Count)
							_inner.Remove(item.Value);
						else
							hs.Remove(item.Key);
						return true;
					}
				}
				return false;
			}

			public bool TryGetValue(char key, out FA value)
			{
				foreach (var trns in _inner)
				{
					if (trns.Value.Contains(key))
					{
						value = trns.Key;
						return true;
					}
				}
				value = null;
				return false;
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}

			void IDictionary<FA, ICollection<char>>.Add(FA key, ICollection<char> value)
			{
				_inner.Add(key, value);
			}

			bool IDictionary<FA, ICollection<char>>.ContainsKey(FA key)
			{
				return _inner.ContainsKey(key);
			}

			bool IDictionary<FA, ICollection<char>>.Remove(FA key)
			{
				return _inner.Remove(key);
			}

			bool IDictionary<FA, ICollection<char>>.TryGetValue(FA key, out ICollection<char> value)
			{
				return _inner.TryGetValue(key, out value);
			}

			void ICollection<KeyValuePair<FA, ICollection<char>>>.Add(KeyValuePair<FA, ICollection<char>> item)
			{
				_inner.Add(item);
			}
			bool ICollection<KeyValuePair<FA, ICollection<char>>>.Contains(KeyValuePair<FA, ICollection<char>> item)
			{
				return _inner.Contains(item);
			}

			void ICollection<KeyValuePair<FA, ICollection<char>>>.CopyTo(KeyValuePair<FA, ICollection<char>>[] array, int arrayIndex)
			{
				_inner.CopyTo(array, arrayIndex);
			}

			bool ICollection<KeyValuePair<FA, ICollection<char>>>.Remove(KeyValuePair<FA, ICollection<char>> item)
			{
				return _inner.Remove(item);
			}

			IEnumerator<KeyValuePair<FA, ICollection<char>>> IEnumerable<KeyValuePair<FA, ICollection<char>>>.GetEnumerator()
			{
				return _inner.GetEnumerator();
			}
		}
		// compares several types of state collections or dictionaries used by FA
		sealed class _SetComparer : IEqualityComparer<IList<FA>>, IEqualityComparer<ICollection<FA>>, IEqualityComparer<IDictionary<char, FA>>
		{
			// ordered comparison
			public bool Equals(IList<FA> lhs, IList<FA> rhs)
			{
				return lhs.Equals<FA>(rhs);
			}
			// unordered comparison
			public bool Equals(ICollection<FA> lhs, ICollection<FA> rhs)
			{
				return lhs.Equals<FA>(rhs);
			}
			public bool Equals(IDictionary<char, FA> lhs, IDictionary<char, FA> rhs)
			{
				return lhs.Equals<KeyValuePair<char, FA>>(rhs);
			}
			public bool Equals(IDictionary<FA, ICollection<char>> lhs, IDictionary<FA, ICollection<char>> rhs)
			{
				if (lhs.Count != rhs.Count) return false;
				if (ReferenceEquals(lhs, rhs))
					return true;
				else if (ReferenceEquals(null, lhs) || ReferenceEquals(null, rhs))
					return false;
				using (var xe = lhs.GetEnumerator())
				using (var ye = rhs.GetEnumerator())
					while (xe.MoveNext() && ye.MoveNext())
					{
						if (xe.Current.Key != ye.Current.Key)
							return false;
						if (!CollectionUtility.Equals(xe.Current.Value, ye.Current.Value))
							return false;
					}
				return true;
			}
			public int GetHashCode(IList<FA> lhs)
			{
				return lhs.GetHashCode<FA>();
			}
			public int GetHashCode(ICollection<FA> lhs)
			{
				return lhs.GetHashCode<FA>();
			}
			public int GetHashCode(IDictionary<char, FA> lhs)
			{
				return lhs.GetHashCode<KeyValuePair<char, FA>>();
			}
			public static readonly _SetComparer Default = new _SetComparer();
		}
	}
}
