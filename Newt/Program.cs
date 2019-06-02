using System;
using System.Collections.Generic;
using System.IO;
using Grimoire;
namespace Newt
{
	class Program
	{
		static int Main(string[] args)
		{
			var mode = 0;
			string infile = null;
			string file2 = null; // sometimes outfile, sometimes input document
			var al = -1;
			var ns = "";
			for (var i = 0; i < args.Length; i++)
			{
				string s = null;
				if (args[i].StartsWith("/"))
				{
					s = args[i].Substring(1);
					if (0 > al)
						al = i;
				}
				else if (args[i].StartsWith("--"))
				{
					s = args[i].Substring(2);
					if (0 > al)
						al = i;
				}
				if (null != s)
				{
					switch (s.ToLowerInvariant())
					{
						case "namespace":
							++i; // steal the next value
							if (i < args.Length)
								ns = args[i];
							break;
						case "parse":
							mode = 1;
							break;

					}
				}

			}
			if (0 > al) al = args.Length;
			switch (al)
			{
				case 0:
					break;
				case 2:
					file2 = args[1];
					goto case 1;
				case 1:
					infile = args[0];
					break;
				default:
					PrintUsage();
					return 1;
			}
			if ("" == infile) infile = null;
			if ("" == file2) file2 = null;
			EbnfDocument doc = null;
			if (mode == 1) // parse 
			{
				return DoParse(infile, file2);
			}
			var msgs = new List<object>();
			using (var sw = (null == file2) ? Console.Out : new StreamWriter(File.OpenWrite(file2)))
			{
				var sww = sw as StreamWriter;
				if (null != sww)
					sww.BaseStream.SetLength(0L);

				using (var sr = (null == infile) ? Console.In : new StreamReader(infile))
				{
					try
					{
						doc = EbnfDocument.ReadFrom(sr);
					}
					catch (ExpectingException ex)
					{
						var em = string.Concat("Error parsing grammar: ", ex.Message);
						msgs.Add(em);
						Console.Error.WriteLine(em);
						WriteHeader(sw, infile, msgs);
						return 2;
					}
				}
				var hasErrors = false;
				foreach (var m in doc.Prepare(false))
				{
					msgs.Add(m);
					Console.Error.WriteLine(m);
					if (EbnfErrorLevel.Error == m.ErrorLevel)
						hasErrors = true;
				}

				if (hasErrors)
				{
					// make sure to dump the messages
					WriteHeader(sw, infile, msgs);
					return 3;
				}

				var name = (file2 != null) ? Path.GetFileNameWithoutExtension(file2) : doc.StartProduction + "Parser";
				var cfg = doc.ToCfg();

				foreach (var m in cfg.PrepareLL1(false))
				{
					msgs.Add(m);
					Console.Error.WriteLine(m);
					if (CfgErrorLevel.Error == m.ErrorLevel)
						hasErrors = true;
				}
				if (hasErrors)
				{
					WriteHeader(sw, infile, msgs);
					return 4;
				}
				Console.Error.WriteLine();
				Console.Error.WriteLine("Final grammar:");
				Console.Error.WriteLine();
				Console.Error.WriteLine(cfg);
				Console.Error.WriteLine();
				Console.Error.WriteLine("{0} Terminals, {1} NonTerminals, {2} Total Symbols", cfg.Terminals.Count, cfg.NonTerminals.Count, cfg.Symbols.Count);
				var lexer = doc.ToLexer(cfg);
				WriteHeader(sw, infile, msgs);
				var hasNS = !string.IsNullOrEmpty(ns);
				if (hasNS)
					sw.WriteLine(string.Concat("namespace ", ns, " {"));

				cfg.WriteCSharpTableDrivenLL1ParserClassTo(sw, name, null, lexer);
				if (hasNS)
					sw.WriteLine("}");

			}

			return 0;

		}
		public static void PrintUsage()
		{
			Console.Error.WriteLine("Usage: newt [<grammarfile> [<outputfile>]] [/namespace <nsname>]");
			Console.Error.WriteLine("Generates code for an LL(1) parser in C# based on an EBNF grammar.");
			Console.Error.WriteLine();
			Console.Error.WriteLine("\t<grammarfile>: The input grammar to create a parser for, or unspecified for <stdin>");
			Console.Error.WriteLine();
			Console.Error.WriteLine("\t<outputfile>: The new C# class file to generate a parser to, or unspecified for <stdout>");
			Console.Error.WriteLine();
			Console.Error.WriteLine("\t<nsname>: The namespace under which to generate the specified class.");
			Console.Error.WriteLine();
			Console.Error.WriteLine("If <outputfile> is specified, the name of the class will be the name of the file withough the path or extension");
			Console.Error.WriteLine("If <outputfile> is not specified, the name of the class will be the name of start symbol with \"Parser\" appended.");

		}
		public static void WriteHeader(TextWriter writer, string infile, IEnumerable<object> msgs)
		{
			if (null != infile)
				writer.WriteLine(string.Concat("#line 1 \"", Path.GetFullPath(infile).Replace("\"", "\"\""), "\""));
			foreach (var m in msgs)
			{
				var em = m as EbnfMessage;
				if (null != em)
				{
					if (EbnfErrorLevel.Warning == em.ErrorLevel)
						writer.Write("#warning ");
					else if (EbnfErrorLevel.Error == em.ErrorLevel)
						writer.Write("#error ");
					else if (EbnfErrorLevel.Message == em.ErrorLevel)
						writer.Write("// Generator ");
					else
						continue;
					writer.WriteLine(em.ToString());
				}
				else
				{
					var cm = m as CfgMessage;
					if (null != cm)
					{
						if (CfgErrorLevel.Warning == cm.ErrorLevel)
							writer.Write("#warning ");
						else if (CfgErrorLevel.Error == cm.ErrorLevel)
							writer.Write("#error ");
						else if (CfgErrorLevel.Message == cm.ErrorLevel)
							writer.Write("// Generator ");
						else
							continue;
						writer.WriteLine(cm.ToString());
					}
					else
						writer.WriteLine(string.Concat("#error ", m));
				}
			}
		}
		static int DoParse(string grammarfile, string inputFile)
		{
			throw new NotImplementedException();
		}
	}
}