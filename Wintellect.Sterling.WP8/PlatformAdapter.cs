
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

using Wintellect.Sterling.Core;
using System.Threading;

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
    }
}
