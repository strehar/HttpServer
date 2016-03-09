using System.IO;
using DotLiquidCore.Exceptions;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
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
