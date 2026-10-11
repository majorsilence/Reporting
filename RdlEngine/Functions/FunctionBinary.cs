
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Majorsilence.Reporting.Rdl;


namespace Majorsilence.Reporting.Rdl
{
	/// <summary>
	/// Binary operator
	/// </summary>
	[Serializable]
	internal abstract class FunctionBinary
	{
		public IExpr _lhs;			// lhs 
		public IExpr _rhs;			// rhs

		/// <summary>
		/// Arbitrary binary operater; might be a
		/// </summary>
		public FunctionBinary() 
		{
			_lhs = null;
			_rhs = null;
		}

		public FunctionBinary(IExpr l, IExpr r) 
		{
			_lhs = l;
			_rhs = r;
		}

		public async Task<bool> IsConstant()
		{
			if (await _lhs.IsConstant().ConfigureAwait(false))
				return await _rhs.IsConstant().ConfigureAwait(false);

			return false;
		}

//		virtual public bool EvaluateBoolean(Report rpt, Row row)
//		{
//			return false;
//		}

		public IExpr Lhs
		{
			get { return  _lhs; }
			set {  _lhs = value; }
		}

		public IExpr Rhs
		{
			get { return  _rhs; }
			set {  _rhs = value; }
		}
	}
}
