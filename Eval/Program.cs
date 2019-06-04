//#define REGEN
using System;
using Grimoire;
namespace Eval
{
	static class Program
	{
		static void Main(string[] args)
		{
#if !REGEN
			Console.WriteLine("Simple Expression Evaluator");
			Console.WriteLine("Hit Enter on a new line to exit.");
			var parser = new ExprParser();
			while (true)
			{
				Console.Write(">");
				var line = Console.ReadLine();
				if (string.IsNullOrEmpty(line)) return;
				var pc = ParseContext.Create(line);
				parser.Restart(pc);
				var ptree = parser.ParseSubtree();
				// parse subtree will not read the whole
				// text, just the subtree so in order to 
				// validate the *entire* input we must read
				// the rest.
				while(parser.Read())
				{
					switch(parser.NodeType)
					{
						case LLNodeType.Error:
							Console.Error.WriteLine("Error: " + parser.Value);
							break;
					}
				}
				
				// check if there's an error currently.
				switch (parser.NodeType)
				{
					case LLNodeType.Error:
						Console.Error.WriteLine("Error in line after expression");
						break;
				}
				
				Console.WriteLine(ptree);
				Console.WriteLine();
				try
				{
					Console.WriteLine("Evaluation: {0} = {1}", line, Eval(ptree));
				}
				catch(Exception ex)
				{
					Console.Error.WriteLine("Evaluation error: " + ex.Message);
				}
			}
#endif
		}
#if !REGEN
		static int Eval(ParseNode pn)
		{
			switch (pn.SymbolId)
			{
				case ExprParser.@int:
					// Parsed value relies on the "type" attribute specified in the grammar.
					// see expr.ebnf
					// In this grammar, we specified that it was an int, so the parser
					// has automatically parsed the value into ParsedValue for us.
					// The system uses System.ComponentModel.TypeConverter to work this
					// magic so it's extensible, and should already handle a myriad of .NET
					// types
					return (int)pn.ParsedValue;
				case ExprParser.expr:
				case ExprParser.term:
					// the nice thing about a parser
					// is you already know what the tree
					// has to look like, so it has been
					// pre validated.
					var ic = pn.Children.Count;
					var i = 0;
					var lhs = Eval(pn.Children[i]);
					++i;
					if (i < ic)
					{
						var op = pn.Children[i].Value;
						++i;
						for (; i < ic; ++i)
						{
							var rhs = Eval(pn.Children[i]);
							switch (op)
							{
								case "*":
									lhs *= rhs;
									break;
								case "/":
									lhs /= rhs;
									break;
								case "+":
									lhs += rhs;
									break;
								case "-":
									lhs -= rhs;
									break;
							}
							++i;
						}
					}
					return lhs;
				case ExprParser.factor:
					// it's a nested expression
					// it's possibly surrounded by 
					// parenthesis
					switch(pn.Children.Count)
					{
					
						case 1: // no parens
							return Eval(pn.Children[0]);
						case 3:
							return Eval(pn.Children[1]);
						default:
							return 0;
					}
				default:
					return 0;
			}	
		}
#endif
	}
}
