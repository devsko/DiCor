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
            // https://dicom.nema.org/medical/dicom/current/output/html/part05.html#sect_6.2

            static IEnumerable<KeyValuePair<ushort, Details>> EnumerateDetails()
            {
                yield return new((ushort)AE._value, new("Application Entity"));
                yield return new((ushort)AS._value, new("Age String"));
                yield return new((ushort)AT._value, new("Attribute Tag"));
                yield return new((ushort)CS._value, new("Code String"));
                yield return new((ushort)DA._value, new("Date"));
                yield return new((ushort)DS._value, new("Decimal String"));
                yield return new((ushort)DT._value, new("Date Time"));
                yield return new((ushort)FL._value, new("Floating Point Single"));
                yield return new((ushort)FD._value, new("Floating Point Double"));
                yield return new((ushort)IS._value, new("Integer String"));
                yield return new((ushort)LO._value, new("Long String"));
                yield return new((ushort)LT._value, new("Long Text"));
                yield return new((ushort)OB._value, new("Other Byte String"));
                yield return new((ushort)OD._value, new("Other Double String"));
                yield return new((ushort)OF._value, new("Other Float String"));
                yield return new((ushort)OL._value, new("Other Long"));
                yield return new((ushort)OV._value, new("Other 64-bit Very Long"));
                yield return new((ushort)OW._value, new("Other Word String"));
                yield return new((ushort)PN._value, new("Person Name"));
                yield return new((ushort)SH._value, new("Short String"));
                yield return new((ushort)SL._value, new("Signed Long"));
                yield return new((ushort)SQ._value, new("Sequence of Items"));
                yield return new((ushort)SS._value, new("Signed Short"));
                yield return new((ushort)ST._value, new("Short Text"));
                yield return new((ushort)SV._value, new("Signed 64-bit Very Long"));
                yield return new((ushort)TM._value, new("Time"));
                yield return new((ushort)UC._value, new("Unlimited Characters"));
                yield return new((ushort)UI._value, new("Unique Identifier (UID)"));
                yield return new((ushort)UL._value, new("Unsigned Long"));
                yield return new((ushort)UN._value, new("Unknown"));
                yield return new((ushort)UR._value, new("Universal Resource Identifier or Universal Resource Locator (URI/URL)"));
                yield return new((ushort)US._value, new("Unsigned Short"));
                yield return new((ushort)UT._value, new("Unlimited Text"));
                yield return new((ushort)UV._value, new("Unsigned 64-bit Very Long"));
            }
        }

        public Details GetDetails()
            => s_dictionary.TryGetValue((ushort)_value, out Details? details) ? details : throw new ArgumentException($"{this} is not a valid VR.", "value");

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
