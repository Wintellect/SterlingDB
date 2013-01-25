using System;

namespace Wintellect.Sterling.Core.Exceptions
{
    public class SterlingIsolatedStorageException : SterlingException
    {
        public SterlingIsolatedStorageException(Exception ex) : base(string.Format(Exceptions.SterlingIsolatedStorageException,ex.Message), ex)
        {
            
        }
    }
}
