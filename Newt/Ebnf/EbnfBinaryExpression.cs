using System;
using System.Collections.Generic;
using System.Text;

namespace Grimoire
{
#if GRIMOIRELIB
	public
#else
	internal
#endif
	abstract class EbnfBinaryExpression :EbnfExpression
	{
		public EbnfExpression Left { get; set; } = null;
		public EbnfExpression Right { get; set; } = null;
	}
}
