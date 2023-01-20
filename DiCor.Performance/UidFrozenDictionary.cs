using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Attributes;

namespace DiCor.Performance
{
    public class UidFrozenDictionary
    {
        private class Comparer : IEqualityComparer<byte[]>
        {
            public bool Equals(byte[]? x, byte[]? y)
            {
                if (ReferenceEquals(x, y))
                    return true;
                if (x is null || y is null)
                    return false;

                return x.SequenceEqual(y);
            }
            public int GetHashCode([DisallowNull] byte[] obj)
            {
                HashCode hash = new();
                hash.AddBytes(obj.AsSpan());
                return hash.ToHashCode();
            }
        }
        public UidFrozenDictionary()
        {
            Comparer comparer = new();

            var field = typeof(Uid).GetField("s_dictionary", BindingFlags.Static | BindingFlags.NonPublic)!;

            _uidDictionary = (FrozenDictionary<Uid, Uid.Details>)field.GetValue(null)!;
            _dictionary = _uidDictionary.Keys.ToDictionary(uid => uid.Value, comparer).ToFrozenDictionary(comparer);

            _uidValues = _uidDictionary.Keys.Select(uid => (byte[])uid.Value.Clone()).ToArray();
            _uids = _uidValues.Select(value => new Uid(value, false)).ToArray();

            Console.WriteLine("XXXXXXXXXXX");
            Console.WriteLine(_dictionary.GetType().FullName);
            Console.WriteLine(_dictionary.GetType().Assembly.Location);
            Console.WriteLine(_dictionary.Comparer.GetType().FullName);
            Console.WriteLine(_dictionary.GetType().GetField("_partialComparer", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(_dictionary)!.GetType().FullName ?? "");
        }

        private byte[][] _uidValues;
        private Uid[] _uids;
        private FrozenDictionary<Uid, Uid.Details> _uidDictionary;
        private FrozenDictionary<byte[], Uid> _dictionary;

        [Benchmark(Baseline = true)]
        public void UidDictionary()
        {
            foreach (var uid in _uids)
            {
                var details = _uidDictionary[uid];
            }
        }
        [Benchmark]
        public void UidValueDictionary()
        {
            foreach (var uid in _uidValues)
            {
                var details = _dictionary[uid];
            }
        }
    }
}
