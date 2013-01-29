using System;
using System.Collections;
using System.IO;
using System.Linq;
using Wintellect.Sterling.Core.Database;
using Wintellect.Sterling.Core.Exceptions;
using System.Collections.Generic;

namespace Wintellect.Sterling.Core.Serialization
{
    /// <summary>
    ///     Wraps nodes for passing directly into the Save pass of the Serialization Helper
    ///     Basically just hosts another object so that the helper can recursively navigate properties
    ///     Useful in external serializers that want to re-enter the stream using the helper methods
    /// </summary>
    public class SerializationNode
    {
        public object Value { get; set; }

        public static SerializationNode WrapForSerialization(object obj)
        {
            return new SerializationNode {Value = obj};
        }

        public T UnwrapForDeserialization<T>()
        {
            return (T) Value;
        }
    }

    /// <summary>
    ///     This class assists with the serialization and de-serialization of objects
    /// </summary>
    /// <remarks>
    ///     This is where the heavy lifting is done, and likely where most of the tweaks make sense
    /// </remarks>
    public class SerializationHelper
    {
        // a few constants to serialize null values to the stream
        private const ushort NULL = 0;
        private const ushort NOTNULL = 1;
        private const string NULL_DISPLAY = "[NULL]";
        private const string NOTNULL_DISPLAY = "[NOT NULL]";
        private const string PROPERTY_VALUE_SEPARATOR = ":";
        private const string END_OF_INSTANCE = "[END_OF_INSTANCE]";
        
        /// <summary>
        ///     The import cache, stores what properties are available and how to access them. Each type has a matching dictionary with the property names as keys
        ///     and the SerializationCache objects as values (provides access to the properties).
        /// </summary>
        private readonly
            Dictionary<Type, Dictionary<string, SerializationCache>>
            _propertyCache =
                new Dictionary
                    <Type, Dictionary<string, SerializationCache>>();

        private readonly Dictionary<string,Type> _typeRef = new Dictionary<string, Type>();

        private readonly ISterlingDatabaseInstance _database;
        private readonly ISterlingSerializer _serializer;
        private readonly LogManager _logManager;
        private readonly Func<string, int> _typeResolver = s => 1;
        private readonly Func<int, string> _typeIndexer = i => string.Empty;

        /// <summary>
        ///     Cache the properties for a type so we don't reflect every time
        /// </summary>
        /// <param name="type">The type to manage</param>
        private void _CacheProperties(Type type)
        {
            lock (((ICollection)_propertyCache).SyncRoot)
            {
                // fast "out" if already exists
                if (_propertyCache.ContainsKey(type)) return;

                _propertyCache.Add(type, new Dictionary<string, SerializationCache>());

                var isList = PlatformAdapter.Instance.IsAssignableFrom( typeof( IList ), type );
                var isDictionary = PlatformAdapter.Instance.IsAssignableFrom( typeof( IDictionary ), type );
                var isArray = PlatformAdapter.Instance.IsAssignableFrom( typeof( Array ), type );

                var noDerived = isList || isDictionary || isArray; 

                // first fields
                var fields = from f in PlatformAdapter.Instance.GetFields( type )
                             where                              
                             !f.IsStatic &&
                             !f.IsLiteral &&
                             !f.IsIgnored(_database.IgnoreAttribute) && !f.FieldType.IsIgnored(_database.IgnoreAttribute)
                             select new PropertyOrField(f);

                var properties = from p in PlatformAdapter.Instance.GetProperties( type )
                                 where          
                                 ((noDerived && p.DeclaringType.Equals(type) || !noDerived)) &&
                                 p.CanRead && p.CanWrite &&
                                 PlatformAdapter.Instance.GetGetMethod( p ) != null && PlatformAdapter.Instance.GetSetMethod( p ) != null
                                       && !p.IsIgnored(_database.IgnoreAttribute) && !p.PropertyType.IsIgnored(_database.IgnoreAttribute)
                                 select new PropertyOrField(p);                                 

                foreach (var p in properties.Concat(fields))
                {                    
                    var propType = p.PfType;   
                 
                    // eagerly add to the type master
                    _typeResolver(propType.AssemblyQualifiedName);

                    var p1 = p;

                    _propertyCache[type].Add(p1.Name, new SerializationCache(propType, p1.Name, (parent, property) => p1.Setter(parent, property), p1.GetValue));
                }                
            }
        }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="database">Database this is a helper for</param>
        /// <param name="serializer">The serializer</param>
        /// <param name="logManager">The logger</param>
        /// <param name="typeResolver"></param>
        /// <param name="typeIndexer"></param>
        /// <param name="platform"></param>
        public SerializationHelper(ISterlingDatabaseInstance database, ISterlingSerializer serializer,
                                   LogManager logManager, Func<string,int> typeResolver, Func<int,string> typeIndexer)
        {
            _database = database;
            _serializer = serializer;
            _logManager = logManager;
            _typeResolver = typeResolver;
            _typeIndexer = typeIndexer;
        }

