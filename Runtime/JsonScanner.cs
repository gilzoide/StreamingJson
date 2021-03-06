using System;
using System.IO;
using System.Text;

namespace Gilzoide.StreamingJson
{
    public class JsonScanner
    {
        private readonly TextReader _reader;
        private readonly StringBuilder _stringBuilder = new StringBuilder();
        
        public JsonScanner(TextReader reader)
        {
            _reader = reader;
        }
        
        public JsonScanner(string text) : this(new StringReader(text)) {}

        public static bool ReadWhitespace(TextReader reader)
        {
            while (char.IsWhiteSpace((char) reader.Peek()))
            {
                reader.Read();
            }
            return true;
        }
        public bool ReadWhitespace() => ReadWhitespace(_reader);

        public static bool ReadNull(TextReader reader)
        {
            return reader.ReadExpecting("null");
        }
        public bool ReadNull() => ReadNull(_reader);
        
        public static bool ReadBool(TextReader reader, out bool value)
        {
            if (ReadTrue(reader))
            {
                value = true;
                return true;
            }

            value = false;
            return ReadFalse(reader);
        }
        public bool ReadBool(out bool value) => ReadBool(_reader, out value);
        
        public static bool ReadTrue(TextReader reader)
        {
            return reader.ReadExpecting("true");
        }
        public bool ReadTrue() => ReadTrue(_reader);
        
        public static bool ReadFalse(TextReader reader)
        {
            return reader.ReadExpecting("false");
        }
        public bool ReadFalse() => ReadFalse(_reader);

        public static bool ReadInt(TextReader reader, StringBuilder builder, out int value)
        {
            bool isNumber = ReadNumber(reader, builder, out string text);
            value = isNumber ? int.Parse(text) : default;
            return isNumber;
        }
        public static bool ReadInt(TextReader reader, out int value) => ReadInt(reader, new StringBuilder(), out value);
        public bool ReadInt(out int value) => ReadInt(_reader, _stringBuilder, out value);
        
        public static bool ReadLong(TextReader reader, StringBuilder builder, out long value)
        {
            bool isNumber = ReadNumber(reader, builder, out string text);
            value = isNumber ? long.Parse(text) : default;
            return isNumber;
        }
        public static bool ReadLong(TextReader reader, out long value) => ReadLong(reader, new StringBuilder(), out value);
        public bool ReadLong(out long value) => ReadLong(_reader, _stringBuilder, out value);
        
        public static bool ReadFloat(TextReader reader, StringBuilder builder, out float value)
        {
            bool isNumber = ReadNumber(reader, builder, out string text);
            value = isNumber ? float.Parse(text) : default;
            return isNumber;
        }
        public static bool ReadFloat(TextReader reader, out float value) => ReadFloat(reader, new StringBuilder(), out value);
        public bool ReadFloat(out float value) => ReadFloat(_reader, _stringBuilder, out value);
        
        public static bool ReadDouble(TextReader reader, StringBuilder builder, out double value)
        {
            bool isNumber = ReadNumber(reader, builder, out string text);
            value = isNumber ? double.Parse(text) : default;
            return isNumber;
        }
        public static bool ReadDouble(TextReader reader, out double value) => ReadDouble(reader, new StringBuilder(), out value);
        public bool ReadDouble(out double value) => ReadDouble(_reader, _stringBuilder, out value);

        public static bool ReadString(TextReader reader, StringBuilder builder, out string value)
        {
            if (!reader.ReadIf('"'))
            {
                value = "";
                return false;
            }
            
            builder.Clear();
            int c;
            while ((c = reader.Read()) >= 0)
            {
                switch (c)
                {
                    case '\\':
                        builder.Append(ReadEscapeSequence(reader));
                        break;
                    
                    case '"':
                        value = builder.ToString();
                        return true;
                    
                    default:
                        builder.Append((char) c);
                        break;
                }
            }

            throw new ParseException("Expected closing double quotes");
        }
        public static bool ReadString(TextReader reader, out string value) => ReadString(reader, new StringBuilder(), out value);
        public bool ReadString(out string value) => ReadString(_reader, _stringBuilder, out value);

