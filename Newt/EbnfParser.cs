using System;
using System.Collections.Generic;
using System.Text;

namespace Grimoire
{
	partial class EbnfParser : Grimoire.TableDrivenLL1Parser
	{
		public EbnfParser(Grimoire.ParseContext parseContext = null) : base(_ParseTable, _StartingConfiguration, _LexTable, _Symbols, _SubstitutionsAndHiddenTerminals, _BlockEnds, _CollapsedNonTerminals, _Types, parseContext) { }
		public const int EOS = 36;
		public const int ERROR = 37;
		public const int grammar = 1;
		public const int expressions = 2;
		public const int expression = 4;
		public const int symbol = 5;
		public const int attributes = 7;
		public const int attrvalue = 8;
		public const int production = 10;
		public const int attribute = 15;
		public const int literal = 17;
		public const int regex = 18;
		public const int identifier = 19;
		public const int lparen = 20;
		public const int rparen = 21;
		public const int lbracket = 22;
		public const int rbracket = 23;
		public const int lbrace = 24;
		public const int rbrace = 25;
		public const int integer = 26;
		public const int lt = 27;
		public const int gt = 28;
		public const int eq = 29;
		public const int semi = 30;
		public const int or = 31;
		public const int comma = 32;
		static readonly string[] _Symbols = { "implicitlist", "grammar", "expressions", "implicitlist2", "expression", "symbol", "implicitlist3", "attributes", "attrvalue", "implicitlist`", "production", "production`", "expressions`", "implicitlist2`", "implicitlist3`", "attribute", "attribute`", "literal", "regex", "identifier", "lparen", "rparen", "lbracket", "rbracket", "lbrace", "rbrace", "integer", "lt", "gt", "eq", "semi", "or", "comma", "whitespace", "lineComment", "blockComment", "#EOS", "#ERROR" };
		static readonly (int Left, int[] Right)[][] _ParseTable = new (int Left, int[] Right)[][] {
		new (int Left, int[] Right)[] {
				(-1,null)
				,(-1,null)
				,(0, new System.Int32[] { 10, 9 })
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(0, new System.Int32[] { })
				}
		,new (int Left, int[] Right)[] {
				(-1,null)
				,(-1,null)
				,(1, new System.Int32[] { 10, 0 })
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				}
		,new (int Left, int[] Right)[] {
				(2, new System.Int32[] { 4, 12 })
				,(2, new System.Int32[] { 4, 12 })
				,(2, new System.Int32[] { 4, 12 })
				,(2, new System.Int32[] { 4, 12 })
				,(2, new System.Int32[] { })
				,(2, new System.Int32[] { 4, 12 })
				,(2, new System.Int32[] { })
				,(2, new System.Int32[] { 4, 12 })
				,(2, new System.Int32[] { })
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(2, new System.Int32[] { })
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				}
		,new (int Left, int[] Right)[] {
				(3, new System.Int32[] { 5, 13 })
				,(3, new System.Int32[] { 5, 13 })
				,(3, new System.Int32[] { 5, 13 })
				,(3, new System.Int32[] { 5, 13 })
				,(3, new System.Int32[] { })
				,(3, new System.Int32[] { 5, 13 })
				,(3, new System.Int32[] { })
				,(3, new System.Int32[] { 5, 13 })
				,(3, new System.Int32[] { })
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(3, new System.Int32[] { })
				,(3, new System.Int32[] { })
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				}
		,new (int Left, int[] Right)[] {
				(4, new System.Int32[] { 3 })
				,(4, new System.Int32[] { 3 })
				,(4, new System.Int32[] { 3 })
				,(4, new System.Int32[] { 3 })
				,(-1,null)
				,(4, new System.Int32[] { 3 })
				,(-1,null)
				,(4, new System.Int32[] { 3 })
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				}
		,new (int Left, int[] Right)[] {
				(5, new System.Int32[] { 17 })
				,(5, new System.Int32[] { 18 })
				,(5, new System.Int32[] { 19 })
				,(5, new System.Int32[] { 20, 2, 21 })
				,(-1,null)
				,(5, new System.Int32[] { 22, 2, 23 })
				,(-1,null)
				,(5, new System.Int32[] { 24, 2, 25 })
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				}
		,new (int Left, int[] Right)[] {
				(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(6, new System.Int32[] { })
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(6, new System.Int32[] { 32, 15, 14 })
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				}
		,new (int Left, int[] Right)[] {
				(-1,null)
				,(-1,null)
				,(7, new System.Int32[] { 15, 6 })
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				}
		,new (int Left, int[] Right)[] {
				(8, new System.Int32[] { 17 })
				,(-1,null)
				,(8, new System.Int32[] { 19 })
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(8, new System.Int32[] { 26 })
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				}
		,new (int Left, int[] Right)[] {
				(-1,null)
				,(-1,null)
				,(9, new System.Int32[] { 0 })
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(9, new System.Int32[] { })
				}
		,new (int Left, int[] Right)[] {
				(-1,null)
				,(-1,null)
				,(10, new System.Int32[] { 19, 11 })
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				}
		,new (int Left, int[] Right)[] {
				(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(11, new System.Int32[] { 27, 7, 28, 29, 2, 30 })
				,(-1,null)
				,(11, new System.Int32[] { 29, 2, 30 })
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				}
		,new (int Left, int[] Right)[] {
				(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(12, new System.Int32[] { })
				,(-1,null)
				,(12, new System.Int32[] { })
				,(-1,null)
				,(12, new System.Int32[] { })
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(12, new System.Int32[] { })
				,(12, new System.Int32[] { 31, 2 })
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				}
		,new (int Left, int[] Right)[] {
				(13, new System.Int32[] { 3 })
				,(13, new System.Int32[] { 3 })
				,(13, new System.Int32[] { 3 })
				,(13, new System.Int32[] { 3 })
				,(13, new System.Int32[] { })
				,(13, new System.Int32[] { 3 })
				,(13, new System.Int32[] { })
				,(13, new System.Int32[] { 3 })
				,(13, new System.Int32[] { })
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(13, new System.Int32[] { })
				,(13, new System.Int32[] { })
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				}
		,new (int Left, int[] Right)[] {
				(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(14, new System.Int32[] { })
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(14, new System.Int32[] { 6 })
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				}
		,new (int Left, int[] Right)[] {
				(-1,null)
				,(-1,null)
				,(15, new System.Int32[] { 19, 16 })
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				}
		,new (int Left, int[] Right)[] {
				(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(16, new System.Int32[] { })
				,(16, new System.Int32[] { 29, 8 })
				,(-1,null)
				,(-1,null)
				,(16, new System.Int32[] { })
				,(-1,null)
				,(-1,null)
				,(-1,null)
				,(-1,null)
				}
};

		static readonly int[] _SubstitutionsAndHiddenTerminals = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, -2, -2, -2, 36, 37, -1 };
		static readonly (int SymbolId, bool IsNonTerminal, int NonTerminalCount) _StartingConfiguration = (1, true, 17);
		static readonly string[] _BlockEnds = new string[] {
		null
		,null
		,null
		,null
		,null
		,null
		,null
		,null
		,null
		,null
		,null
		,null
		,null
		,null
		,null
		,null
		,null
		,null
		,null
		,null
		,null
		,null
		,null
		,null
		,null
		,null
		,null
		,null
		,null
		,null
		,null
		,null
		,null
		,null
		,null
		,"*/"
		,null
		,null
};
		static readonly System.Type[] _Types = new System.Type[] {
		null
		,null
		,null
		,null
		,null
		,null
		,null
		,null
		,null
		,null
		,null
		,null
		,null
		,null
		,null
		,null
		,null
		,null
		,null
		,null
		,null
		,null
		,null
		,null
		,null
		,null
		,null
		,null
		,null
		,null
		,null
		,null
		,null
		,null
		,null
		,null
		,null
		,null
};
		static readonly int[] _CollapsedNonTerminals = new int[] {
-3,-1,-1,-3,-1,-1,-3,-1,-1,-3,-1,-3,-3,-3,-3,-1,-3,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1};
		static readonly (int Accept, ((char First, char Last)[] Ranges, int Destination)[] Transitions, int[] PossibleAccepts)[] _LexTable = new (System.Int32 Accept, ((char First, char Last)[] Ranges, int Destination)[] Transitions, System.Int32[] PossibleAccepts)[] {
		(-1, new ((char First, char Last)[] Ranges, int Destination)[] {
				(new (char First,char Last)[] {
						((char)34,(char)34)
},1)            ,(new (char First,char Last)[] {
						((char)39,(char)39)
},5)            ,(new (char First,char Last)[] {
						((char)65,(char)90)
						,((char)95,(char)95)
						,((char)97,(char)122)
},9)            ,(new (char First,char Last)[] {
						((char)45,(char)45)
},11)           ,(new (char First,char Last)[] {
						((char)48,(char)57)
},12)           ,(new (char First,char Last)[] {
						((char)9,(char)13)
						,((char)32,(char)32)
},13)           ,(new (char First,char Last)[] {
						((char)47,(char)47)
},14)           ,(new (char First,char Last)[] {
						((char)124,(char)124)
},18)           ,(new (char First,char Last)[] {
						((char)60,(char)60)
},19)           ,(new (char First,char Last)[] {
						((char)62,(char)62)
},20)           ,(new (char First,char Last)[] {
						((char)61,(char)61)
},21)           ,(new (char First,char Last)[] {
						((char)59,(char)59)
},22)           ,(new (char First,char Last)[] {
						((char)44,(char)44)
},23)           ,(new (char First,char Last)[] {
						((char)40,(char)40)
},24)           ,(new (char First,char Last)[] {
						((char)41,(char)41)
},25)           ,(new (char First,char Last)[] {
						((char)91,(char)91)
},26)           ,(new (char First,char Last)[] {
						((char)93,(char)93)
},27)           ,(new (char First,char Last)[] {
						((char)123,(char)123)
},28)           ,(new (char First,char Last)[] {
						((char)125,(char)125)
},29)}, new int[] { 17, 18, 19, 19, 26, 33, 34, 34, 35, 31, 27, 28, 29, 30, 32, 20, 21, 22, 23, 24, 25 })
		,(-1, new ((char First, char Last)[] Ranges, int Destination)[] {
				(new (char First,char Last)[] {
						((char)0,(char)33)
						,((char)35,(char)91)
						,((char)93,(char)65535)
},2)            ,(new (char First,char Last)[] {
						((char)92,(char)92)
},4)            ,(new (char First,char Last)[] {
						((char)34,(char)34)
},3)}, new int[] { 17 })
		,(-1, new ((char First, char Last)[] Ranges, int Destination)[] {
				(new (char First,char Last)[] {
						((char)34,(char)34)
},3)            ,(new (char First,char Last)[] {
						((char)0,(char)33)
						,((char)35,(char)91)
						,((char)93,(char)65535)
},2)            ,(new (char First,char Last)[] {
						((char)92,(char)92)
},4)}, new int[] { 17 })
		,(17, new ((char First, char Last)[] Ranges, int Destination)[] {
}, new int[] { 17 })
		,(-1, new ((char First, char Last)[] Ranges, int Destination)[] {
				(new (char First,char Last)[] {
						((char)0,(char)65535)
},2)}, new int[] { 17 })
		,(-1, new ((char First, char Last)[] Ranges, int Destination)[] {
				(new (char First,char Last)[] {
						((char)0,(char)38)
						,((char)40,(char)91)
						,((char)93,(char)65535)
},6)            ,(new (char First,char Last)[] {
						((char)92,(char)92)
},8)            ,(new (char First,char Last)[] {
						((char)39,(char)39)
},7)}, new int[] { 18 })
		,(-1, new ((char First, char Last)[] Ranges, int Destination)[] {
				(new (char First,char Last)[] {
						((char)39,(char)39)
},7)            ,(new (char First,char Last)[] {
						((char)0,(char)38)
						,((char)40,(char)91)
						,((char)93,(char)65535)
},6)            ,(new (char First,char Last)[] {
						((char)92,(char)92)
},8)}, new int[] { 18 })
		,(18, new ((char First, char Last)[] Ranges, int Destination)[] {
}, new int[] { 18 })
		,(-1, new ((char First, char Last)[] Ranges, int Destination)[] {
				(new (char First,char Last)[] {
						((char)0,(char)65535)
},6)}, new int[] { 18 })
		,(19, new ((char First, char Last)[] Ranges, int Destination)[] {
				(new (char First,char Last)[] {
						((char)45,(char)45)
						,((char)48,(char)57)
						,((char)65,(char)90)
						,((char)95,(char)95)
						,((char)97,(char)122)
},10)}, new int[] { 19, 19 })
		,(19, new ((char First, char Last)[] Ranges, int Destination)[] {
				(new (char First,char Last)[] {
						((char)45,(char)45)
						,((char)48,(char)57)
						,((char)65,(char)90)
						,((char)95,(char)95)
						,((char)97,(char)122)
},10)}, new int[] { 19 })
		,(-1, new ((char First, char Last)[] Ranges, int Destination)[] {
				(new (char First,char Last)[] {
						((char)48,(char)57)
},12)}, new int[] { 26 })
		,(26, new ((char First, char Last)[] Ranges, int Destination)[] {
				(new (char First,char Last)[] {
						((char)48,(char)57)
},12)}, new int[] { 26 })
		,(33, new ((char First, char Last)[] Ranges, int Destination)[] {
				(new (char First,char Last)[] {
						((char)9,(char)13)
						,((char)32,(char)32)
},13)}, new int[] { 33 })
		,(-1, new ((char First, char Last)[] Ranges, int Destination)[] {
				(new (char First,char Last)[] {
						((char)47,(char)47)
},15)           ,(new (char First,char Last)[] {
						((char)42,(char)42)
},17)}, new int[] { 34, 34, 35 })
		,(34, new ((char First, char Last)[] Ranges, int Destination)[] {
				(new (char First,char Last)[] {
						((char)0,(char)9)
						,((char)11,(char)65535)
},16)}, new int[] { 34, 34 })
		,(34, new ((char First, char Last)[] Ranges, int Destination)[] {
				(new (char First,char Last)[] {
						((char)0,(char)9)
						,((char)11,(char)65535)
},16)}, new int[] { 34 })
		,(35, new ((char First, char Last)[] Ranges, int Destination)[] {
}, new int[] { 35 })
		,(31, new ((char First, char Last)[] Ranges, int Destination)[] {
}, new int[] { 31 })
		,(27, new ((char First, char Last)[] Ranges, int Destination)[] {
}, new int[] { 27 })
		,(28, new ((char First, char Last)[] Ranges, int Destination)[] {
}, new int[] { 28 })
		,(29, new ((char First, char Last)[] Ranges, int Destination)[] {
}, new int[] { 29 })
		,(30, new ((char First, char Last)[] Ranges, int Destination)[] {
}, new int[] { 30 })
		,(32, new ((char First, char Last)[] Ranges, int Destination)[] {
}, new int[] { 32 })
		,(20, new ((char First, char Last)[] Ranges, int Destination)[] {
}, new int[] { 20 })
		,(21, new ((char First, char Last)[] Ranges, int Destination)[] {
}, new int[] { 21 })
		,(22, new ((char First, char Last)[] Ranges, int Destination)[] {
}, new int[] { 22 })
		,(23, new ((char First, char Last)[] Ranges, int Destination)[] {
}, new int[] { 23 })
		,(24, new ((char First, char Last)[] Ranges, int Destination)[] {
}, new int[] { 24 })
		,(25, new ((char First, char Last)[] Ranges, int Destination)[] {
}, new int[] { 25 })
}
;
	}
}
