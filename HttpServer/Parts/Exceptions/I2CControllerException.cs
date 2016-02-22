using System;

namespace Feri.MS.Parts.Exceptions
{
    internal class I2CControllerException : Exception
    {
        public I2CControllerException()
            : base("Could not find the I2C controller")
        { }
    }
}