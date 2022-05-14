using System;

namespace Gilzoide.StreamingJson
{
    public class ParseException : Exception
    {
        public ParseException() {}
        public ParseException(string message) : base(message) {}
    }
}