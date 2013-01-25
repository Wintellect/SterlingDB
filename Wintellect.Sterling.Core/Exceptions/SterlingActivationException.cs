using System;

namespace Wintellect.Sterling.Core.Exceptions
{
    public class SterlingActivationException : SterlingException 
    {
        public SterlingActivationException(string operation) : base(string.Format(Exceptions.SterlingActivationException, operation))
        {
            
        }
    }
}
