using System;
using System.Diagnostics;
using System.IO;
using Grimoire;
namespace Perf
{
	class Program
	{
		static void Main(string[] args)
		{
			var file = @"..\..\..\ebnf.ebnf";
			var doc = EbnfDocument.ReadFrom(file);
			doc.Prepare();
			var cfg = doc.ToCfg();
			cfg.PrepareLL1();
			var lexer = doc.ToLexer(cfg);
			string filestring;
			using (var sr = File.OpenText(file))
				filestring = sr.ReadToEnd();
			LLParser parser = new RuntimeLL1Parser(cfg, lexer);
			parser.Restart(ParseContext.Create(filestring));
			Console.WriteLine(parser.ParseSubtree());
			var sw = new Stopwatch();
			
			sw.Restart();
			for (var i = 0; i < 100; ++i)
			{
				parser.Restart(ParseContext.Create(filestring));
				while (parser.Read()) ;
			}
			sw.Stop();
			Console.WriteLine("Runtime Parser: {0}", sw.Elapsed / 100);

			parser = new EbnfParser();
			sw.Restart();
			for (var i = 0; i < 100; ++i)
			{
				parser.Restart(ParseContext.Create(filestring));
				while (parser.Read()) ;
			}
			sw.Stop();
			Console.WriteLine("Table Driven Parser: {0}",sw.Elapsed / 100);


		}
	}
}
