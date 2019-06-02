// if you can't get the thing to build because it can't 
// find EbnfParser, comment this out. Do the build.
// make sure EbnfParser.cs is in your project (it will
// have been generated). Then uncomment this and recompile
#define PARSER
using Grimoire;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HighlighterDemo
{
	public partial class Main : Form
	{
		RuntimeLL1Parser _parser;
		public Main()
		{
			InitializeComponent();
			var doc = EbnfDocument.ReadFrom(@"..\..\Ebnf.ebnf");
			var cfg = doc.ToCfGrammar();
			//cfg.PrepareLL1();
			var lexer = doc.ToLexer(cfg);
			_parser = new RuntimeLL1Parser(cfg, lexer, null);
			return;
		}

		void Colorize()
		{
			// this is super cheesy. the "right" (ish) way to do it is to modift RtfText directly.
			// this way flashes and scrolls all over the place
			// but this is a sample of parsing, not of using the richtextbox!
#if PARSER
			var h = EditBox.HideSelection; 
			EditBox.HideSelection = true;// <--- doesn't seem to work =(
			string text = EditBox.Text;
			
			_parser.Restart(ParseContext.Create(text));
			ParseNode tree=null;
			try
			{
				// this is so much easier with a tree involved.
				tree = _parser.ParseSubtree();
				Debug.WriteLine(tree.ToString(_parser));
			}
			catch
			{
				
				return;
			}
			var ss = EditBox.SelectionStart;
			var sl = EditBox.SelectionLength;
			EditBox.Select((int)0, text.Length);
			EditBox.SelectionColor = Color.DarkGreen;
			
			// get every node in the tree output as a list.
			foreach (var pn in tree.FillDescendantsAndSelf())
			{
				// get what and where to color from each node 
				// based on what "kind" of node it is

				if (Equals("symbol",_parser.GetSymbolById(pn.SymbolId))) // symbol
				{

					EditBox.Select((int)pn.Position, pn.Length);
					EditBox.SelectionColor = Color.Blue;

				}
				else if (Equals("regex", _parser.GetSymbolById(pn.SymbolId)))
				{

					EditBox.Select((int)pn.Position, pn.Length);
					EditBox.SelectionColor = Color.DarkRed;

				}
				else if (Equals("literal", _parser.GetSymbolById(pn.SymbolId)))
				{

					EditBox.Select((int)pn.Position, pn.Length);
					EditBox.SelectionColor = Color.DarkCyan;

				}
				else if (Equals("production", _parser.GetSymbolById(pn.SymbolId)))
				{

					EditBox.Select((int)pn.Position, pn.Length);
					EditBox.SelectionColor = Color.Black;

				}
				else if (Equals("attribute", _parser.GetSymbolById(pn.SymbolId)))
				{

					EditBox.Select((int)pn.Position, pn.Length);
					EditBox.SelectionColor = Color.DarkOrchid;

				} else if(-2==pn.SymbolId) // error
				{
					EditBox.Select((int)pn.Position, pn.Length);
					EditBox.SelectionColor = Color.DarkRed;
				}
			}
		
			EditBox.Select(ss, sl);
			EditBox.HideSelection = h;
			
#endif

		}

		private void Main_TextChanged(object sender, EventArgs e)
		{
			Colorize();
		}

	}
}
