using System.IO;

namespace DotLiquidCore.Tags
{
	public class IfChanged : DotLiquidCore.Block
	{
		public override void Render(Context context, TextWriter result)
		{
			context.Stack(() =>
			{
				string tempString;
				using (TextWriter temp = new StringWriter())
				{
					RenderAll(NodeList, context, temp);
					tempString = temp.ToString();
				}

				if (tempString != (context.Registers["ifchanged"] as string))
				{
					context.Registers["ifchanged"] = tempString;
					result.Write(tempString);
				}
			});
		}
	}
}