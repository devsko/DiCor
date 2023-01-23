using System;
using System.Collections.Frozen;
using System.Collections.Generic;

namespace DiCor
{
    public partial struct VR
    {
        private static readonly FrozenDictionary<ushort, Details> s_dictionary = InitializeDictionary();

        private static FrozenDictionary<ushort, Details> InitializeDictionary()
        {
            return EnumerateDetails().ToFrozenDictionary();

            // PS3.5 - 6.2 Value Representation

            static IEnumerable<KeyValuePair<ushort, Details>> EnumerateDetails()
            {
                yield return new(AE._value, new("Application Entity"));
                yield return new(AS._value, new("Age String"));
                yield return new(AT._value, new("Attribute Tag"));
                yield return new(CS._value, new("Code String"));
                yield return new(DA._value, new("Date"));
                yield return new(DS._value, new("Decimal String"));
                yield return new(DT._value, new("Date Time"));
                yield return new(FL._value, new("Floating Point Single"));
                yield return new(FD._value, new("Floating Point Double"));
                yield return new(IS._value, new("Integer String"));
                yield return new(LO._value, new("Long String"));
                yield return new(LT._value, new("Long Text"));
                yield return new(OB._value, new("Other Byte String"));
                yield return new(OD._value, new("Other Double String"));
                yield return new(OF._value, new("Other Float String"));
                yield return new(OL._value, new("Other Long"));
                yield return new(OV._value, new("Other 64-bit Very Long"));
                yield return new(OW._value, new("Other Word String"));
                yield return new(PN._value, new("Person Name"));
                yield return new(SH._value, new("Short String"));
                yield return new(SL._value, new("Signed Long"));
                yield return new(SQ._value, new("Sequence of Items"));
                yield return new(SS._value, new("Signed Short"));
                yield return new(ST._value, new("Short Text"));
                yield return new(SV._value, new("Signed 64-bit Very Long"));
                yield return new(TM._value, new("Time"));
                yield return new(UC._value, new("Unlimited Characters"));
                yield return new(UI._value, new("Unique Identifier (UID)"));
                yield return new(UL._value, new("Unsigned Long"));
                yield return new(UN._value, new("Unknown"));
                yield return new(UR._value, new("Universal Resource Identifier or Universal Resource Locator (URI/URL)"));
                yield return new(US._value, new("Unsigned Short"));
                yield return new(UT._value, new("Unlimited Text"));
                yield return new(UV._value, new("Unsigned 64-bit Very Long"));
            }
        }

        public Details GetDetails()
            => s_dictionary.TryGetValue(_value, out Details? details) ? details : throw new ArgumentException($"{this} is not a valid VR.", "value");

        public sealed class Details
        {
            public string? Name { get; }

            internal Details(string name)
            {
                Name = name;
            }
        }
    }
}
