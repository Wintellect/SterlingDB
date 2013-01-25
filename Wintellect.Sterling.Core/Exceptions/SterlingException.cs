using System;

namespace Wintellect.Sterling.Core.Exceptions
{
    /// <summary>
    ///     Base from which sterling exceptions derived
    /// </summary>
    public class SterlingException : Exception
    {
        public SterlingException()
        {
            
        }

        public SterlingException(string message) : base(message)
        {
            
        }

        public SterlingException(string message, Exception innerException) : base(message, innerException)
        {
            
        }
    }
}