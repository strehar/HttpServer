using System.IO;
using DotLiquidCore.Exceptions;

namespace DotLiquidCore.Tags
{
    public class Break : Tag
    {
        public override void Render(Context context, TextWriter result)
        {
            throw new BreakInterrupt();
        }
    }
}
