using System;
using System.Collections.Generic;
using System.IO;
using Wintellect.Sterling.Core.Exceptions;

namespace Wintellect.Sterling.Core.Database
{
    /// <summary>
    /// Helper class to resolve the types of elements stored in database tables.
    /// </summary>
    public class TableTypeResolver
    {
        #region Fields
        private static List<ISterlingTypeResolver> _typeResolvers = new List<ISterlingTypeResolver>();
        private static Dictionary<string, Type> _resolvedTypes = new Dictionary<string, Type>();
        #endregion

        #region Public Methods
        public static void RegisterTypeResolver(ISterlingTypeResolver interceptor)
        {
            if (interceptor == null)
            {
                throw new ArgumentNullException("interceptor");
            }

            if (!_typeResolvers.Contains(interceptor))
            {
                _typeResolvers.Add(interceptor);
            }
        }

        public static Type ResolveTableType(string fullTypeName)
        {
            // TODO: searching for replacement type first makes unit testing possible, but isn't nice design
            Type tableType = (ResolveCachedType(fullTypeName) ??
                ResolveReplacementType(fullTypeName) ??
                ResolveOriginalType(fullTypeName));
            
            return tableType;
        }
        #endregion

        #region Private Implementation
        private static Type ResolveCachedType(string fullTypeName)
        {
            Type result;
            _resolvedTypes.TryGetValue(fullTypeName, out result);
            return result;
        }

        private static Type ResolveOriginalType(string fullTypeName)
        {
            Type result = null;

            try
            {
                result = Type.GetType( fullTypeName, false );
                CacheResolvedType( fullTypeName, result );
            }
            //catch (TypeLoadException) { }
            //catch (FileLoadException) { }
            catch ( Exception ) { }
            return result;
        }

        private static Type ResolveReplacementType(string fullTypeName)
        {
            Type result = null;
            foreach (ISterlingTypeResolver typeResolver in _typeResolvers)
            {
                result = typeResolver.ResolveTableType(fullTypeName);
                if (result != null)
                {
                    CacheResolvedType(fullTypeName, result);
                    break;
                }
            }
            return result;
        }

        private static void CacheResolvedType(string fullTypeName, Type resolvedType)
        {
            if (resolvedType != null)
            {
                _resolvedTypes[fullTypeName] = resolvedType;
            }
        }
        #endregion
    }
}