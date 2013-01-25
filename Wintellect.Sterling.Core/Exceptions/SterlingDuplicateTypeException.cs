using System;

namespace Wintellect.Sterling.Core.Exceptions
{
    public class SterlingDuplicateTypeException : SterlingException 
    {
        public SterlingDuplicateTypeException(Type type, string databaseName) :
            base(string.Format(Exceptions.SterlingDuplicateTypeException, type.FullName, databaseName))
        {
            
        }
    }
}
