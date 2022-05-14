using System.IO;
using NUnit.Framework;

namespace Gilzoide.StreamingJson.Tests
{
    public class JsonScannerTests
    {
        [Test]
        public void Null()
        {
            Assert.IsTrue(JsonScanner.ReadNull(new StringReader("null")));
            Assert.IsTrue(JsonScanner.ReadNull(new StringReader("nulllll")));
            Assert.IsTrue(JsonScanner.ReadNull(new StringReader("null, false")));

            Assert.Throws<ParseException>(() => JsonScanner.ReadNull(new StringReader("notnull")));
            Assert.Throws<ParseException>(() => JsonScanner.ReadNull(new StringReader("nul")));

            Assert.IsFalse(JsonScanner.ReadNull(new StringReader("\"This is a string, not null\"")));
            Assert.IsFalse(JsonScanner.ReadNull(new StringReader("")));
        }
        
        [Test]
        public void True()
        {
            Assert.IsTrue(JsonScanner.ReadTrue(new StringReader("true")));
            Assert.IsTrue(JsonScanner.ReadTrue(new StringReader("true,false")));

            Assert.Throws<ParseException>(() => JsonScanner.ReadTrue(new StringReader("tru")));
            Assert.Throws<ParseException>(() => JsonScanner.ReadTrue(new StringReader("throw")));

            Assert.IsFalse(JsonScanner.ReadTrue(new StringReader("false")));
            Assert.IsFalse(JsonScanner.ReadTrue(new StringReader("null")));
            Assert.IsFalse(JsonScanner.ReadTrue(new StringReader("")));
        }
        
        [Test]
        public void False()
        {
            Assert.IsTrue(JsonScanner.ReadFalse(new StringReader("false")));
            Assert.IsTrue(JsonScanner.ReadFalse(new StringReader("false,true")));

            Assert.Throws<ParseException>(() => JsonScanner.ReadFalse(new StringReader("fal")));
            Assert.Throws<ParseException>(() => JsonScanner.ReadFalse(new StringReader("failed")));

            Assert.IsFalse(JsonScanner.ReadFalse(new StringReader("true")));
            Assert.IsFalse(JsonScanner.ReadFalse(new StringReader("null")));
            Assert.IsFalse(JsonScanner.ReadFalse(new StringReader("")));
        }
        
        [Test]
        public void Number()
        {
            TestValidNumber("0");
            TestValidNumber("1");
            TestValidNumber("-500");
            TestValidNumber("-0.64");
            TestValidNumber("3e20");

            TestNotNumber("null");
            TestNotNumber("false");
            TestNotNumber("error");
            TestNotNumber("");
            
            TestNumberException("0.");
            TestNumberException("0.5e");
        }
        private void TestValidNumber(string s)
        {
            Assert.IsTrue(JsonScanner.ReadNumber(new StringReader(s), out _));
        }
        private void TestNotNumber(string s)
        {
            Assert.IsFalse(JsonScanner.ReadNumber(new StringReader(s), out _));
        }
        private void TestNumberException(string s)
        {
            Assert.Throws<ParseException>(() => JsonScanner.ReadNumber(new StringReader(s), out _));
        }

        [Test]
        public void String()
        {
            TestValidString("\"\"");
            TestValidString("\"hi!\"");
            TestValidString("\"some\\\\escaped\\t stuff\\n\\r\\u32a2\"");
            
            TestNotString("null");
            TestNotString("true");
            TestNotString("false");
            TestNotString("42");
            TestNotString("error");
            TestNotString("[???]");
            TestNotString("");
            
            TestStringException("\"not closed");
            TestStringException("\"not closed\\\"");
            TestStringException("\"invalid escape \\:\"");
            TestStringException("\"invalid escape \\a\"");
            TestStringException("\"invalid escape \\u87\"");
            TestStringException("\"invalid escape \\uuuuu\"");
        }
        private void TestValidString(string s)
        {
            Assert.IsTrue(JsonScanner.ReadString(new StringReader(s), out _));
        }
        private void TestNotString(string s)
        {
            Assert.IsFalse(JsonScanner.ReadString(new StringReader(s), out _));
        }
        private void TestStringException(string s)
        {
            Assert.Throws<ParseException>(() => JsonScanner.ReadString(new StringReader(s), out _));
        }
    }
}
