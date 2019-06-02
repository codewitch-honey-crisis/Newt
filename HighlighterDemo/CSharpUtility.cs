using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;

namespace Grimoire
{
	// TODO: Test unicode support - easiest to test using Regex codegen + "." regex
	static partial class CSharpUtility
	{
		public static void WriteCSharpLiteralTo(TextWriter writer, object val)
		{
			if (null == val)
			{
				writer.Write("null");
				return;
			}
			if (val is bool)
			{
				writer.Write(((bool)val) ? "true" : "false");
				return;
			}
			if (val is string)
			{
				WriteCSharpStringTo(writer, (string)val);
				return;
			}
			if (val is Array && 1 == ((Array)val).Rank && 0 == ((Array)val).GetLowerBound(0))
			{
				WriteCSharpArrayTo(writer, (Array)val);
				return;
			}
			if (val is char)
			{
				WriteCSharpCharTo(writer, (char)val);
				return;
			}
			if (val is short || val is ushort || val is int || val is uint || val is ulong || val is long || val is byte || val is sbyte || val is float || val is double || val is decimal)
			{
				writer.Write(val);
				return;
			}
			var conv = TypeDescriptor.GetConverter(val);
			if(null!=conv)
			{
				if(conv.CanConvertTo(typeof(InstanceDescriptor)))
				{
					var desc = conv.ConvertTo(val, typeof(InstanceDescriptor)) as InstanceDescriptor;
					if (!desc.IsComplete)
						throw new NotSupportedException(string.Format("The type \"{0}\" could not be serialized.", val.GetType().FullName));
					var ctor = desc.MemberInfo as ConstructorInfo;
					if (null != ctor)
					{
						writer.Write(string.Concat("new ", ctor.DeclaringType.FullName, "("));
						var delim = "";
						foreach (var arg in desc.Arguments)
						{
							writer.Write(delim);
							WriteCSharpLiteralTo(writer, arg);
							delim = ", ";
						}
						writer.Write(")");
					}
					else
						throw new NotSupportedException(string.Format("The instance descriptor for type \"{0}\" is not supported.", val.GetType().FullName));
				} else
					throw new NotSupportedException(string.Format("The type \"{0}\" could not be serialized.", val.GetType().FullName));
			} else
				throw new NotSupportedException(string.Format("The type \"{0}\" could not be serialized.", val.GetType().FullName));
		}
		public static void WriteCSharpStringTo(TextWriter writer, string str)
		{
			writer.Write("\"");
			for (int i = 0; i < str.Length; ++i)
				_WriteCSharpCharPartTo(writer, str[i]);
			writer.Write("\"");
		}
		public static void WriteCSharpCharTo(TextWriter writer, char ch)
		{
			writer.Write("\'");
			_WriteCSharpCharPartTo(writer, ch);
			writer.Write("\'");
		}
		public static void WriteCSharpArrayTo(TextWriter writer, Array arr)
		{
			if (1 == arr.Rank && 0 == arr.GetLowerBound(0))
			{
				writer.Write(string.Concat("new ", arr.GetType().GetElementType().FullName, "[] {"));
				var delim = " ";
				var i = 0;
				foreach (var elem in arr)
				{
					writer.Write(delim);
					WriteCSharpLiteralTo(writer, elem);
					if(50==i)
					{
						i = 0;
						writer.WriteLine();
						writer.Write("\t");
					}
					delim = ", ";
					++i;
				}
				writer.Write(" }");
				return;
			}
			throw new NotSupportedException("Only SZArrays can be serialized to code.");

		}
		static void _WriteCSharpCharPartTo(TextWriter writer, char ch)
		{
			switch (ch)
			{
				case '\'':
				case '\"':
				case '\\':
					writer.Write('\\');
					writer.Write(ch);
					return;
				case '\t':
					writer.Write("\\t");
					return;
				case '\n':
					writer.Write("\\n");
					return;
				case '\r':
					writer.Write("\\r");
					return;
				case '\0':
					writer.Write("\\0");
					return;
				case '\f':
					writer.Write("\\f");
					return;
				case '\v':
					writer.Write("\\v");
					return;
				case '\b':
					writer.Write("\\b");
					return;
				case '\a':
					writer.Write("\\a");
					return;
				case '\u2028':
				case '\u2029':
				case '\u0084':
				case '\u0085':
					writer.Write("\\u");
					writer.Write(unchecked((ushort)ch).ToString("x4"));
					break;
				default:
					if (char.IsControl(ch) || char.IsSurrogate(ch) || char.IsWhiteSpace(ch))
					{
						if (ch <= byte.MaxValue)
						{
							writer.Write("\\x");
							writer.Write(unchecked((byte)ch).ToString("x2"));
						}
						else
						{
							writer.Write("\\u");
							writer.Write(unchecked((ushort)ch).ToString("x4"));
						}
					}
					else
						writer.Write(ch);
					break;
			}
		}
		public static bool IsKeyword(string value)
		{
			return _FixedStringLookup(keywords, value);
		}
		public static string CreateEscapedIdentifier(string identifier)
		{
			if (IsKeyword(identifier) || _IsPrefixTwoUnderscore(identifier))
			{
				return "@" + identifier;
			}
			return identifier;
		}
		static bool _IsPrefixTwoUnderscore(string value)
		{
			if (value.Length < 3)
			{
				return false;
			}
			else
			{
				return ((value[0] == '_') && (value[1] == '_') && (value[2] != '_'));
			}
		}
		#region Lookup Tables
		// from microsoft's reference implementation of the c# code dom provider
		// This routine finds a hit within a single sorted array, with the assumption that the
		// value and all the strings are of the same length.
		private static bool _FixedStringLookupContains(string[] array, string value)
		{
			int min = 0;
			int max = array.Length;
			int pos = 0;
			char searchChar;
			while (pos < value.Length)
			{

				searchChar = value[pos];

				if ((max - min) <= 1)
				{
					// we are down to a single item, so we can stay on this row until the end.
					if (searchChar != array[min][pos])
					{
						return false;
					}
					pos++;
					continue;
				}

				// There are multiple items to search, use binary search to find one of the hits
				if (!_FindCharacter(array, searchChar, pos, ref min, ref max))
				{
					return false;
				}
				// and move to next char
				pos++;
			}
			return true;
		}

