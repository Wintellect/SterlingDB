using System;

namespace Wintellect.Sterling.Core.Exceptions
{
    public class SterlingIndexNotFoundException : SterlingException 
    {
        public SterlingIndexNotFoundException(string indexName, Type type) : 
            base(string.Format(Exceptions.SterlingIndexNotFoundException, indexName, type.FullName))
        {
            
        }
    }
}
