using System;
using System.IO;

namespace Wintellect.Sterling.Core.Serialization
{
    /// <summary>
    ///     Serializer for sterling
    /// </summary>
    public interface ISterlingSerializer
    {
        /// <summary>
        ///     Return true if this serializer can handle the object
        /// </summary>
        /// <param name="targetType">The target</param>
        /// <returns>True if it can be serialized</returns>
        bool CanSerialize(Type targetType);

        /// <summary>
        ///     Serialize the object
        /// </summary>
        /// <param name="target">The target</param>
        /// <param name="writer">The writer</param>
        void Serialize(object target, BinaryWriter writer);

        /// <summary>
        ///     Deserialize the object
        /// </summary>
        /// <param name="type">The type of the object</param>
        /// <param name="reader">A reader to deserialize from</param>
        /// <returns>The deserialized object</returns>
        object Deserialize(Type type, BinaryReader reader);

        /// <summary>
        ///     Return true
        /// </summary>
        /// <typeparam name="T">The type</typeparam>
        /// <returns>True if it can handle it</returns>
        bool CanSerialize<T>();        

        /// <summary>
        ///     Typed deserialization
        /// </summary>
        /// <typeparam name="T">The type</typeparam>
        /// <param name="reader">The reader</param>
        /// <returns>The type</returns>
        T Deserialize<T>(BinaryReader reader);        
    }
}