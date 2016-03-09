#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace DotLiquidCore.Exceptions
{
    public class InterruptException : LiquidException
    {
        public InterruptException(string message) : base(message)
        {
        }
    }

    public class BreakInterrupt : InterruptException
    {
        public BreakInterrupt()
            : base("Misplaced 'break' statement")
        {
        }
    }

    public class ContinueInterrupt : InterruptException
    {
        public ContinueInterrupt()
            : base("Misplaced 'continue' statement")
        {
        }
    }
}
