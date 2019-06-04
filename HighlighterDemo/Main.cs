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
#if PARSER
		EbnfParser _parser;
		private bool _colorizing;
#endif
		public Main()
		{
			InitializeComponent();
			_parser = new EbnfParser();
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
				Debug.WriteLine(tree);
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

				if (Equals("symbol", pn.Symbol)) // symbol
				{

					EditBox.Select((int)pn.Position, pn.Length);
					EditBox.SelectionColor = Color.Blue;

				}
				else if (Equals("regex", pn.Symbol))
				{

					EditBox.Select((int)pn.Position, pn.Length);
					EditBox.SelectionColor = Color.DarkRed;

				}
				else if (Equals("literal", pn.Symbol))
				{

					EditBox.Select((int)pn.Position, pn.Length);
					EditBox.SelectionColor = Color.DarkCyan;

				}
				else if (Equals("production", pn.Symbol))
				{

					EditBox.Select((int)pn.Position, pn.Length);
					EditBox.SelectionColor = Color.Black;

				}
				else if (Equals("attribute", pn.Symbol))
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
			
			Colorize2();
		}
		void Colorize2()
		{
			if (_colorizing)
				return;
			_colorizing = true;
			var text = EditBox.Text;
			var sel = EditBox.SelectionStart;
			EditBox.Clear();
			var sb = new StringBuilder();
			sb.Append("{\\rtf1");
			sb.Append(RtfUtility.ToColorTable(
				Color.Black, 
				Color.DarkGreen, 
				Color.DarkRed,
				Color.DarkOliveGreen,
				Color.Blue,
				Color.DarkCyan,
				Color.BlueViolet,
				Color.DarkGray));
			var p = new EbnfParser(ParseContext.Create(text));
			var pos = 0l;
			var cols = new Stack<int>();
			cols.Push(0);
			while (p.Read())
			{
				switch(p.NodeType)
				{
					case LLNodeType.NonTerminal:
						switch(p.SymbolId)
						{
							case EbnfParser.attribute:
								cols.Push(3);
								break;
							case EbnfParser.production:
								cols.Push(0);
								break;
							case EbnfParser.symbol:
							case EbnfParser.expressions:
								cols.Push(4);
								break;
							default:
								cols.Push(0);
								break;
						}
						break;
					case LLNodeType.EndNonTerminal:
						cols.Pop();
						break;
					case LLNodeType.Terminal:
					case LLNodeType.Error:
						if(p.Position>pos) {
							sb.Append("\\cf1 ");
							sb.Append(RtfUtility.Escape(text.Substring((int)pos, (int)(p.Position - pos))));
						}
						if (LLNodeType.Error == p.NodeType)
							sb.Append("\\cf2");
						else
						{
							sb.Append("\\cf");
							
							switch (p.SymbolId) {
								case EbnfParser.literal:
									sb.Append(5);
									break;
								case EbnfParser.regex:
									sb.Append(6);
									break;
								

								default:
									sb.Append(cols.Peek());
									break;
							}
							
						}
						sb.Append(RtfUtility.Escape(p.Value));
						pos = p.Position+p.Value.Length;
						break;
				}
				
			}
			sb.Append("}");
			System.Diagnostics.Debug.WriteLine(sb.ToString());
			EditBox.Rtf = sb.ToString();
			EditBox.SelectionStart = sel;
			_colorizing = false;
		}

	}
}