        /// <summary>
        ///     External entry point for save, used by serializers
        ///     or other methods that simply want to intercept the
        ///     serialization stream. Wraps the object in a node and
        ///     then parses recursively
        /// </summary>
        /// <remarks>
        ///     See the custom serializer test for an example
        /// </remarks>
        /// <param name="obj">The instance to save</param>
        /// <param name="bw">The writer to inject to</param>
        public void Save(object obj, BinaryWriter bw)
        {
            var node = SerializationNode.WrapForSerialization(obj);
            Save(typeof(SerializationNode), node, bw, new CycleCache(),true);
        }

        /// <summary>
        ///     Recursive save operation
        /// </summary>
        /// <param name="type">The type to save (passed to support NULL)</param>
        /// <param name="instance">The instance to type</param>
        /// <param name="bw">The writer to save it to</param>
        /// <param name="cache">Cycle cache</param>
        /// <param name="saveTypeExplicit">False if the calling method has already stored the object type, otherwise true</param>
        public void Save(Type type, object instance, BinaryWriter bw, CycleCache cache, bool saveTypeExplicit)
        {
            _logManager.Log(SterlingLogLevel.Verbose, string.Format("Sterling is serializing type {0}", type.FullName),
                            null);
            
            // need to indicate to the stream whether or not this is null
            var nullFlag = instance == null;

            _SerializeNull(bw, nullFlag);

            if (nullFlag) return;

            // build the cache for reflection
            if (!_propertyCache.ContainsKey(type))
            {
                //_CacheProperties(type);
                _CacheProperties(type);
            }

            if ( PlatformAdapter.Instance.IsAssignableFrom( typeof( Array ), type ) )
            {
                _SaveArray(bw, cache, instance as Array);
            }
            else if ( PlatformAdapter.Instance.IsAssignableFrom( typeof( IList ), type ) )
            {
                _SaveList(instance as IList, bw, cache);
            }
            else if ( PlatformAdapter.Instance.IsAssignableFrom( typeof( IDictionary ), type ) )
            {
                _SaveDictionary(instance as IDictionary, bw, cache);              
            }
            else if (saveTypeExplicit)
            {
                bw.Write(_typeResolver(type.AssemblyQualifiedName));
            }

            // now iterate the serializable properties - create a copy to avoid multi-threaded conflicts
            foreach (var p in new Dictionary<string, SerializationCache>(_propertyCache[type]))
            {
                var serializationCache = p.Value;
                var value = serializationCache.GetMethod(instance);
                _InnerSave(value == null ? serializationCache.PropType : value.GetType(), serializationCache.PropertyName, value, bw, cache);
            }

            // indicate the end of the instance was reached.
            bw.Write(END_OF_INSTANCE);
        }

        private void _SaveList(IList list, BinaryWriter bw, CycleCache cache)
        {
            _SerializeNull(bw, list == null);

            if (list == null)
            {
                return;
            }

            bw.Write(list.Count);
            foreach(var item in list)
            {
                _InnerSave(item == null ? typeof(string) : item.GetType(), "ListItem", item, bw, cache);
            }
        }

        private void _SaveDictionary(IDictionary dictionary, BinaryWriter bw, CycleCache cache)
        {
            _SerializeNull(bw, dictionary == null);

            if (dictionary == null)
            {
                return;
            }

            bw.Write(dictionary.Count);
            foreach (var item in dictionary.Keys)
            {
                _InnerSave(item.GetType(), "DictionaryItemKey", item, bw, cache);
                _InnerSave(dictionary[item] == null ? typeof(string) : dictionary[item].GetType(), "DictionaryItemValue", dictionary[item], bw, cache);
            }
        }

