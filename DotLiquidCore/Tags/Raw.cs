using System.Collections.Generic;
using System.Text.RegularExpressions;
using DotLiquidCore.Util;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace DotLiquidCore.Tags
{
	/// <summary>
	/// Raw
	/// Raw outputs text as is, usefull if your template contains Liquid syntax.
	/// 
	/// {% raw %}{% if user = 'tobi' %}hi{% endif %}{% endraw %}
	/// </summary>
	public class Raw : DotLiquidCore.Block
	{
		protected override void Parse(List<string> tokens)
		{
			NodeList = NodeList ?? new List<object>();
			NodeList.Clear();

			string token;
			while ((token = tokens.Shift()) != null)
			{
				Match fullTokenMatch = FullToken.Match(token);
				if (fullTokenMatch.Success && BlockDelimiter == fullTokenMatch.Groups[1].Value)
				{
					EndTag();
					return;
				}
				else
					NodeList.Add(token);
			}

			AssertMissingDelimitation();
		}
	}
}