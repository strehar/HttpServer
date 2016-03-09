using System.IO;
using System.Text.RegularExpressions;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace DotLiquidCore.Tags
{
	public class Comment : DotLiquidCore.Block
	{
		public static string FromShortHand(string @string)
		{
			if (@string == null)
				return @string;

			Match match = Regex.Match(@string, Liquid.CommentShorthand);
			return match.Success ? string.Format(@"{{% comment %}}{0}{{% endcomment %}}", match.Groups[1].Value) : @string;
		}

		public override void Render(Context context, TextWriter result)
		{
		}
	}
}