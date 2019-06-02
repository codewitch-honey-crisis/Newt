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
	class EbnfRepeatExpression :EbnfUnaryExpression, IEquatable<EbnfRepeatExpression>,ICloneable
	{
		public EbnfRepeatExpression(EbnfExpression expression) { Expression = expression; }
		public EbnfRepeatExpression() { }
		public override bool IsTerminal => false;

		public override IList<IList<string>> ToDisjunctions(EbnfDocument parent,Cfg cfg)
		{
			var _listId = cfg.GetUniqueId("implicitlist");
			IDictionary<string, object> attrs = new Dictionary<string, object>();
			attrs.Add("collapse", true);
			cfg.AttributeSets.Add(_listId, attrs);
			var expr = new EbnfOrExpression(new EbnfOrExpression(new EbnfConcatExpression(Expression,new EbnfRefExpression(_listId)), Expression), null);
			foreach (var nt in expr.ToDisjunctions(parent, cfg))
			{
				CfgRule r = new CfgRule();
				r.Left = _listId;
				foreach (var s in nt)
				{
					if (1 < r.Right.Count && null == s)
						continue;
					r.Right.Add(s);
				}
				if (!cfg.Rules.Contains(r))
					cfg.Rules.Add(r);
			}
			return new List<IList<string>>(new IList<string>[] { new List<string>(new string[] { _listId }) });
		}
		public EbnfRepeatExpression Clone() {
			var result = new EbnfRepeatExpression(Expression);
			result.SetPositionInfo(Line, Column, Position);
			return result;
		}

		object ICloneable.Clone() => Clone();
		public bool Equals(EbnfRepeatExpression rhs)
		{
			if (ReferenceEquals(rhs, this)) return true;
			if (ReferenceEquals(rhs, null)) return false;
			return Equals(Expression, rhs.Expression);
		}
		public override bool Equals(object obj) => Equals(obj as EbnfRepeatExpression);
		public override int GetHashCode()
		{
			if (null != Expression) return Expression.GetHashCode();
			return 0;
		}
		public static bool operator ==(EbnfRepeatExpression lhs, EbnfRepeatExpression rhs)
		{
			if (ReferenceEquals(lhs, rhs)) return true;
			if (ReferenceEquals(lhs, null) || ReferenceEquals(rhs, null)) return false;
			return lhs.Equals(rhs);
		}
		public static bool operator !=(EbnfRepeatExpression lhs, EbnfRepeatExpression rhs)
		{
			if (ReferenceEquals(lhs, rhs)) return false;
			if (ReferenceEquals(lhs, null) || ReferenceEquals(rhs, null)) return true;
			return !lhs.Equals(rhs);
		}
		public override string ToString()
		{
			if (null == Expression) return "{ }";
			return string.Concat("{ ", Expression.ToString(), " }");
		}
	}
}
