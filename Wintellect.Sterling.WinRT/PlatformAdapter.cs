
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

using Wintellect.Sterling.Core;
using System.Threading;

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
    }
}
