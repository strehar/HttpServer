namespace DotLiquidCore
{
#pragma warning disable CS1584 // XML comment has syntactically incorrect cref attribute
                              /// <summary>
                              /// See here for motivation: <see cref="http://wiki.github.com/tobi/liquid/using-liquid-without-rails"/>.
                              /// This allows for extra security by only giving the template access to the specific
                              /// variables you want it to have access to.
                              /// </summary>
    public interface ILiquidizable
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        object ToLiquid();
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}