        private void _SaveArray(BinaryWriter bw, CycleCache cache, Array array)
        {
            _SerializeNull(bw, array == null);

            if (array == null)
            {
                return;
            }

            bw.Write(array.Length);
            foreach (var item in array)
            {
                _InnerSave(item == null ? typeof(string) : item.GetType(), "ArrayItem", item, bw, cache);
            }
        }

        private void _InnerSave(Type type, string propertyName, object instance, BinaryWriter bw,  CycleCache cache)
        {                                    
            if (_database.IsRegistered(type))
            {
                // foreign table - write if it is null or not, and if not null, write the key
                // then serialize it separately
                _SerializeClass(type, propertyName, instance, bw, cache);
                return;
            }
            
            if (_serializer.CanSerialize(type))
            {
                _SerializeProperty(type, propertyName, instance, bw);
                return;
            }

            if (instance is Array)
            {
                bw.Write(propertyName + PROPERTY_VALUE_SEPARATOR);
                bw.Write(_typeResolver(type.AssemblyQualifiedName));
                _SaveArray(bw, cache, instance as Array);
                return;
            }

            if ( PlatformAdapter.Instance.IsAssignableFrom( typeof( IList ), type ) )
            {
                bw.Write(propertyName + PROPERTY_VALUE_SEPARATOR);
                bw.Write(_typeResolver(type.AssemblyQualifiedName));
                _SaveList(instance as IList, bw, cache);                                
                return;
            }

            if ( PlatformAdapter.Instance.IsAssignableFrom( typeof( IDictionary ), type ) )
            {
                bw.Write(propertyName + PROPERTY_VALUE_SEPARATOR);
                bw.Write(_typeResolver(type.AssemblyQualifiedName));
                _SaveDictionary(instance as IDictionary, bw, cache);
                return;
            }           
                       
            bw.Write(propertyName + PROPERTY_VALUE_SEPARATOR);
            bw.Write(_typeResolver(type.AssemblyQualifiedName));
            Save(type, instance, bw, cache,false);
        }

        /// <summary>
        ///     Serializes a property
        /// </summary>
        /// <param name="type">The parent type</param>
        /// <param name="propertyName">The property name</param>
        /// <param name="propertyValue">The property value</param>
        /// <param name="bw">The writer</param>
        private void _SerializeProperty(Type type, string propertyName, object propertyValue, BinaryWriter bw)
        {
            bw.Write(propertyName + PROPERTY_VALUE_SEPARATOR);
            bw.Write(_typeResolver(type.AssemblyQualifiedName));

            var isNull = propertyValue == null;
            _SerializeNull(bw, isNull);

            if (isNull)
            {
                return;
            }

            _serializer.Serialize(propertyValue, bw);
        }

        /// <summary>
        ///     Serialize a class
        /// </summary>
        /// <param name="type">The type</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="foreignTable">The referenced type</param>
        /// <param name="bw">The writer</param>
        /// <param name="cache">Cycle cache</param>
        private void _SerializeClass(Type type, string propertyName, object foreignTable, BinaryWriter bw, CycleCache cache)
        {
            bw.Write(propertyName + PROPERTY_VALUE_SEPARATOR);
            bw.Write(_typeResolver(type.AssemblyQualifiedName));

            // serialize to the stream if the foreign key is nulled
            _SerializeNull(bw, foreignTable == null);

            if (foreignTable == null) return;

            var task = _database.SaveAsync(foreignTable.GetType(), foreignTable.GetType(),foreignTable, cache);

            //TODO: fix this
            if( task.Wait( TimeSpan.FromSeconds( 10 ) ) == false )
            {
                throw new TimeoutException( "Foreign key save operation failed to complete in 10 seconds." );
            }

            var foreignKey = task.Result;
            
            // need to be able to serialize the key 
            if (!_serializer.CanSerialize(foreignKey.GetType()))
            {
                var exception = new SterlingSerializerException(_serializer, foreignKey.GetType());
                _logManager.Log(SterlingLogLevel.Error, exception.Message, exception);
                throw exception;
            }

            _logManager.Log(SterlingLogLevel.Verbose,
                            string.Format(
                                "Sterling is saving foreign key of type {0} with value {1} for parent {2}",
                                foreignKey.GetType().FullName, foreignKey, type.FullName), null);

            _serializer.Serialize(foreignKey, bw);            
        }

