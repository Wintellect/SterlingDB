using System;

namespace Wintellect.Sterling.Core.Serialization
{    
    /// <summary>
    ///     Cache for serialization of properties
    /// </summary>
    internal class SerializationCache
    {
        public SerializationCache(Type propertyType, string propertyName,
            Action<object, object> setter, Func<object, object> getter)
        {
            PropType = propertyType;          
            SetMethod = setter;
            GetMethod = getter;
            PropertyName = propertyName;
        }

        /// <summary>
        ///     Property type
        /// </summary>
        public Type PropType { get; private set; }

        /// <summary>
        ///     The setter for the type
        /// </summary>
        public Action<object, object> SetMethod { get; private set; }

        /// <summary>
        ///     The getter for the type
        /// </summary>
        public Func<object, object> GetMethod { get; private set; }

        /// <summary>
        ///     The name of the property.
        /// </summary>
        public string PropertyName { get; private set; }
    }
}
