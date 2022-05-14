using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Gilzoide.StreamingJson
{
    public static class TextReaderExtensions
    {
        public static bool ReadIf(this TextReader reader, char character)
        {
            return reader.Peek() == character && reader.Read() >= 0;
        }

        public static bool ReadExpecting(this TextReader reader, string chars)
        {
            using (IEnumerator<char> enumerator = chars.GetEnumerator())
            {
                if (!enumerator.MoveNext() || !reader.ReadIf(enumerator.Current))
                {
                    return false;
                }

                while (enumerator.MoveNext())
                {
                    int c = reader.Read();
                    if (c < 0)
                    {
                        throw new ParseException($"Encountered EOF while parsing {chars}");
                    }
                    if (c != enumerator.Current)
                    {
                        throw new ParseException($"Error while parsing {chars}");
                    }
                }

                return true;
            }
        }

        public static bool ReadDigit(this TextReader reader, out char digit)
        {
            int c = reader.Peek();
            if (c >= 0)
            {
                return char.IsDigit(digit = (char) c) && reader.Read() >= 0;
            }

            digit = default;
            return false;
        }

        public static char ReadDigit(this TextReader reader)
        {
            return ReadDigit(reader, out char digit)
                ? digit
                : throw new ParseException("Expected digit");
        }

        public static bool ReadHexDigit(this TextReader reader, out char hexDigit)
        {
            int c = reader.Peek();
            if (c >= 0)
            {
                return (char.IsDigit(hexDigit = (char) c)
                        || c >= 'a' && c <= 'f'
                        || c >= 'A' && c <= 'F')
                    && reader.Read() >= 0;
            }

            hexDigit = default;
            return false;
        }

        public static char ReadHexDigit(this TextReader reader)
        {
            return ReadHexDigit(reader, out char hexDigit)
                ? hexDigit
                : throw new ParseException("Expected hexadecimal digit");
        }

        public static int ReadHexDigitValue(this TextReader reader)
        {
            return ReadHexDigit(reader) - '0';
        }
    }
}