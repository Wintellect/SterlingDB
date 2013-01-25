using System;

namespace Wintellect.Sterling.Core.Exceptions
{
    public class SterlingTableNotFoundException : SterlingException
    {
        public SterlingTableNotFoundException(Type tableType, string databaseName)
            : base(string.Format(Exceptions.SterlingTableNotFoundException, tableType.FullName, databaseName))
        {
        }

        public SterlingTableNotFoundException(string typeName, string databaseName)
            : base(string.Format(Exceptions.SterlingTableNotFoundException, typeName, databaseName))
        {
        }
    }
}