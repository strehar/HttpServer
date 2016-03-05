using System;

namespace DotLiquidCore.NamingConventions
{
	public interface INamingConvention
	{
		StringComparer StringComparer { get; }
		string GetMemberName(string name);
	}
}