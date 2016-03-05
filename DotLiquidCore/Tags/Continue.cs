using System.IO;
using DotLiquidCore.Exceptions;

namespace DotLiquidCore.Tags
{
    public class Continue : Tag
    {
        public override void Render(Context context, TextWriter result)
        {
            throw new ContinueInterrupt();
        }
    }
}