using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Gilzoide.StreamingJson.Cache;

namespace Gilzoide.StreamingJson
{
    public class JsonParser
    {
        private delegate bool ValueParser<T>(JsonParser scanner, out T value);

        private static readonly IReadOnlyDictionary<Type, ValueParser<object>> _primitiveParsers =
            new Dictionary<Type, ValueParser<object>>
            {
                [typeof(bool)] = (JsonParser parser, out object obj) =>
                {
                    bool success = parser.Parse(out bool value);
                    obj = value;
                    return success;
                },
                [typeof(int)] = (JsonParser parser, out object obj) =>
                {
                    bool success = parser.Parse(out int value);
                    obj = value;
                    return success;
                },
                [typeof(long)] = (JsonParser parser, out object obj) =>
                {
                    bool success = parser.Parse(out long value);
                    obj = value;
                    return success;
                },
                [typeof(float)] = (JsonParser parser, out object obj) =>
                {
                    bool success = parser.Parse(out float value);
                    obj = value;
                    return success;
                },
                [typeof(double)] = (JsonParser parser, out object obj) =>
                {
                    bool success = parser.Parse(out double value);
                    obj = value;
                    return success;
                },
                [typeof(string)] = (JsonParser parser, out object obj) =>
                {
                    bool success = parser.Parse(out string value);
                    obj = value;
                    return success;
                },
            };

        private static readonly IDictionary<Type, ValueParser<object>> _cachedParsers =
            new Dictionary<Type, ValueParser<object>>();

        private static readonly SerializedFieldsCache _serializedFieldsCache = new SerializedFieldsCache();

        private static readonly MethodInfo _parseArray = typeof(JsonParser).GetMethod(nameof(ParseArray));
        private static readonly MethodInfo _parseICollection = typeof(JsonParser).GetMethod(nameof(ParseICollection));
        private static readonly MethodInfo _parseIDictionary = typeof(JsonParser).GetMethod(nameof(ParseIDictionary));
        private static readonly MethodInfo _parseObject = typeof(JsonParser).GetMethod(nameof(ParseObject), new[] { typeof(object).MakeByRefType() });

        private readonly JsonScanner _scanner;

        public JsonParser(TextReader reader)
        {
            _scanner = new JsonScanner(reader);
        }

        public JsonParser(string text)
        {
            _scanner = new JsonScanner(text);
        }

        #region Primitives (bool, numbers, string)

        public bool Parse(out bool value)
        {
            _scanner.ReadWhitespace();
            return _scanner.ReadBool(out value);
        }

        public bool Parse(out int value)
        {
            _scanner.ReadWhitespace();
            return _scanner.ReadInt(out value);
        }

        public bool Parse(out long value)
        {
            _scanner.ReadWhitespace();
            return _scanner.ReadLong(out value);
        }

        public bool Parse(out float value)
        {
            _scanner.ReadWhitespace();
            return _scanner.ReadFloat(out value);
        }

        public bool Parse(out double value)
        {
            _scanner.ReadWhitespace();
            return _scanner.ReadDouble(out value);
        }

        public bool Parse(out string value)
        {
            _scanner.ReadWhitespace();
            return _scanner.ReadString(out value);
        }

        #endregion

        #region List<T>

        public bool ParseICollection<TCollection, T>(out TCollection list) where TCollection : ICollection<T>, new()
        {
            list = new TCollection();
            
            if (_scanner.ReadWhitespace() && !_scanner.ReadOpenArray())
            {
                return false;
            }

            if (ParseArrayValue(list))
            {
                while (_scanner.ReadWhitespace() && _scanner.ReadValueSeparator())
                {
                    if (!ParseArrayValue(list))
                    {
                        throw new ParseException($"Expected {typeof(T).Name} value");
                    }
                }
            }

            if (_scanner.ReadWhitespace() && !_scanner.ReadCloseArray())
            {
                throw new ParseException("Expected closing array ']'");
            }

            return true;
        }
        public bool ParseList<T>(out List<T> list) => ParseICollection<List<T>, T>(out list);
        public bool Parse<T>(out List<T> list) => ParseList(out list);

        private bool ParseArrayValue<T>(ICollection<T> list)
        {
            if (!Parse(out T value))
            {
                return false;
            }
            list.Add(value);
            return true;
        }

        #endregion

        #region T[]

        public bool ParseArray<T>(out T[] array)
        {
            if (!ParseList(out List<T> list))
            {
                array = null;
                return false;
            }

            array = list.ToArray();
            return true;
        }

        public bool Parse<T>(out T[] array) => ParseArray(out array);

        #endregion

        #region Dictionary<string, T>