        public static bool ReadNumber(TextReader reader, StringBuilder builder, out string value)
        {
            builder.Clear();
            
            // '-'?
            builder.AppendIfRead(reader, '-');
            
            // '0' | [0-9]+
            if (!builder.AppendIfReadDigit(reader, out char digit))
            {
                value = "";
                return false;
            }
            if (digit != '0')
            {
                ReadDigits(reader, builder);
            }
            // '' | '.' [0-9]+
            if (builder.AppendIfRead(reader, '.'))
            {
                if (!ReadDigits(reader, builder))
                {
                    throw new ParseException("Expected digits while parsing number fractional part");
                }
            }
            // '' | [eE] [-+]? [0-9]+
            if (builder.AppendIfRead(reader, 'e') || builder.AppendIfRead(reader, 'E'))
            {
                _ = builder.AppendIfRead(reader, '-') || builder.AppendIfRead(reader, '+');
                if (!ReadDigits(reader, builder))
                {
                    throw new ParseException("Expected digits while parsing number exponential part");
                }
            }

            value = builder.ToString();
            return true;
        }
        public static bool ReadNumber(TextReader reader, out string value) => ReadNumber(reader, new StringBuilder(), out value);
        public bool ReadNumber(out string value) => ReadNumber(_reader, _stringBuilder, out value);

        public static bool ReadOpenArray(TextReader reader)
        {
            return reader.ReadIf('[');
        }
        public bool ReadOpenArray() => ReadOpenArray(_reader);
        
        public static bool ReadCloseArray(TextReader reader) => reader.ReadIf(']');
        public bool ReadCloseArray() => ReadCloseArray(_reader);
        
        public static bool ReadValueSeparator(TextReader reader) => reader.ReadIf(',');
        public bool ReadValueSeparator() => ReadValueSeparator(_reader);
        
        public static bool ReadOpenObject(TextReader reader) => reader.ReadIf('{');
        public bool ReadOpenObject() => ReadOpenObject(_reader);
        
        public static bool ReadCloseObject(TextReader reader) => reader.ReadIf('}');
        public bool ReadCloseObject() => ReadCloseObject(_reader);
        
        public static bool ReadKeySeparator(TextReader reader) => reader.ReadIf(':');
        public bool ReadKeySeparator() => ReadKeySeparator(_reader);

        public static bool SkipElement(TextReader reader, StringBuilder builder)
        {
            ReadWhitespace(reader);
            switch (reader.Peek())
            {
                case 'n': return ReadNull(reader);
                case 't': return ReadTrue(reader);
                case 'f': return ReadFalse(reader);
                case '0': case '1': case '2': case '3': case '4': case '5': case '6': case '7': case '8': case '9':
                case '-': return ReadNumber(reader, builder, out _);
                case '"': return ReadString(reader, builder, out _);
                case '[':
                    ReadOpenArray(reader);
                    if (SkipElement(reader, builder))
                    {
                        while (ReadWhitespace(reader) && ReadValueSeparator(reader))
                        {
                            SkipElement(reader, builder);
                        }
                    }
                    return ReadCloseArray(reader);
                case '{':
                    ReadOpenObject(reader);
                    if (SkipElement(reader, builder) && ReadKeySeparator(reader) && SkipElement(reader, builder))
                    {
                        while (ReadWhitespace(reader) && ReadValueSeparator(reader))
                        {
                            SkipElement(reader, builder);
                            ReadWhitespace(reader);
                            ReadKeySeparator(reader);
                            SkipElement(reader, builder);
                        }
                    }
                    return ReadCloseObject(reader);
                default:
                    return false;
            }
        }
        public static bool SkipElement(TextReader reader) => SkipElement(reader, new StringBuilder());
        public bool SkipElement() => SkipElement(_reader, _stringBuilder); 

        // Private helper methods
        private static bool ReadDigits(TextReader reader, StringBuilder builder)
        {
            if (!reader.ReadDigit(out char digit))
            {
                return false;
            }
            do
            {
                builder.Append(digit);
            } while (reader.ReadDigit(out digit));
            
            return true;
        }

        private static char ReadEscapeSequence(TextReader reader)
        {
            int c = reader.Read();
            switch (c)
            {
                case '"': return '"';
                case '\\': return '\\';
                case '/': return '/';
                case 'b': return '\b';
                case 'f': return '\f';
                case 'n': return '\n';
                case 'r': return '\r';
                case 't': return '\t';
                case 'u':
                    int value = reader.ReadHexDigitValue() << 24
                        + reader.ReadHexDigitValue() << 16
                        + reader.ReadHexDigitValue() << 8
                        + reader.ReadHexDigitValue();
                    return (char) value;
                
                default:
                    throw new ParseException("Invalid escape sequence");
            }
        }
    }
}