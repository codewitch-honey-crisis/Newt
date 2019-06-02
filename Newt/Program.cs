using System;
using System.Collections.Generic;
using Grimoire;
namespace Newt
{
	class Program
	{
		static void Main(string[] args)
		{
			var file = @"..\..\..\ebnf.ebnf";
			var doc = EbnfDocument.ReadFrom(file);
			foreach (var msg in doc.Prepare())
				Console.Error.WriteLine(msg);
			Console.Error.WriteLine();
			var cfg = doc.ToCfg();
			foreach(var msg in cfg.PrepareLL1())
				Console.Error.WriteLine(msg);
			Console.Error.WriteLine();
			var lexer = doc.ToLexer(cfg);
			//var parser = new RuntimeLL1Parser(cfg, lexer, ParseContext.CreateFromFile(file));
			var parser = new EbnfParser(ParseContext.CreateFromFile(file));
			Console.WriteLine(parser.ParseSubtree().ToString(parser));
			//cfg.WriteCSharpTableDrivenLL1ParserClassTo(Console.Out,"EbnfParser","public",lexer);
			//cfg.WriteCSharpLL1ParseTableCreateExpressionTo(Console.Out);
			return;
		
		}
		static void TestConflicts()
		{
			var file = @"..\..\..\test.ebnf";
			var doc = EbnfDocument.ReadFrom(file);
			Console.WriteLine(doc);
			foreach (var msg in doc.Prepare())
				Console.Error.WriteLine(msg);
			Console.Error.WriteLine();
			var cfg = doc.ToCfg();
			foreach (var msg in cfg.PrepareLL1())
				Console.Error.WriteLine(msg);
			Console.Error.WriteLine();
			return;

		}
		static EbnfDocument Example1()
		{
			var doc = new EbnfDocument();
			// E= T E'
			doc.Productions.Add("E",
				new EbnfProduction(
					new EbnfConcatExpression(
						new EbnfRefExpression("T"),
						new EbnfRefExpression("E'"))));
			// E' = "+" T E' | 
			doc.Productions.Add("E'",
				new EbnfProduction(
					new EbnfOrExpression(
						new EbnfConcatExpression(
							new EbnfConcatExpression(
								new EbnfLiteralExpression("+"),
								new EbnfRefExpression("T")),
							new EbnfRefExpression("E'"))
						, null)));
			// T = F T'
			doc.Productions.Add("T",
				new EbnfProduction(
					new EbnfConcatExpression(
						new EbnfRefExpression("F"),
						new EbnfRefExpression("T'"))));

			// T' = "*" F T' | 
			doc.Productions.Add("T'",
				new EbnfProduction(
					new EbnfOrExpression(
						new EbnfConcatExpression(
							new EbnfConcatExpression(
								new EbnfLiteralExpression("*"),
								new EbnfRefExpression("F")),
							new EbnfRefExpression("T'"))
						, null)));

			// F= "(" E ")" | int
			doc.Productions.Add("F",
				new EbnfProduction(
					new EbnfOrExpression(
						new EbnfRefExpression("int"),
						new EbnfConcatExpression(
							new EbnfConcatExpression(
								new EbnfLiteralExpression("("),
								new EbnfRefExpression("E")
								),
							new EbnfLiteralExpression(")")
							)
						)));
			doc.Productions.Add("int",
				new EbnfProduction(
					new EbnfRegexExpression("[0-9]+")));
			doc.StartProduction = "E";
			return doc;
		}
		static EbnfDocument Example2()
		{
			var doc = new EbnfDocument();
			// E->E + T | T
			// T->T * F | F
			// F-> (E) | int
			doc.Productions.Add("E",
				new EbnfProduction(
					new EbnfOrExpression(
						new EbnfConcatExpression(
							"E",
							new EbnfLiteralExpression("+"),
							"T"),
						"T")));
			doc.Productions.Add("T",
				new EbnfProduction(
					new EbnfOrExpression(
						new EbnfConcatExpression(
							"T",
							new EbnfLiteralExpression("*"),
							"F"),
					"F")));
			doc.Productions.Add("F",
				new EbnfProduction(
					new EbnfOrExpression(
						new EbnfConcatExpression(
								new EbnfLiteralExpression("("),
								"E",
								new EbnfLiteralExpression(")")
								),
						"int")));
			doc.Productions.Add("int",
				new EbnfProduction(
					new EbnfRegexExpression("[0-9]+")));
			doc.StartProduction = "E";
			return doc;
		}
	}
}
