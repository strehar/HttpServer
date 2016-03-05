using System;

namespace DotLiquidCore.Util
{
	internal class Symbol
	{
		public Func<object, bool> EvaluationFunction { get; set; }

		public Symbol(Func<object, bool> evaluationFunction)
		{
			EvaluationFunction = evaluationFunction;
		}
	}
}