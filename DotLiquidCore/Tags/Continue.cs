using System.IO;
using DotLiquidCore.Exceptions;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
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