using System;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace DotLiquidCore.NamingConventions
{
	public interface INamingConvention
	{
		StringComparer StringComparer { get; }
		string GetMemberName(string name);
	}
}