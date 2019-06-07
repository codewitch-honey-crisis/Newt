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

		private void Main_TextChanged(object sender, EventArgs e)
		{
			
			Colorize();
		}
		void Colorize()
		{
#if PARSER
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
			var pos = 0L;
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
#endif
		}

	}
}
