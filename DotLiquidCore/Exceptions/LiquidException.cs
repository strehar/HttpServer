#region Changes
/*
Changed by Miha Strehar in 2016, Removed [Serializable] as it does not exist in .NET Core 
Changed ApplicationException to Exception.
*/
#endregion
using System;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace DotLiquidCore.Exceptions
{
    public abstract class LiquidException : Exception
	{
		protected LiquidException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		protected LiquidException(string message)
			: base(message)
		{
		}

		protected LiquidException()
		{
		}
	}
}