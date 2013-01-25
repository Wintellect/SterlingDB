using System;

namespace Wintellect.Sterling.Core.Exceptions
{
    public class SterlingDuplicateDatabaseException : SterlingException
    {
        public SterlingDuplicateDatabaseException(ISterlingDatabaseInstance instance) : base(
            string.Format(Exceptions.SterlingDuplicateDatabaseException, instance.GetType().FullName))
        {
        }

        public SterlingDuplicateDatabaseException(Type type)
            : base(
                string.Format(Exceptions.SterlingDuplicateDatabaseException, type.FullName))
        {
        }
    }
}