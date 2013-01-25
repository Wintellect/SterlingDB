using System;

namespace Wintellect.Sterling.Core
{
    /// <summary>
    /// Implement this interface when you have renamed a property on one of your serialized classes. 
    /// Register it by calling RegisterPropertyConverter on your database.
    /// </summary>
    public interface ISterlingPropertyConverter
    {
        /// <summary>
        /// Returns the type this converter can convert properties for.
        /// </summary>
        /// <returns>A System.Type.</returns>
        Type IsConverterFor();

        /// <summary>
        /// Sets the new property of the given instance to the given value.
        /// </summary>
        /// <param name="instance">The instance</param>
        /// <param name="oldPropertyName">The old property name</param>
        /// <param name="value">The value</param>
        void SetValue(object instance, string oldPropertyName, object value);
    }
}