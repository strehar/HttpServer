using System.IO;

namespace DotLiquidCore
{
	internal interface IRenderable
	{
		void Render(Context context, TextWriter result);
	}
}