        public bool ParseIDictionary<TDict, T>(out TDict dict) where TDict : IDictionary<string, T>, new()
        {
            dict = new TDict();
            
            if (_scanner.ReadWhitespace() && !_scanner.ReadOpenObject())
            {
                return false;
            }

            if (ParseKeyValuePair(dict))
            {
                while (_scanner.ReadWhitespace() && _scanner.ReadValueSeparator())
                {
                    if (!ParseKeyValuePair(dict))
                    {
                        throw new ParseException($"Expected {typeof(T).Name} value");
                    }
                }
            }
            
            if (_scanner.ReadWhitespace() && !_scanner.ReadCloseObject())
            {
                throw new ParseException("Expected closing object '}'");
            }

            return true;
        }
        public bool ParseDictionary<T>(out Dictionary<string, T> dict) => ParseIDictionary<Dictionary<string, T>, T>(out dict);
        public bool Parse<T>(out Dictionary<string, T> dict) => ParseDictionary(out dict);

        private bool ParseKeyValuePair<T>(IDictionary<string, T> dict)
        {
            if (!Parse(out string key))
            {
                return false;
            }

            if (_scanner.ReadWhitespace() && !_scanner.ReadKeySeparator())
            {
                throw new ParseException("Expected colon ':'");
            }

            if (!Parse(out T value))
            {
                throw new ParseException($"Error parsing {typeof(T).Name} value");
            }
            dict[key] = value;
            return true;
        }

        #endregion

        #region Serializable class/struct

        public bool ParseObject(Type type, out object value)
        {
            if (_scanner.ReadWhitespace() && !_scanner.ReadOpenObject())
            {
                value = null;
                return false;
            }
            
            value = Activator.CreateInstance(type);
            Dictionary<string, FieldInfo> knownFields = _serializedFieldsCache.Get(type);

            if (ParseField(knownFields, value))
            {
                while (_scanner.ReadWhitespace() && _scanner.ReadValueSeparator())
                {
                    if (!ParseField(knownFields, value))
                    {
                        throw new ParseException("Expected field");
                    }
                }
            }
            
            if (_scanner.ReadWhitespace() && !_scanner.ReadCloseObject())
            {
                throw new ParseException("Expected closing object '}'");
            }

            return true;
        }
        public bool ParseObject<T>(out object value) => ParseObject(typeof(T), out value);

        private bool ParseField(IReadOnlyDictionary<string, FieldInfo> knownFields, object obj)
        {
            if (!Parse(out string key))
            {
                return false;
            }

            if (_scanner.ReadWhitespace() && !_scanner.ReadKeySeparator())
            {
                throw new ParseException("Expected colon ':'");
            }

            if (knownFields.TryGetValue(key, out FieldInfo field))
            {
                if (!ParseAny(field.FieldType, out object value))
                {
                    throw new ParseException($"Error parsing {field.FieldType.Name} value");
                }
                field.SetValue(obj, value);
            }
            else
            {
                _scanner.SkipElement();
            }

            return true;
        }

        #endregion

        #region Generic type

        public bool ParseAny(Type type, out object value)
        {
            if (_primitiveParsers.TryGetValue(type, out ValueParser<object> func)
                || _cachedParsers.TryGetValue(type, out func))
            {
                return func(this, out value);
            }

            if (type.IsArray)
            {
                MethodInfo method = _parseArray.MakeGenericMethod(type.GetElementType());
                var parseArray = (ValueParser<object>) Delegate.CreateDelegate(typeof(ValueParser<object>), method);
                _cachedParsers[type] = func = parseArray;
                return func.Invoke(this, out value);
            }

            Type[] genericArguments = type.GetGenericArguments();
            if (genericArguments.Length == 1 && typeof(ICollection<>).MakeGenericType(genericArguments[0]).IsAssignableFrom(type))
            {
                MethodInfo method = _parseICollection.MakeGenericMethod(type, genericArguments[0]);
                var parseListDelegate = (ValueParser<object>) Delegate.CreateDelegate(typeof(ValueParser<object>), method);
                _cachedParsers[type] = func = parseListDelegate;
            }
            else if (genericArguments.Length == 2 && typeof(IDictionary<,>).MakeGenericType(typeof(string), genericArguments[1]).IsAssignableFrom(type))
            {
                MethodInfo method = _parseIDictionary.MakeGenericMethod(type, genericArguments[1]);
                var parseDictionaryDelegate = (ValueParser<object>) Delegate.CreateDelegate(typeof(ValueParser<object>), method);
                _cachedParsers[type] = func = parseDictionaryDelegate;
            }
            else
            {
                MethodInfo method = _parseObject.MakeGenericMethod(type);
                var parseObjectDelegate = (ValueParser<object>) Delegate.CreateDelegate(typeof(ValueParser<object>), method);
                _cachedParsers[type] = func = parseObjectDelegate;
            }

            return func(this, out value);
        }

        public bool Parse(Type type, out object value) => ParseAny(type, out value);

        public bool Parse<T>(out T value)
        {
            bool success = ParseAny(typeof(T), out object obj);
            value = (T) obj;
            return success;
        }

        #endregion
    }
}