
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

using Wintellect.Sterling.Core;
using System.Threading;
using System.IO;
using System.Windows.Media.Imaging;

namespace Wintellect.Sterling.WP8
{
    public class PlatformAdapter : ISterlingPlatformAdapter
    {
        public PlatformAdapter()
        {
        }

        public bool IsAssignableFrom( Type target, Type test )
        {
            return target.IsAssignableFrom( test );
        }

        public bool IsSubclassOf( Type target, Type test )
        {
            return target.IsSubclassOf( test );
        }

        public bool IsEnum( Type target )
        {
            return target.IsEnum;
        }

        public IEnumerable<FieldInfo> GetFields( Type type )
        {
            return type.GetFields();
        }

        public IEnumerable<PropertyInfo> GetProperties( Type type )
        {
            return type.GetProperties();
        }

        public MethodInfo GetGetMethod( PropertyInfo property )
        {
            return property.GetGetMethod();
        }

        public MethodInfo GetSetMethod( PropertyInfo property )
        {
            return property.GetSetMethod();
        }

        public IEnumerable<Attribute> GetCustomAttributes( Type target, Type attributeType, bool inherit )
        {
            return target.GetCustomAttributes( attributeType, inherit ).Cast<Attribute>();
        }

        public void Sleep( int milliseconds )
        {
            Thread.Sleep( milliseconds );
        }

        public Tuple<Type, Action<BinaryWriter, object>, Func<BinaryReader, object>> GetBitmapSerializer()
        {
            return Tuple.Create( typeof( WriteableBitmap ),
                     (Action<BinaryWriter, object>) ( ( bw, obj ) =>
                     {
                         var bitmap = (WriteableBitmap) obj;
                         bw.Write( bitmap.PixelWidth );
                         bw.Write( bitmap.PixelHeight );
                         var count = bitmap.Pixels.Length * sizeof( int );
                         var pixels = new byte[ count ];
                         Buffer.BlockCopy( bitmap.Pixels, 0, pixels, 0, count );
                         bw.Write( pixels, 0, pixels.Length );
                     } ),
                    (Func<BinaryReader, object>) ( br =>
                    {
                        var width = br.ReadInt32();
                        var height = br.ReadInt32();
                        var bitmap = new WriteableBitmap( width, height );
                        var count = bitmap.Pixels.Length * sizeof( int );
                        var pixels = new byte[ count ];
                        br.Read( pixels, 0, count );
                        Buffer.BlockCopy( pixels, 0, bitmap.Pixels, 0, count );
                        return bitmap;
                    } ) );
        }
    }
}
