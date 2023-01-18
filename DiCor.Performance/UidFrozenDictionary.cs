using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Attributes;

namespace DiCor.Performance
{
    public class UidFrozenDictionary
    {
        public UidFrozenDictionary()
        {
            var field = typeof(Uid).GetField("s_dictionary", BindingFlags.Static | BindingFlags.NonPublic)!;
            _dictionary = (FrozenDictionary<Uid, Uid.Details>)field.GetValue(null)!;
            _uids = _dictionary.Keys.ToArray();

            _stringDictionary = _uids
                .Zip(_uids.Select(uid => _dictionary[uid]))
                .Select(tuple => new KeyValuePair<string, Uid.Details>(tuple.First.ToString()!, tuple.Second))
                .ToFrozenDictionary(StringComparer.Ordinal);
            _stringUids = _uids.Select(uid => uid.ToString()!).ToArray();
        }

        private FrozenDictionary<Uid, Uid.Details> _dictionary;
        private Uid[] _uids;
        private FrozenDictionary<string, Uid.Details> _stringDictionary;
        private string[] _stringUids;

        [Benchmark(Baseline = true)]
        public void TestUidDictionary()
        {
            foreach (var uid in _uids)
            {
                var details = _dictionary[uid];
            }
        }

        [Benchmark]
        public void TestStringDictionary()
        {
            foreach (var uid in _stringUids)
            {
                var details = _stringDictionary[uid];
            }
        }
    }
}
