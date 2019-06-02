#line 1 "C:\dev\Newt\Eval\Expr.ebnf"
// Generator Message: Removing rule expr -> expr add term because it is directly left recursive.
// Generator Message: Adding rule expr` -> add term expr` to replace rule expr -> expr add term
// Generator Message: Adding rule expr` -> to replace rule expr -> expr add term
// Generator Message: Removing rule term -> term mul factor because it is directly left recursive.
// Generator Message: Adding rule term` -> mul factor term` to replace rule term -> term mul factor
// Generator Message: Adding rule term` -> to replace rule term -> term mul factor
partial class ExprParser : Grimoire.TableDrivenLL1Parser {
	public ExprParser(Grimoire.ParseContext parseContext=null) : base(_ParseTable,_StartingConfiguration,_LexTable,_Symbols,_SubstitutionsAndHiddenTerminals,_BlockEnds,_CollapsedNonTerminals,_Types,parseContext) { }
	public const int EOS=11;
	public const int ERROR=12;
	public const int expr = 0;
	public const int term = 1;
	public const int factor = 2;
	public const int lparen = 5;
	public const int rparen = 6;
	public const int @int = 7;
	public const int add = 8;
	public const int mul = 9;
	static readonly string[] _Symbols = {"expr", "term", "factor", "expr`", "term`", "lparen", "rparen", "int", "add", "mul", "whitespace", "#EOS", "#ERROR" };
	static readonly (int Left, int[] Right)[][] _ParseTable = new (int Left, int[] Right)[][] {
	new (int Left, int[] Right)[] {
		(0, new System.Int32[] { 1, 3 })
		,(-1,null)
		,(0, new System.Int32[] { 1, 3 })
		,(-1,null)
		,(-1,null)
		,(-1,null)
		,(-1,null)
		}
	,new (int Left, int[] Right)[] {
		(1, new System.Int32[] { 2, 4 })
		,(-1,null)
		,(1, new System.Int32[] { 2, 4 })
		,(-1,null)
		,(-1,null)
		,(-1,null)
		,(-1,null)
		}
	,new (int Left, int[] Right)[] {
		(2, new System.Int32[] { 5, 0, 6 })
		,(-1,null)
		,(2, new System.Int32[] { 7 })
		,(-1,null)
		,(-1,null)
		,(-1,null)
		,(-1,null)
		}
	,new (int Left, int[] Right)[] {
		(-1,null)
		,(3, new System.Int32[] { })
		,(-1,null)
		,(3, new System.Int32[] { 8, 1, 3 })
		,(-1,null)
		,(-1,null)
		,(3, new System.Int32[] { })
		}
	,new (int Left, int[] Right)[] {
		(-1,null)
		,(4, new System.Int32[] { })
		,(-1,null)
		,(4, new System.Int32[] { })
		,(4, new System.Int32[] { 9, 2, 4 })
		,(-1,null)
		,(4, new System.Int32[] { })
		}
};

	static readonly int[] _SubstitutionsAndHiddenTerminals = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, -2, 11, 12, -1 };
	static readonly (int SymbolId,bool IsNonTerminal,int NonTerminalCount) _StartingConfiguration = (0, true, 5);
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
};
	static readonly System.Type[] _Types = new System.Type[] { 
	null
	,null
	,null
	,null
	,null
	,null
	,null
	,typeof(System.Int32)

	,null
	,null
	,null
	,null
	,null
};
	static readonly int[] _CollapsedNonTerminals = new int[] { 
-1,-1,-1,-3,-3,-1,-1,-1,-1,-1,-1,-1,-1};
	static readonly (int Accept, ((char First, char Last)[] Ranges, int Destination)[] Transitions, int[] PossibleAccepts)[] _LexTable = new (System.Int32 Accept, ((char First, char Last)[] Ranges, int Destination)[] Transitions, System.Int32[] PossibleAccepts)[] {
	(-1, new ((char First, char Last)[] Ranges, int Destination)[] {
		(new (char First,char Last)[] {
			((char)43,(char)43)
			,((char)45,(char)45)
},1)		,(new (char First,char Last)[] {
			((char)42,(char)42)
			,((char)47,(char)47)
},2)		,(new (char First,char Last)[] {
			((char)40,(char)40)
},3)		,(new (char First,char Last)[] {
			((char)41,(char)41)
},4)		,(new (char First,char Last)[] {
			((char)48,(char)57)
},5)		,(new (char First,char Last)[] {
			((char)9,(char)10)
			,((char)13,(char)13)
			,((char)32,(char)32)
},6)}, new int[] { 8, 9, 5, 6, 7, 10 })
	,(8, new ((char First, char Last)[] Ranges, int Destination)[] {
}, new int[] { 8 })
	,(9, new ((char First, char Last)[] Ranges, int Destination)[] {
}, new int[] { 9 })
	,(5, new ((char First, char Last)[] Ranges, int Destination)[] {
}, new int[] { 5 })
	,(6, new ((char First, char Last)[] Ranges, int Destination)[] {
}, new int[] { 6 })
	,(7, new ((char First, char Last)[] Ranges, int Destination)[] {
		(new (char First,char Last)[] {
			((char)48,(char)57)
},5)}, new int[] { 7 })
	,(10, new ((char First, char Last)[] Ranges, int Destination)[] {
		(new (char First,char Last)[] {
			((char)9,(char)10)
			,((char)13,(char)13)
			,((char)32,(char)32)
},6)}, new int[] { 10 })
}
;
}
