
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Wintellect.Sterling.Core
{
    public interface ISterlingPlatformAdapter
    {
        bool IsAssignableFrom( Type target, Type test );
        bool IsSubclassOf( Type target, Type test );
        bool IsEnum( Type target );
        IEnumerable<FieldInfo> GetFields( Type type );
        IEnumerable<PropertyInfo> GetProperties( Type type );
        MethodInfo GetGetMethod( PropertyInfo property );
        MethodInfo GetSetMethod( PropertyInfo property );
        IEnumerable<Attribute> GetCustomAttributes( Type target, Type attributeType, bool inherit );
        void Sleep( int milliseconds );
        Tuple<Type, Action<BinaryWriter, object>, Func<BinaryReader, object>> GetBitmapSerializer();
    }
}
