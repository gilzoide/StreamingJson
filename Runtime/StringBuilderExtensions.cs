using System.IO;
using System.Text;

namespace Gilzoide.StreamingJson
{
    public static class StringBuilderExtensions
    {
        public static bool AppendIfRead(this StringBuilder builder, TextReader reader, char character)
        {
            if (!reader.ReadIf(character))
            {
                return false;
            }

            builder.Append(character);
            return true;
        }

        public static bool AppendIfReadDigit(this StringBuilder builder, TextReader reader, out char digit)
        {
            if (!reader.ReadDigit(out digit))
            {
                return false;
            }

            builder.Append(digit);
            return true;
        }
    }
}