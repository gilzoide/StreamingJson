using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Serialization;

namespace Gilzoide.StreamingJson.Tests
{
    public class JsonParserTests
    {
        [Serializable]
        private struct SomeType
        {
            public int One;
            public int Two;
            [FormerlySerializedAs("Values")] public int[] Others;
        }
        
        [Test]
        public void BoolArray()
        {
            TestValidArray("[true,false,true,true]", new[] { true, false, true, true });
            TestValidArray("\t[ false ,\ntrue ]", new[] { false, true });
            
            TestNotArray("1");
            TestNotArray("true");
            TestNotArray("null, error");
            TestNotArray("{\"type\": \"object\"}");
            TestNotArray("");
            
            TestArrayException<bool>("[");
            TestArrayException<bool>("[true");
            TestArrayException<bool>("[true false]");
            TestArrayException<bool>("[true,,false]");
            TestArrayException<bool>("[1]");
        }

        [Test]
        public void Any()
        {
            var parser = new JsonParser("   [1,\t2,\n3 ] ");
            Assert.IsTrue(parser.ParseAny(typeof(int[]), out object value));
            Assert.AreEqual(new[] {1, 2, 3}, value);
            
            parser = new JsonParser("   [1,\t2,\n3 ] ");
            Assert.IsTrue(parser.ParseAny(typeof(List<int>), out value));
            CollectionAssert.AreEqual(new List<int> {1, 2, 3}, (IEnumerable) value);
            
            parser = new JsonParser("{\"one\":1,\"two\":2}");
            Assert.IsTrue(parser.ParseAny(typeof(Dictionary<string, int>), out value));
            CollectionAssert.AreEquivalent(new Dictionary<string, int> {["one"] = 1, ["two"] = 2}, (IEnumerable) value);
            
            parser = new JsonParser("{\"Item1\":1,\"Item2\":2}");
            Assert.IsTrue(parser.ParseObject(typeof((int, int)), out value));
            Assert.AreEqual((1, 2), value);
            
            parser = new JsonParser("{\"One\":1, \"Two\": 2}");
            Assert.IsTrue(parser.ParseObject(typeof(SomeType), out value));
            Assert.AreEqual(new SomeType { One = 1, Two = 2 }, value);
            
            parser = new JsonParser("{\n  \"Values\": [1, 52, -500],\n  \"Nonexistent\": [{}, {}]}");
            Assert.IsTrue(parser.Parse(out SomeType someValue));
            Assert.AreEqual(default(int), someValue.One);
            Assert.AreEqual(default(int), someValue.Two);
            Assert.AreEqual(new[]{ 1, 52, -500 }, someValue.Others);
        }

        private static void TestValidArray(string text, bool[] expectedValues)
        {
            var parser = new JsonParser(text);
            Assert.IsTrue(parser.Parse(out bool[] array));
            Assert.AreEqual(expectedValues, array);
        }
        private static void TestNotArray(string text)
        {
            var parser = new JsonParser(text);
            Assert.IsFalse(parser.Parse(out bool[] array));
            Assert.IsNull(array);
        }
        private static void TestArrayException<T>(string text)
        {
            var parser = new JsonParser(text);
            Assert.Throws<ParseException>(() => parser.Parse(out T[] _));
        }
    }
}