		// Do a binary search on the character array at the specific position and constrict the ranges appropriately.
		static bool _FindCharacter(string[] array, char value, int pos, ref int min, ref int max)
		{
			int index = min;
			while (min < max)
			{
				index = (min + max) / 2;
				char comp = array[index][pos];
				if (value == comp)
				{
					// We have a match. Now adjust to any adjacent matches
					int newMin = index;
					while (newMin > min && array[newMin - 1][pos] == value)
					{
						newMin--;
					}
					min = newMin;

					int newMax = index + 1;
					while (newMax < max && array[newMax][pos] == value)
					{
						newMax++;
					}
					max = newMax;
					return true;
				}
				if (value < comp)
				{
					max = index;
				}
				else
				{
					min = index + 1;
				}
			}
			return false;
		}
		internal static bool _FixedStringLookup(string[][] lookupTable, string value)
		{
			int length = value.Length;
			if (length <= 0 || length - 1 >= lookupTable.Length)
			{
				return false;
			}

			string[] subArray = lookupTable[length - 1];
			if (subArray == null)
			{
				return false;
			}
			return _FixedStringLookupContains(subArray, value);
		}

		static readonly string[][] keywords = new string[][] {
			null,           // 1 character
            new string[] {  // 2 characters
                "as",
				"do",
				"if",
				"in",
				"is",
			},
			new string[] {  // 3 characters
                "for",
				"int",
				"new",
				"out",
				"ref",
				"try",
			},
			new string[] {  // 4 characters
                "base",
				"bool",
				"byte",
				"case",
				"char",
				"else",
				"enum",
				"goto",
				"lock",
				"long",
				"null",
				"this",
				"true",
				"uint",
				"void",
			},
			new string[] {  // 5 characters
                "break",
				"catch",
				"class",
				"const",
				"event",
				"false",
				"fixed",
				"float",
				"sbyte",
				"short",
				"throw",
				"ulong",
				"using",
				"while",
			},
			new string[] {  // 6 characters
                "double",
				"extern",
				"object",
				"params",
				"public",
				"return",
				"sealed",
				"sizeof",
				"static",
				"string",
				"struct",
				"switch",
				"typeof",
				"unsafe",
				"ushort",
			},
			new string[] {  // 7 characters
                "checked",
				"decimal",
				"default",
				"finally",
				"foreach",
				"private",
				"virtual",
			},
			new string[] {  // 8 characters
                "abstract",
				"continue",
				"delegate",
				"explicit",
				"implicit",
				"internal",
				"operator",
				"override",
				"readonly",
				"volatile",
			},
			new string[] {  // 9 characters
                "__arglist",
				"__makeref",
				"__reftype",
				"interface",
				"namespace",
				"protected",
				"unchecked",
			},
			new string[] {  // 10 characters
                "__refvalue",
				"stackalloc",
			},
		};
		#endregion
	}
}
