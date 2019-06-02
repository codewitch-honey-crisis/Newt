using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Grimoire
{
	static class ParserUtility
	{
		public static Type ResolveType(string typeName)
		{
			switch (typeName)
			{
				case "string":
					return typeof(string);
				case "int":
					return typeof(int);
				case "uint":
					return typeof(uint);
				case "short":
					return typeof(short);
				case "ushort":
					return typeof(ushort);
				case "long":
					return typeof(long);
				case "ulong":
					return typeof(ulong);
				case "decimal":
					return typeof(decimal);
				case "float":
					return typeof(float);
				case "double":
					return typeof(double);
				case "char":
					return typeof(char);
				case "byte":
					return typeof(byte);
				case "sbyte":
					return typeof(sbyte);
				case "bool":
					return typeof(bool);
				case "object": // useless but consistent
					return typeof(object);
			}
			var t = Type.GetType(typeName, false, true);
			if (null != t) return t;
			t = Type.GetTypeFromProgID(typeName);
			if (null != t) return t;
			throw new InvalidOperationException("The type \"" + typeName + "\" was not found or could not be loaded.");
		}
		public static object GetParsedValue(Type type, string value)
		{
			if (null != type)
			{
				TypeConverter tc = TypeDescriptor.GetConverter(type);
				if (null != tc && tc.CanConvertFrom(typeof(string)))
					return tc.ConvertFromInvariantString(value);
			}
			return value;
		}
		public static (int Accept, string Value, (int First, int Last)[] ExpectingRanges, int[] ExpectingSymbols) Lex2(
			(int Accept, ((char First, char Last)[] Ranges, int Destination)[] Transitions, int[] PossibleAccepts)[] dfaTable,
			int errorSymbol,
			ParseContext pc,
			StringBuilder sb = null)
		{
			if (null == sb)
				sb = new StringBuilder();
			else
				sb.Clear();
			pc.EnsureStarted();
			int state = 0;
			var dfaEntry = dfaTable[state];
			var acc = dfaEntry.Accept;
			if (-1 == pc.Current)
			{
				if (0 > acc)
					return DoRecoveryDfat(
							dfaTable,
							dfaEntry,
							errorSymbol,
							pc,
							sb,
							dfaEntry.Transitions);


				return (dfaEntry.Accept, sb.ToString(), null, null);
			}
			while (true)
			{
				var ns = GetDfatTransition(dfaEntry.Transitions, (char)pc.Current);
				if (-1 == ns)
				{
					if (0 > dfaEntry.Accept)
						return DoRecoveryDfat(
							dfaTable,
							dfaEntry,
							errorSymbol,
							pc,
							sb,
							dfaEntry.Transitions);


					return (dfaEntry.Accept, sb.ToString(), null, null);
				}
				state = ns;
				dfaEntry = dfaTable[state];
				if (-1 != pc.Current)
					sb.Append((char)pc.Current);
				if (-1 == pc.Advance())
				{
					if (0 > dfaEntry.Accept)
						return DoRecoveryDfat(
							dfaTable,
							dfaEntry,
							errorSymbol,
							pc,
							sb,
							dfaEntry.Transitions);

					return (dfaEntry.Accept, sb.ToString(), null, null);
				}

			}
		}
		public static IEnumerable<char> ExpandRange(KeyValuePair<char, char> range)
		{
			if (range.Value < range.Key)
				for (int i = range.Value; i >= range.Key; --i)
					yield return (char)i;
			else
				for (int i = range.Key; i <= range.Value; ++i)
					yield return (char)i;
		}
		public static IEnumerable<char> ExpandRanges(IEnumerable<KeyValuePair<char, char>> ranges)
		{
			foreach (var range in ranges)
				foreach (char ch in ExpandRange(range))
					yield return ch;
		}
		static (int Accept, string Value, (int First, int Last)[] ExpectingRanges, int[] ExpectingSymbols) DoRecoveryDfat(
			(int Accept, ((char First, char Last)[] Ranges, int Destination)[] Transitions, int[] PossibleAccepts)[] dfaTable,
			(int Accept, ((char First, char Last)[] Ranges, int Destination)[] Transitions, int[] PossibleAccepts) dfaEntry,
			int errorSymbol,
			ParseContext pc, StringBuilder sb, ((char First, char Last)[] Ranges, int Destination)[] trns)
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
					var dt = GetDfatTransition(dfaTable[0].Transitions, (char)pc.Current);
					if (0 > dt)
						break;

				}
			}
			return (errorSymbol, sb.ToString(), ranges.ToArray(), dfaEntry.PossibleAccepts);
		}
		static int GetDfatTransition(((char First, char Last)[] Ranges, int Destination)[] trns, char ch)
		{
			for (int i = 0; i < trns.Length; ++i)
			{
				var trn = trns[i];
				for (int j = 0; j < trn.Ranges.Length; ++j)
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
	}
}
