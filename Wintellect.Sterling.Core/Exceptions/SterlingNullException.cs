using System;

namespace Wintellect.Sterling.Core.Exceptions
{
    public class SterlingNullException : SterlingException 
    {
        public SterlingNullException(string property, Type type) : base(string.Format(Exceptions.SterlingNullException, property, type.FullName))
        {
            
        }
    }
}