        /// <summary>
        ///     Helper load for serializers - this is not part of the internal recursion
        ///     Basically allows a node to be saved in a wrapper, and this is the entry
        ///     to unwrap it
        /// </summary>
        /// <typeparam name="T">Type of the object to laod</typeparam>
        /// <param name="br">The reader stream being accessed</param>
        /// <returns>The unwrapped object instance</returns>
        public T Load<T>(BinaryReader br)
        {
            var node = Load(typeof (SerializationNode), null, br, new CycleCache()) as SerializationNode;
            if (node != null)
            {
                return node.UnwrapForDeserialization<T>();
            }
            return default(T);
        }

        /// <summary>
        ///     Recursive load operation
        /// </summary>
        /// <param name="type">The type to save (passed to support NULL)</param>
        /// <param name="key">The associated key (for cycle detection)</param>
        /// <param name="br">The reader</param>
        /// <param name="cache">Cycle cache</param>
        public object Load(Type type, object key, BinaryReader br, CycleCache cache)
        {
            _logManager.Log(SterlingLogLevel.Verbose,
                            string.Format("Sterling is de-serializing type {0}", type.FullName), null);

            if (_DeserializeNull(br))
            {
                return null;
            }
            
            // make a template
            var instance = Activator.CreateInstance(type);

            // build the reflection cache);
            if (!_propertyCache.ContainsKey(type))
            {
                //_CacheProperties(type);
                _CacheProperties(type);
            }

            if (instance is Array)
            {
                // push to the stack
                cache.Add(type, instance, key);
                var isNull = _DeserializeNull(br);

                if (!isNull)
                {
                    var count = br.ReadInt32();
                    for (var x = 0; x < count; x++)
                    {
                        ((Array)instance).SetValue(_Deserialize(br, cache).Value, x);
                    }
                }
            }
            else if (instance is IList)
            {
                // push to the stack
                cache.Add(type, instance, key);
                var isNull = _DeserializeNull(br);
                if (!isNull)
                {
                    _LoadList(br, cache, instance as IList);
                }
            }

            else if (instance is IDictionary)
            {
                // push to the stack
                cache.Add(type, instance, key);
                var isNull = _DeserializeNull(br);
                if (!isNull)
                {
                    _LoadDictionary(br, cache, instance as IDictionary);
                }
            }
            else
            {
                type = Type.GetType(_typeIndexer(br.ReadInt32()));
                if (instance.GetType() != type)
                {
                    instance = Activator.CreateInstance(type);
                }

                // push to the stack
                cache.Add(type, instance, key);

                // build the reflection cache);
                if (!_propertyCache.ContainsKey(type))
                {
                    //_CacheProperties(type);
                    _CacheProperties(type);
                }
            }

            // now iterate until the end of the file was reached
            _IteratePropertiesUntilEndOfFileIsReached(br, cache, type, instance);
            return instance;
        }

