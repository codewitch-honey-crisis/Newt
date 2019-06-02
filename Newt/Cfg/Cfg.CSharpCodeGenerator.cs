using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Grimoire
{
	using CS = CSharpUtility;
	partial class Cfg
	{
		static bool _IsValidIdentifier(string identifier)
		{
			if (string.IsNullOrEmpty(identifier)) return false;
			if (!char.IsLetter(identifier[0]) && '_' != identifier[0])
				return false;

			for (var i = 1; i < identifier.Length; i++)
			{
				char ch = identifier[i];
				if (!char.IsLetterOrDigit(ch) && '_' != ch && '-' != ch)
					return false;
			}
			return true;

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
		public void WriteCSharpSymbolConstantsTo(TextWriter writer, string modifiers)
		{
			if (null == modifiers)
				modifiers = "";
			var names = new HashSet<string>();
			writer.WriteLine(string.Concat("\t", modifiers, " const int EOS=", GetSymbolId("#EOS").ToString(), ";"));
			names.Add("EOS");
			writer.WriteLine(string.Concat("\t",modifiers," const int ERROR=",GetSymbolId("#ERROR").ToString(),";"));
			names.Add("ERROR");
			foreach (var sym in _EnumSymbols())
			{
				IDictionary<string, object> d;
				object o;
				if (AttributeSets.TryGetValue(sym, out d) && d.TryGetValue("hidden", out o) && o is bool && (bool)o)
					continue;
				if (AttributeSets.TryGetValue(sym, out d) && d.TryGetValue("collapse", out o) && o is bool && (bool)o)
					continue;
				var sid = Convert.ToString(sym);
				if (_IsValidIdentifier(sid))
				{
					string s;
					if (!string.IsNullOrEmpty(modifiers))
						s = string.Concat("\t", modifiers, " const int ");
					else
						s = string.Concat("\t", modifiers, "const int ");

					var id = GetSymbolId(sym);
					s = string.Concat(s, CS.CreateEscapedIdentifier(sid.Replace('-', '_')), " = ");
					s = _GetUniqueName(names, s);
					names.Add(s);
					s = string.Concat(s, id.ToString(), ";");
					writer.WriteLine(s);

				}
			}

		}
		public void WriteCSharpLL1ParseTableCreateExpressionTo(TextWriter writer, IDictionary<int, IDictionary<int, (int Left, int[] Right)>> parseTable = null)
		{
			if (null == parseTable)
				parseTable = ToLL1ParseTable();
			var ntc = _EnumNonTerminals().Count();
			
			writer.WriteLine("new (int Left, int[] Right)[][] {");
			for(var i =0;i<ntc;++i)
			{
				writer.Write("\t");
				if (0 != i) writer.Write(",");
				IDictionary<int, (int Left, int[] Right)> d;
				if (parseTable.TryGetValue(i, out d))
				{
					writer.WriteLine("new (int Left, int[] Right)[] {");
					var j = 0;
					foreach (var t in _EnumTerminals())
					{
						if (Equals(t, "#ERROR"))
							continue;
						writer.Write("\t\t");
						if (0 != j) writer.Write(",");
						(int Left, int[] Right) ir;
						if (d.TryGetValue(j + ntc, out ir))
						{
							writer.Write("(");
							CS.WriteCSharpLiteralTo(writer, ir.Left);
							writer.Write(", ");
							CS.WriteCSharpLiteralTo(writer, ir.Right);
							writer.WriteLine(")");
						}
						else
							writer.WriteLine("(-1,null)");
						++j;
					}
					writer.WriteLine("\t\t}");
				}
				else
					writer.WriteLine("null");
				
			}
			writer.Write("}");
		}
		public void WriteCSharpTableDrivenLL1ParserClassTo(TextWriter writer, string name, string modifiers = null, FA lexer = null, IDictionary<int, IDictionary<int, (int Left, int[] Right)>> parseTable = null)
		{
			if (string.IsNullOrEmpty(name))
				name = "Parser";
			if (!string.IsNullOrEmpty(modifiers))
				writer.Write(string.Concat(modifiers, " "));
			writer.Write(string.Concat("partial class ", name, " : Grimoire.TableDrivenLL1Parser"));
			writer.WriteLine(" {");
			writer.WriteLine(string.Concat("\tpublic ", name, "(Grimoire.ParseContext parseContext=null) : base(_ParseTable,_StartingConfiguration,_LexTable,_Symbols,_SubstitutionsAndHiddenTerminals,_BlockEnds,_CollapsedNonTerminals,_Types,parseContext) { }"));
			WriteCSharpSymbolConstantsTo(writer, "public");
			writer.Write("\tstatic readonly string[] _Symbols = {");
			var delim = "";
			foreach (var sym in _EnumSymbols())
			{
				writer.Write(delim);
				CS.WriteCSharpLiteralTo(writer, sym);
				delim = ", ";
			}
			writer.WriteLine(" };");
			writer.Write("\tstatic readonly (int Left, int[] Right)[][] _ParseTable = ");
			WriteCSharpLL1ParseTableCreateExpressionTo(writer, parseTable);
			writer.WriteLine(";");
			writer.WriteLine();

			writer.Write("\tstatic readonly int[] _SubstitutionsAndHiddenTerminals = new int[] { ");
			delim = "";
			foreach (var sym in _EnumSymbols())
			{
				IDictionary<string, object> attrs;
				if (AttributeSets.TryGetValue(sym, out attrs))
				{
					if ((bool)attrs.TryGetValue("hidden", false))
						writer.Write("-2");
					else
					{
						object sub = attrs.TryGetValue("substitute", null);
						if (null != sub)
							writer.Write(GetSymbolId(sub as string));
						else
							writer.Write(GetSymbolId(sym));
					}
				}
				else
					writer.Write(GetSymbolId(sym));
				writer.Write(", ");
			}
			writer.WriteLine("-1 };");

			writer.Write("\tstatic readonly (int SymbolId,bool IsNonTerminal,int NonTerminalCount) _StartingConfiguration = (");
			var ss = StartSymbol;
			var startId = GetSymbolId(ss);
			var isNonTerminal = IsNonTerminal(ss);
				
			writer.Write(string.Concat(startId, ", "));
			CS.WriteCSharpLiteralTo(writer, isNonTerminal);
			writer.Write(", ");
			CS.WriteCSharpLiteralTo(writer, _EnumNonTerminals().Count());
			writer.WriteLine(");");

			writer.WriteLine("\tstatic readonly string[] _BlockEnds = new string[] { ");
			delim = "\t";
			foreach (var sym in _EnumSymbols())
			{
				writer.Write(delim);
				IDictionary<string, object> attrs;
				if (AttributeSets.TryGetValue(sym, out attrs))
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

			writer.WriteLine("\tstatic readonly System.Type[] _Types = new System.Type[] { ");
			delim = "\t";
			foreach (var sym in _EnumSymbols())
			{
				writer.Write(delim);
				IDictionary<string, object> attrs;
				if (AttributeSets.TryGetValue(sym, out attrs))
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
				if (AttributeSets.TryGetValue(sym, out attrs))
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
