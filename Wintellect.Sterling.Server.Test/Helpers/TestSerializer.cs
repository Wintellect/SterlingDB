
using System;
using System.IO;
using Wintellect.Sterling.Core;
using Wintellect.Sterling.Core.Serialization;

namespace Wintellect.Sterling.Test.Helpers
{
    public struct TestStruct
    {
        public int Value;
        public DateTime Date; 
    }

    public class TestSerializer : BaseSerializer 
    {
        /// <summary>
        ///     Return true if this serializer can handle the object
        /// </summary>
        /// <param name="targetType">The target</param>
        /// <returns>True if it can be serialized</returns>
        public override bool CanSerialize(Type targetType)
        {
            return targetType.Equals(typeof (TestStruct));
        }

        /// <summary>
        ///     Serialize the object
        /// </summary>
        /// <param name="target">The target</param>
        /// <param name="writer">The writer</param>
        public override void Serialize(object target, BinaryWriter writer)
        {
            var instance = (TestStruct) target;
            writer.Write(instance.Value);
            writer.Write(instance.Date.Ticks);
        }

        /// <summary>
        ///     Deserialize the object
        /// </summary>
        /// <param name="type">The type of the object</param>
        /// <param name="reader">A reader to deserialize from</param>
        /// <returns>The deserialized object</returns>
        public override object Deserialize(Type type, BinaryReader reader)
        {
            return new TestStruct {Value = reader.ReadInt32(), Date = new DateTime(reader.ReadInt64())};
        }
    }
}