        /// <summary>
        /// Deserializes the next part in the BinaryReader and returns a KeyValuePair containing the property name as key van the deserialized object as value.
        /// </summary>
        /// <param name="br">The binary reader</param>
        /// <param name="cache">The cycle cache</param>
        /// <returns>A KeyValuePair containing the property name and the property value.</returns>
        private KeyValuePair<string, object> _Deserialize(BinaryReader br, CycleCache cache)
        {
            var propertyName = br.ReadString().Replace(PROPERTY_VALUE_SEPARATOR, string.Empty);
            if (propertyName == END_OF_INSTANCE)
            {
                return new KeyValuePair<string, object>(END_OF_INSTANCE, END_OF_INSTANCE);
            }

            var typeName = _typeIndexer(br.ReadInt32());

            if (_DeserializeNull(br))
            {
                return new KeyValuePair<string, object>(propertyName, null);
            }

            Type typeResolved = null;

            if (!_typeRef.TryGetValue(typeName, out typeResolved))
            {
                typeResolved = Type.GetType(typeName);

                lock(((ICollection)_typeRef).SyncRoot)
                {
                    if (!_typeRef.ContainsKey(typeName))
                    {
                        _typeRef.Add(typeName, typeResolved);
                    }
                }
            }            

            if (_database.IsRegistered(typeResolved))
            {
                var keyType = _database.GetKeyType(typeResolved);
                var key = _serializer.Deserialize(keyType, br);

                var cached = cache.CheckKey(keyType, key);
                if (cached != null)
                {
                    return new KeyValuePair<string, object>(propertyName, cached);
                }

                var task = _database.LoadAsync(typeResolved, key, cache);

                //TODO: fix this
                if ( task.Wait( TimeSpan.FromSeconds( 10 ) ) == false )
                {
                    throw new TimeoutException( "Foreign key load operation failed to complete in 10 seconds." );
                }

                cached = task.Result;

                cache.Add(typeResolved, cached, key);
                
                return new KeyValuePair<string, object>(propertyName, cached);
            }

            if (_serializer.CanSerialize(typeResolved))
            {
                return new KeyValuePair<string, object>(propertyName, _serializer.Deserialize(typeResolved, br));
            }


            if ( PlatformAdapter.Instance.IsAssignableFrom( typeof( Array ), typeResolved ) )
            {                
                var count = br.ReadInt32();
                var array = Array.CreateInstance(typeResolved.GetElementType(), count);
                for (var x = 0; x < count; x++)
                {
                    array.SetValue(_Deserialize(br, cache).Value, x);
                }

                return new KeyValuePair<string, object>(propertyName, array);
            }

            if ( PlatformAdapter.Instance.IsAssignableFrom( typeof( IList ), typeResolved ) )
            {
                var list = Activator.CreateInstance(typeResolved) as IList;
                return new KeyValuePair<string, object>(propertyName, _LoadList(br, cache, list));              
            }

            if ( PlatformAdapter.Instance.IsAssignableFrom( typeof( IDictionary ), typeResolved ) )
            {
                var dictionary = Activator.CreateInstance(typeResolved) as IDictionary;
                return new KeyValuePair<string, object>(propertyName, _LoadDictionary(br, cache, dictionary));
            }            

            var instance = Activator.CreateInstance(typeResolved);

            // build the reflection cache);
            if (!_propertyCache.ContainsKey(typeResolved))
            {
                //_CacheProperties(type);
                _CacheProperties(typeResolved);
            }

            // now iterate until the end of the file was reached
            _IteratePropertiesUntilEndOfFileIsReached(br, cache, typeResolved, instance);
            return new KeyValuePair<string, object>(propertyName, instance);
        }

        private void _IteratePropertiesUntilEndOfFileIsReached(BinaryReader br, CycleCache cache, Type typeResolved, object instance)
        {
            KeyValuePair<string, object> propertyPair = _Deserialize(br, cache);
            while (propertyPair.Key != END_OF_INSTANCE)
            {
                SerializationCache serializationCache;
                if (_propertyCache[typeResolved].TryGetValue(propertyPair.Key, out serializationCache))
                {
                    serializationCache.SetMethod(instance, propertyPair.Value);
                }
                else
                {
                    // unknown property, see if it should be converted or ignored
                    ISterlingPropertyConverter propertyConverter;
                    if (_database.TryGetPropertyConverter(typeResolved, out propertyConverter))
                    {
                        propertyConverter.SetValue(instance, propertyPair.Key, propertyPair.Value);
                    }
                }

                propertyPair = _Deserialize(br, cache);
            }
        }
        
        private IDictionary _LoadDictionary(BinaryReader br, CycleCache cache, IDictionary dictionary)
        {            
            var count = br.ReadInt32();
            for (var x = 0; x < count; x++)
            {
                dictionary.Add(_Deserialize(br, cache).Value, _Deserialize(br, cache).Value);
            }
            return dictionary;
        }

        private IList _LoadList(BinaryReader br, CycleCache cache, IList list)
        {
            var count = br.ReadInt32();
            for (var x = 0; x < count; x++)
            {
                list.Add(_Deserialize(br, cache).Value);
            }
            return list;
        }

        private void _SerializeNull(BinaryWriter bw, bool isNull)
        {
            bw.Write(isNull ? NULL : NOTNULL);
            _logManager.Log(SterlingLogLevel.Verbose, string.Format("{0}", isNull ? NULL_DISPLAY : NOTNULL_DISPLAY), null);
        }    

        private bool _DeserializeNull(BinaryReader br)
        {
            var nullFlag = br.ReadUInt16();
            var isNull = nullFlag == NULL;
            _logManager.Log(SterlingLogLevel.Verbose, string.Format("{0}", isNull ? NULL_DISPLAY : NOTNULL_DISPLAY),null);
            return isNull;
        }
    }
}