
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;

using Wintellect.Sterling.Core;
using System.Threading;
using System.IO;
using Windows.UI.Xaml.Media.Imaging;

namespace Wintellect.Sterling.WinRT
{
    public class PlatformAdapter : ISterlingPlatformAdapter
    {
        public PlatformAdapter()
        {
        }

        public bool IsAssignableFrom( Type target, Type test )
        {
            return target.GetTypeInfo().IsAssignableFrom( test.GetTypeInfo() );
        }

        public bool IsSubclassOf( Type target, Type test )
        {
            return target.GetTypeInfo().IsSubclassOf( test );
        }

        public bool IsEnum( Type target )
        {
            return target.GetTypeInfo().IsEnum;
        }

        public IEnumerable<FieldInfo> GetFields( Type type )
        {
            return type.GetRuntimeFields().Where( f => f.IsPublic );
        }

        public IEnumerable<PropertyInfo> GetProperties( Type type )
        {
            return type.GetRuntimeProperties().Where( p => p.CanRead && p.CanWrite && p.SetMethod.IsPublic && p.GetMethod.IsPublic );
        }

        public MethodInfo GetGetMethod( PropertyInfo property )
        {
            return property.GetMethod;
        }

        public MethodInfo GetSetMethod( PropertyInfo property )
        {
            return property.SetMethod;
        }

        public IEnumerable<Attribute> GetCustomAttributes( Type target, Type attributeType, bool inherit )
        {
            return target.GetTypeInfo().GetCustomAttributes( attributeType, inherit );
        }

        public void Sleep( int milliseconds )
        {
            new ManualResetEvent( false ).WaitOne( milliseconds );
        }

        public Tuple<Type, Action<BinaryWriter, object>, Func<BinaryReader, object>> GetBitmapSerializer()
        {
            return Tuple.Create( typeof( WriteableBitmap ),
                     (Action<BinaryWriter, object>) ( ( bw, obj ) =>
                     {
                         var bitmap = (WriteableBitmap) obj;
                         bw.Write( bitmap.PixelWidth );
                         bw.Write( bitmap.PixelHeight );
                         var pixels = bitmap.PixelBuffer.ToArray();
                         bw.Write( pixels.Length );
                         bw.Write( pixels, 0, pixels.Length );
                     } ),
                     (Func<BinaryReader, object>) ( br =>
                     {
                         var width = br.ReadInt32();
                         var height = br.ReadInt32();
                         var count = br.ReadInt32();
                         var bitmap = new WriteableBitmap( width, height );
                         var pixels = new byte[ count ];
                         br.Read( pixels, 0, count );
                         pixels.CopyTo( bitmap.PixelBuffer );
                         return bitmap;
                     } ) );

        }
    }
}
