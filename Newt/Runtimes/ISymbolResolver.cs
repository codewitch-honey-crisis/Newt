using System;
using System.Collections.Generic;
using System.Text;

namespace Grimoire
{
#if GRIMOIRELIB || NEWT
	public
#else
	internal
#endif
	interface ISymbolResolver
	{
		string GetSymbolById(int symbolId);
		int GetSymbolId(string symbol);
	}
}
