using System;
using System.Collections.Generic;
using System.IO;
using Wintellect.Sterling.Core.Exceptions;

namespace Wintellect.Sterling.Core.Serialization
{
    /// <summary>
    ///     Default serializer handles the instances the writer is overloaded for by default
    /// </summary>
    public class DefaultSerializer : BaseSerializer 
    {
        /// <summary>
        ///     Dictionary of serializers
        /// </summary>
        private readonly Dictionary<Type, Tuple<Action<BinaryWriter,object>,Func<BinaryReader,object>>> _serializers
            = new Dictionary<Type, Tuple<Action<BinaryWriter, object>, Func<BinaryReader, object>>>();

        /// <summary>
        ///     Default constructor
        /// </summary>
        public DefaultSerializer()
        {
            // wire up the serialization pairs 
            _serializers.Add( typeof(bool), new Tuple<Action<BinaryWriter, object>, Func<BinaryReader, object>>(
                (bw,obj) => bw.Write((bool)obj),
                br => br.ReadBoolean()));

            _serializers.Add(typeof(byte), new Tuple<Action<BinaryWriter, object>, Func<BinaryReader, object>>(
                (bw, obj) => bw.Write((byte)obj),
                br => br.ReadByte()));

            _serializers.Add(typeof(byte[]), new Tuple<Action<BinaryWriter, object>, Func<BinaryReader, object>>(
               (bw, obj) => {
                   bw.Write(((byte[])obj).Length);
                   bw.Write((byte[])obj);
               },
               br => br.ReadBytes(br.ReadInt32())));

            _serializers.Add(typeof(char), new Tuple<Action<BinaryWriter, object>, Func<BinaryReader, object>>(
               (bw, obj) => bw.Write((char)obj),
               br => br.ReadChar()));

            _serializers.Add(typeof(char[]), new Tuple<Action<BinaryWriter, object>, Func<BinaryReader, object>>(
              (bw, obj) =>
              {
                  bw.Write(((char[])obj).Length);
                  bw.Write((char[])obj);
              },
              br => br.ReadChars(br.ReadInt32())));

            _serializers.Add(typeof(double), new Tuple<Action<BinaryWriter, object>, Func<BinaryReader, object>>(
               (bw, obj) => bw.Write((double)obj),
               br => br.ReadDouble()));

            _serializers.Add(typeof(float), new Tuple<Action<BinaryWriter, object>, Func<BinaryReader, object>>(
               (bw, obj) => bw.Write((float)obj),
               br => br.ReadSingle()));

            _serializers.Add(typeof(int), new Tuple<Action<BinaryWriter, object>, Func<BinaryReader, object>>(
               (bw, obj) => bw.Write((int)obj),
               br => br.ReadInt32()));

            _serializers.Add(typeof(long), new Tuple<Action<BinaryWriter, object>, Func<BinaryReader, object>>(
               (bw, obj) => bw.Write((long)obj),
               br => br.ReadInt64()));

            _serializers.Add(typeof(sbyte), new Tuple<Action<BinaryWriter, object>, Func<BinaryReader, object>>(
               (bw, obj) => bw.Write((sbyte)obj),
               br => br.ReadSByte()));

            _serializers.Add(typeof(short), new Tuple<Action<BinaryWriter, object>, Func<BinaryReader, object>>(
               (bw, obj) => bw.Write((short)obj),
               br => br.ReadInt16()));

            _serializers.Add(typeof(string), new Tuple<Action<BinaryWriter, object>, Func<BinaryReader, object>>(
               (bw, obj) => bw.Write((string)obj),
               br => br.ReadString()));

            _serializers.Add(typeof(uint), new Tuple<Action<BinaryWriter, object>, Func<BinaryReader, object>>(
               (bw, obj) => bw.Write((uint)obj),
               br => br.ReadUInt32()));

            _serializers.Add(typeof(ulong), new Tuple<Action<BinaryWriter, object>, Func<BinaryReader, object>>(
               (bw, obj) => bw.Write((ulong)obj),
               br => br.ReadUInt64()));

            _serializers.Add(typeof(ushort), new Tuple<Action<BinaryWriter, object>, Func<BinaryReader, object>>(
               (bw, obj) => bw.Write((ushort)obj),
               br => br.ReadUInt16()));
        }
        
        /// <summary>
        ///     Return true if this serializer can handle the object
        /// </summary>
        /// <param name="type">The target type</param>
        /// <returns>True if it can be serialized</returns>
        public override bool CanSerialize(Type type)
        {
            return _serializers.ContainsKey(type);
        }

        /// <summary>
        ///     Serialize the object
        /// </summary>
        /// <param name="target">The target</param>
        /// <param name="writer">The writer</param>
        public override void Serialize(object target, BinaryWriter writer)
        {
            if (!CanSerialize(target.GetType()))
            {
                throw new SterlingSerializerException(this, target.GetType());
            }
            _serializers[target.GetType()].Item1(writer, target);
        }

        /// <summary>
        ///     Deserialize the object
        /// </summary>
        /// <param name="type">The type of the object</param>
        /// <param name="reader">A reader to deserialize from</param>
        /// <returns>The deserialized object</returns>
        public override object Deserialize(Type type, BinaryReader reader)
        {
            if (!CanSerialize(type))
            {
                throw new SterlingSerializerException(this, type);
            }
            return _serializers[type].Item2(reader);
        }        
    }
}
