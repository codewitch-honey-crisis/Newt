﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Grimoire
{
#if GRIMOIRELIB || NEWT
	public
#else
	internal
#endif
	abstract class EbnfUnaryExpression : EbnfExpression
	{
		public EbnfExpression Expression { get; set; } = null;
	}
}
