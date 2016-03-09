using System;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace DotLiquidCore.NamingConventions
{
	public class CSharpNamingConvention : INamingConvention
	{
		public StringComparer StringComparer
		{
			get { return StringComparer.Ordinal; }
		}

		public string GetMemberName(string name)
		{
			return name;
		}
	}
}