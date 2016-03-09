#region Changes
/*
Changed by Miha Strehar in 2016, Removed [Serializable] as it does not exist in .NET Core and
cleaned up unnecessary using.
*/
#endregion

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace DotLiquidCore.Exceptions
{
    public class FilterNotFoundException : LiquidException
	{
		public FilterNotFoundException(string message, FilterNotFoundException innerException)
			: base(message, innerException)
		{
		}

		public FilterNotFoundException(string message, params string[] args)
			: base(string.Format(message, args))
		{
		}

		public FilterNotFoundException(string message)
			: base(message)
		{
		}
	}
}