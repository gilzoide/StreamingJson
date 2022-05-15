using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Serialization;

namespace Gilzoide.StreamingJson.Cache
{
    public class SerializedFieldsCache
    {
        private readonly Dictionary<Type, Dictionary<string, FieldInfo>> _cache =
            new Dictionary<Type, Dictionary<string, FieldInfo>>();

        public Dictionary<string, FieldInfo> Get(Type type)
        {
            if (_cache.TryGetValue(type, out Dictionary<string, FieldInfo> fields))
            {
                return fields;
            }

            fields = FindSerializedFields(type);
            return _cache[type] = fields;
        }

        public static Dictionary<string, FieldInfo> FindSerializedFields(Type type)
        {
            var serializedFields = new Dictionary<string, FieldInfo>();
            
            foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (field.IsDefined(typeof(NonSerializedAttribute))
                    || !field.IsPublic && !field.IsDefined(typeof(SerializeField)))
                {
                    continue;
                }

                serializedFields[field.Name] = field;
                    
                var formerlySerializedAs = field.GetCustomAttribute<FormerlySerializedAsAttribute>();
                if (formerlySerializedAs != null)
                {
                    serializedFields[formerlySerializedAs.oldName] = field;
                }
            }

            return serializedFields;
        }
    }
}