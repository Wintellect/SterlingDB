using System;

namespace Wintellect.Sterling.Core
{
    /// <summary>
    /// Implement this interface when you're application will be updated. Typenames might have changed and with this interface, you can return the correct
    /// type for a given type name. Register your resolver by calling RegisterTypeResolver on your database.
    /// </summary>
    public interface ISterlingTypeResolver
    {
        Type ResolveTableType(string fullTypeName);
    }
}