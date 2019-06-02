// FA w/ Error recovery
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Grimoire
{
	
	partial class FA
	{
		public static void WriteCSharpDfaTable2CreationExpressionTo<TAccept>(TextWriter writer, (TAccept Accept, ((char First, char Last)[] Ranges, int Destination)[] Transitions, TAccept[] PossibleAccepts)[] dfaTable)
		{
			var tuple = string.Concat("(", typeof(TAccept).FullName, " Accept, ((char First, char Last)[] Ranges, int Destination)[] Transitions, ", typeof(TAccept).FullName, "[] PossibleAccepts)");
			writer.WriteLine(string.Concat("new ",tuple,"[] {"));
			for(var i = 0;i<dfaTable.Length;i++)
			{
				var dfaEntry = dfaTable[i];
				writer.Write("\t");
				if (0 != i) writer.Write(",");
				writer.WriteLine(string.Concat("(",dfaEntry.Accept,", new ((char First, char Last)[] Ranges, int Destination)[] {"));
				for(var j =0;j<dfaEntry.Transitions.Length;j++)
				{
					var trn = dfaEntry.Transitions[j];
					writer.Write("\t\t");
					if (0 != j) writer.Write(",");
					writer.WriteLine("(new (char First,char Last)[] {");
					for(var k = 0;k<trn.Ranges.Length;k++) {
						var rng = trn.Ranges[k];
						writer.Write("\t\t\t");
						if (0 != k) writer.Write(",");
						writer.Write("((char)");
						// spitting chars here breaks unicode so we use ints
						// WriteCSharpCharTo doesn't support unicode yet.
						CSharpUtility.WriteCSharpLiteralTo(writer, (int)rng.First);
						writer.Write(",(char)");
						CSharpUtility.WriteCSharpLiteralTo(writer, (int)rng.Last);
						writer.WriteLine(")");
					}
					writer.Write("}");
					writer.Write(string.Concat(",", trn.Destination));
					writer.Write(")");
				}
				writer.Write("}, new int[] ");
				writer.Write(CollectionUtility.ToString(dfaEntry.PossibleAccepts));
				writer.WriteLine(")");
			}
			writer.WriteLine("}");
		}
		public static (TAccept Accept, string Value, (int First, int Last)[] ExpectingRanges, TAccept[] ExpectingSymbols) Lex2<TAccept>(
			(TAccept Accept, ((char First, char Last)[] Ranges, int Destination)[] Transitions, TAccept[] PossibleAccepts)[] dfaTable,
			TAccept errorSymbol,
			ParseContext pc,
			StringBuilder sb = null)
		{
			if (null == sb)
				sb = new StringBuilder();
			else
				sb.Clear();
			pc.EnsureStarted();
			var state = 0;
			var dfaEntry = dfaTable[state];
			object acc = dfaEntry.Accept;
			if (-1 == pc.Current)
			{
				if (null == acc)
					return _DoRecoveryDfat<TAccept>(
							dfaTable,
							dfaEntry,
							errorSymbol,
							pc,
							sb,
							dfaEntry.Transitions);

				if (typeof(TAccept) == typeof(int) || typeof(TAccept) == typeof(short) || typeof(TAccept) == typeof(long) || typeof(TAccept) == typeof(sbyte))
				{
					if (-1L == Convert.ToInt64(dfaEntry.Accept))
					{
						return _DoRecoveryDfat(
						dfaTable,
						dfaEntry,
						errorSymbol,
						pc,
						sb,
						dfaEntry.Transitions);

					}
				}
				return (dfaEntry.Accept, sb.ToString(), null, null);
			}
			while (true)
			{
				var ns = _GetDfatTransition(dfaEntry.Transitions, (char)pc.Current);
				if (-1 == ns)
				{
					if (null == dfaEntry.Accept)
					{
						return _DoRecoveryDfat(
							dfaTable,
							dfaEntry,
							errorSymbol,
							pc,
							sb,
							dfaEntry.Transitions);
						
					}
					if (typeof(TAccept) == typeof(int) || typeof(TAccept) == typeof(short) || typeof(TAccept) == typeof(long) || typeof(TAccept) == typeof(sbyte))
					{
						if (-1L == Convert.ToInt64(dfaEntry.Accept))
						{
							return _DoRecoveryDfat(
							dfaTable,
							dfaEntry,
							errorSymbol,
							pc,
							sb,
							dfaEntry.Transitions);

						}
					}
					return (dfaEntry.Accept, sb.ToString(), null, null);
				}
				state = ns;
				dfaEntry = dfaTable[state];
				if (-1 != pc.Current)
					sb.Append((char)pc.Current);
				if (-1 == pc.Advance())
				{
					if (null == dfaEntry.Accept)
						return _DoRecoveryDfat(
							dfaTable,
							dfaEntry,
							errorSymbol,
							pc,
							sb,
							dfaEntry.Transitions);
					if (typeof(TAccept) == typeof(int) || typeof(TAccept) == typeof(short) || typeof(TAccept) == typeof(long) || typeof(TAccept) == typeof(sbyte))
					{
						
						if (-1L == Convert.ToInt64(dfaEntry.Accept))
							return _DoRecoveryDfat(
							dfaTable,
							dfaEntry,
							errorSymbol,
							pc,
							sb,
							dfaEntry.Transitions);
					}
					return (dfaEntry.Accept, sb.ToString(), null, null);
				}

			}
		}
		static (TAccept Accept, string Value, (int First, int Last)[] ExpectingRanges, TAccept[] ExpectingSymbols) _DoRecoveryDfat<TAccept>(
			(TAccept Accept, ((char First, char Last)[] Ranges, int Destination)[] Transitions, TAccept[] PossibleAccepts)[] dfaTable,
			(TAccept Accept, ((char First, char Last)[] Ranges, int Destination)[] Transitions, TAccept[] PossibleAccepts) dfaEntry,
			TAccept errorSymbol,
			ParseContext pc,StringBuilder sb,((char First, char Last)[] Ranges, int Destination)[] trns)
		{
			var ranges = new List<(int First, int Last)>();
			for (var i = 0; i < dfaEntry.Transitions.Length; i++)
			{
				var trn = dfaEntry.Transitions[i];

				for (var j = 0; j < trn.Ranges.Length; j++)
				{
					var range = trn.Ranges[j];
					ranges.Add((range.First, range.Last));
				}

			}
			while (true)
			{
				if (-1 == pc.Current)
					break;
				sb.Append((char)pc.Current);
				if (-1 != pc.Advance())
				{
					var dt = _GetDfatTransition(dfaTable[0].Transitions, (char)pc.Current);
					if (-1!=dt)
						break;
					
				}
			}
			return (errorSymbol, sb.ToString(), ranges.ToArray(), dfaEntry.PossibleAccepts);
		}
		static int _GetDfatTransition(((char First,char Last)[] Ranges, int Destination)[] trns, char ch)
		{
			for (var i = 0; i < trns.Length; ++i)
			{
				var trn = trns[i];
				for (var j = 0; j < trn.Ranges.Length; ++j)
				{
					var rg = trn.Ranges[j];
					if (ch >= rg.First && ch <= rg.Last)
					{
						return trn.Destination;
					}
				}
			}
			return -1; // no state
		}
		public (TAccept Accept, ((char First, char Last)[] Ranges, int Destination)[] Transitions, TAccept[] PossibleAccepts)[] ToDfaTable2<TAccept>()
		{
			var fa = this;
			if (!fa.IsDfa)
				fa = fa.ToDfa();
			var closure = fa.FillClosure();
			var cc = closure.Count;
			var result = new (TAccept Accept, ((char First, char Last)[] Ranges, int Destination)[] Transitions, TAccept[] PossibleAccepts)[cc];
			for (var i = 0;i<result.Length;i++)
			{
				var cfa = closure[i];
				var possibleAccepts = cfa.AcceptingSymbols.Cast<TAccept>().ToArray();
				var tc = ((IDictionary<FA,ICollection<char>>)cfa.Transitions).Count;
				var transitions = new ((char First, char Last)[] Ranges, int Destination)[tc];
				var j = 0;
				foreach (var trns in (IDictionary<FA, ICollection<char>>)cfa.Transitions)
				{
					var dranges = _GetRanges(trns.Value).ToArray();
					var ranges = new (char First, char Last)[dranges.Length];
					for(var k = 0;k<dranges.Length;k++)
					{
						var range = dranges[k];
						ranges[k] = (range.Key, range.Value);
					}
					transitions[j] = (ranges, closure.IndexOf(trns.Key));
					++j;
				}
				object acc;
				if (null == cfa.AcceptingSymbol && (typeof(TAccept) == typeof(int) || typeof(TAccept) == typeof(short) || typeof(TAccept) == typeof(long) || typeof(TAccept) == typeof(sbyte)))
					acc = (TAccept)Convert.ChangeType(-1, typeof(TAccept));
				else
					acc = cfa.AcceptingSymbol;
				result[i] = ((TAccept)Convert.ChangeType(acc,typeof(TAccept)), transitions, possibleAccepts);
			}
			return result;
		}
	}
}
