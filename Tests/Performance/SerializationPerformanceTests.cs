using System;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.PerformanceTesting;
#if HAVE_NEWTONSOFT_JSON
using Newtonsoft.Json;
#endif

namespace Gilzoide.StreamingJson.Tests.Performance
{
    public class SerializationPerformanceTests
    {
        private const string JSON_CONTENTS = "{\"Key\": \"Some key\", \"Values\": [42], \"DeprecatedValue\": 0,\n" +
            "\"KeyValuePairs\": {\"one\": 1}}";

        private struct TestStruct
        {
            public string Key;
            public List<int> Values;
            public Dictionary<string, int> KeyValuePairs;
        }

        [Test, Performance]
        public void StreamingJson()
        {
            RunMeasure(() =>
            {
                new JsonParser(JSON_CONTENTS).Parse(out TestStruct value);
                return value;
            });
        }
        
        [Test, Performance]
        public void JsonUtility()
        {
            RunMeasure(() => UnityEngine.JsonUtility.FromJson<TestStruct>(JSON_CONTENTS));
        }

        private void RunMeasure(Func<TestStruct> method)
        {
            Measure.Method(() =>
                {
                    TestStruct value = method();
//                    Assert.AreEqual("Some key", value.Key);
//                    CollectionAssert.AreEquivalent(new[] {42}, value.Values);
//                    CollectionAssert.AreEquivalent(new Dictionary<string, int> { ["one"] = 1 }, value.KeyValuePairs);
                })
                .GC()
                .MeasurementCount(30)
                .IterationsPerMeasurement(1000)
                .Run();
        }
        
#if HAVE_NEWTONSOFT_JSON
        [Test, Performance]
        public void Newtonsoft()
        {
            RunMeasure(() => JsonConvert.DeserializeObject<TestStruct>(JSON_CONTENTS));
        }
#endif
    }
}
