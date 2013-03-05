
using System;
using Wintellect.Sterling.Core.Exceptions;

namespace Wintellect.Sterling.Server.Azure.TableStorage
{
    public class SterlingFileSystemException : SterlingException 
    {
        public SterlingFileSystemException(Exception ex) : base(string.Format("An exception occurred accessing the file system: {0}", ex), ex)
        {
            
        }
    }
}