using System;
using System.Collections.Generic;
using System.IO;
using Wintellect.Sterling.Core.Exceptions;

namespace Wintellect.Sterling.Core.Serialization
{
    /// <summary>
    ///     Serializes some extended objects
    /// </summary>
    internal class ExtendedSerializer : BaseSerializer
    {
        /// <summary>
        ///     Dictionary of serializers
        /// </summary>
        private readonly Dictionary<Type, Tuple<Action<BinaryWriter, object>, Func<BinaryReader, object>>> _serializers
            = new Dictionary<Type, Tuple<Action<BinaryWriter, object>, Func<BinaryReader, object>>>();

        /// <summary>
        ///     Default constructor
        /// </summary>
        public ExtendedSerializer( ISterlingPlatformAdapter platformAdapter )
        {
            // wire up the serialization pairs 
            _serializers.Add( typeof( DateTime ), new Tuple<Action<BinaryWriter, object>, Func<BinaryReader, object>>(
                                                    ( bw, obj ) =>
                                                        {
                                                            bw.Write( ( (DateTime) obj ).Ticks );
                                                            bw.Write( (int) ( (DateTime) obj ).Kind );
                                                        },
                                                    br =>
                                                        {
                                                            return new DateTime(br.ReadInt64(), (DateTimeKind) br.ReadInt32() );
                                                        } ) );


            _serializers.Add( typeof( Guid ), new Tuple<Action<BinaryWriter, object>, Func<BinaryReader, object>>(
                                                ( bw, obj ) => bw.Write( ( (Guid) obj ).ToByteArray() ),
                                                br => new Guid( br.ReadBytes( 16 ) ) ) );

            _serializers.Add( typeof( Uri ), new Tuple<Action<BinaryWriter, object>, Func<BinaryReader, object>>(
                                               ( bw, obj ) => bw.Write( ( (Uri) obj ).AbsoluteUri ),
                                               br => new Uri( br.ReadString() ) ) );

            _serializers.Add( typeof( decimal ), new Tuple<Action<BinaryWriter, object>, Func<BinaryReader, object>>(
                                                   ( bw, obj ) =>
                                                   {
                                                       var bits = decimal.GetBits( (decimal) obj );
                                                       bw.Write( bits[ 0 ] );
                                                       bw.Write( bits[ 1 ] );
                                                       bw.Write( bits[ 2 ] );
                                                       bw.Write( bits[ 3 ] );
                                                   },
                                                   br =>
                                                   {
                                                       var bits = new int[ 4 ];
                                                       bits[ 0 ] = br.ReadInt32();
                                                       bits[ 1 ] = br.ReadInt32();
                                                       bits[ 2 ] = br.ReadInt32();
                                                       bits[ 3 ] = br.ReadInt32();
                                                       return new decimal( bits );
                                                   } )
                );

            _serializers.Add( typeof( TimeSpan ), new Tuple<Action<BinaryWriter, object>, Func<BinaryReader, object>>(
                                                    ( bw, obj ) => bw.Write( ( (TimeSpan) obj ).Ticks ),
                                                    br => new TimeSpan( br.ReadInt64() ) ) );

            _serializers.Add( typeof( DateTimeOffset ),
                             new Tuple<Action<BinaryWriter, object>, Func<BinaryReader, object>>(
                                 ( bw, obj ) =>
                                 {
                                     var dto = (DateTimeOffset) obj;
                                     bw.Write( dto.Ticks );
                                     bw.Write( dto.Offset.Ticks );
                                 },
                                 br => new DateTimeOffset( br.ReadInt64(), new TimeSpan( br.ReadInt64() ) ) ) );

            var bitmapSerializer = platformAdapter.GetBitmapSerializer();

            if ( bitmapSerializer != null )
            {
                _serializers.Add( bitmapSerializer.Item1, Tuple.Create( bitmapSerializer.Item2, bitmapSerializer.Item3 ) );
            }
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