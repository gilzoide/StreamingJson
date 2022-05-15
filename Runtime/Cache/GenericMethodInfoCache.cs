using System;
using System.Collections.Generic;
using System.Reflection;

namespace Gilzoide.StreamingJson.Cache
{
    public class GenericMethodInfoCache
    {
        private readonly MethodInfo _method;
        private readonly Dictionary<Type, MethodInfo> _cache = new Dictionary<Type, MethodInfo>();

        public GenericMethodInfoCache(MethodInfo method)
        {
            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }
            _method = method;
        }

        public GenericMethodInfoCache(Type type, string methodName) : this(type.GetMethod(methodName)) {}

        public MethodInfo Get(Type type)
        {
            if (_cache.TryGetValue(type, out MethodInfo method))
            {
                return method;
            }

            method = _method.MakeGenericMethod(type);
            return _cache[type] = method;
        }
    }
}