using System;
using System.Collections.Generic;
using System.Text;

namespace Grimoire
{
	/// <summary>
	/// Represents a rule in a context-free grammar
	/// </summary>
	/// <remarks>This class has value semantics</remarks>
#if GRIMOIRELIB || NEWT
	public
#else
	internal
#endif
	class CfgRule : IEquatable<CfgRule>, ICloneable
	{
		/// <summary>
		/// Constructs an empty rule
		/// </summary>
		public CfgRule() { }
		/// <summary>
		/// Constructs the rule
		/// </summary>
		/// <param name="left">The left hand side of the rule.</param>
		/// <param name="right">The right hand symbols for the rule</param>
		public CfgRule(string left, params string[] right) { Left = left; Right.AddRange(right); }
		/// <summary>
		/// Constructs the rule
		/// </summary>
		/// <param name="left">The left hand side of the rule.</param>
		/// <param name="right">The right hand symbols for the rule</param>
		public CfgRule(string left, IEnumerable<string> right) { Left = left; Right.AddRange(right); }
		/// <summary>
		/// Indicates the left hand side of the rule
		/// </summary>
		/// <remarks>Any symbol appearing on the left hand side of any rule is considered non-terminal.</remarks>
		public string Left { get; set; } = null;
		/// <summary>
		/// Indicates the right hand side of the rule
		/// </summary>
		public IList<string> Right { get; } = new List<string>();

		/// <summary>
		/// Indicates whether the rule is of the form A -> ε
		/// </summary>
		public bool IsNil { get { return 0==Right.Count; } }

		/// <summary>
		/// Provides a string representation of the rule.
		/// </summary>
		/// <returns>A string of the form A -> b C representing the rule.</returns>
		public override string ToString()
		{
			var sb = new StringBuilder();
			sb.Append(Left);
			sb.Append(" ->");
			foreach (var s in Right)
			{
				sb.Append(" ");
				sb.Append(s);
			}
			return sb.ToString();
		}
		/// <summary>
		/// Indicates if the rule takes the form of A -> A ...
		/// </summary>
		public bool IsDirectlyLeftRecursive { get { return !IsNil && Equals(Left, Right[0]); } }

		#region Value Semantics
		/// <summary>
		/// Indicates whether two rules are exactly equivelant.
		/// </summary>
		/// <param name="rhs">The rule to compare this rule to.</param>
		/// <returns>True if the rules are equal, otherwise false.</returns>
		public bool Equals(CfgRule rhs)
		{
			if (!Equals(rhs.Left, Left)) return false;
			var ic = Right.Count;
			if (ic != rhs.Right.Count) return false;
			for(var i = 0;i<ic;++i)
				if (!Equals(Right[i], rhs.Right[i]))
					return false;
			return true;
		}
		/// <summary>
		/// Indicates whether two rules are exactly equivelant.
		/// </summary>
		/// <param name="obj">The rule to compare this rule to.</param>
		/// <returns>True if the rules are equal, otherwise false.</returns>
		public override bool Equals(object obj)
		{
			return Equals(obj as CfgRule);
		}
		/// <summary>
		/// Gets a hashcode for the rule
		/// </summary>
		/// <returns>A hashcode representing this rule</returns>
		public override int GetHashCode()
		{
			var result = 0;
			if (null != Left)
				result = Left.GetHashCode();
			result ^= CollectionUtility.GetHashCode(Right);
			return result;
		}
		/// <summary>
		/// Indicates if the rules are equal
		/// </summary>
		/// <param name="lhs">A rule to compare</param>
		/// <param name="rhs">A rule to compare</param>
		/// <returns>True if the rules are equal, false if they are not equal</returns>
		public static bool operator==(CfgRule lhs,CfgRule rhs)
		{
			if (ReferenceEquals(lhs, rhs)) return true;
			if (ReferenceEquals(lhs, null) || ReferenceEquals(rhs, null)) return false;
			return lhs.Equals(rhs);
		}
		/// <summary>
		/// Indicates if the rules are not equal
		/// </summary>
		/// <param name="lhs">A rule to compare</param>
		/// <param name="rhs">A rule to compare</param>
		/// <returns>True if the rules are not equal, false if they are equal</returns>
		public static bool operator !=(CfgRule lhs, CfgRule rhs)
		{
			if (ReferenceEquals(lhs, rhs)) return false;
			if (ReferenceEquals(lhs, null) || ReferenceEquals(rhs, null)) return true;
			return !lhs.Equals(rhs);
		}
		/// <summary>
		/// Performs a deep clone of the rule
		/// </summary>
		/// <returns>A copy of the rule</returns>
		public CfgRule Clone()
		{
			return new CfgRule(Left,Right);
		}
		object ICloneable.Clone() => Clone();
		#endregion
	}
}
