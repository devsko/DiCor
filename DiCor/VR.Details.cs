using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

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
                yield return new((ushort)AE._code, new("Application Entity", false, false, false));
                yield return new((ushort)AS._code, new("Age String", false, false, false));
                yield return new((ushort)AT._code, new("Attribute Tag", false, true, false));
                yield return new((ushort)CS._code, new("Code String", false, false, false));
                yield return new((ushort)DA._code, new("Date", false, false, false));
                yield return new((ushort)DS._code, new("Decimal String", false, false, false));
                yield return new((ushort)DT._code, new("Date Time", false, false, false));
                yield return new((ushort)FD._code, new("Floating Point Double", false, true, false));
                yield return new((ushort)FL._code, new("Floating Point Single", false, true, false));
                yield return new((ushort)IS._code, new("Integer String", false, false, false));
                yield return new((ushort)LO._code, new("Long String", false, true, false));
                yield return new((ushort)LT._code, new("Long Text", false, false, true));
                yield return new((ushort)OB._code, new("Other Byte", true, true, true));
                yield return new((ushort)OD._code, new("Other Double", true, true, true));
                yield return new((ushort)OF._code, new("Other Float", true, true, true));
                yield return new((ushort)OL._code, new("Other Long", true, true, true));
                yield return new((ushort)OV._code, new("Other 64-bit Very Long", true, true, true));
                yield return new((ushort)OW._code, new("Other Word", true, true, true));
                yield return new((ushort)PN._code, new("Person Name", false, false, false));
                yield return new((ushort)SH._code, new("Short String", false, false, false));
                yield return new((ushort)SL._code, new("Signed Long", false, true, false));
                yield return new((ushort)SQ._code, new("Sequence of Items", true, true, true));
                yield return new((ushort)SS._code, new("Signed Short", false, true, false));
                yield return new((ushort)ST._code, new("Short Text", false, false, true));
                yield return new((ushort)SV._code, new("Signed 64-bit Very Long", true, true, false));
                yield return new((ushort)TM._code, new("Time", false, false, false));
                yield return new((ushort)UC._code, new("Unlimited Characters", true, false, false));
                yield return new((ushort)UI._code, new("Unique Identifier (UID)", false, false, false));
                yield return new((ushort)UL._code, new("Unsigned Long", false, true, false));
                yield return new((ushort)UN._code, new("Unknown", true, true, true));
                yield return new((ushort)UR._code, new("Universal Resource Identifier or Universal Resource Locator (URI/URL)", true, false, true));
                yield return new((ushort)US._code, new("Unsigned Short", false, false, false));
                yield return new((ushort)UT._code, new("Unlimited Text", true, false, true));
                yield return new((ushort)UV._code, new("Unsigned 64-bit Very Long", true, true, false));
            }
        }

        public bool IsKnown([NotNullWhen(true)] out Details? details)
            => s_dictionary.TryGetValue((ushort)_code, out details);

        public Details GetDetails()
            => s_dictionary.TryGetValue((ushort)_code, out Details? details) ? details : throw new ArgumentException($"{this} is not a valid VR.", "value");

        public sealed class Details
        {
            public string? Name { get; }

            public bool Length32bit { get; }

            public bool BinaryValue { get; }

            public bool AlwaysSingleValue { get; }

            internal Details(string name, bool length32bit, bool binaryValue, bool alwaysSingleValue)
            {
                Name = name;
                Length32bit = length32bit;
                BinaryValue = binaryValue;
                AlwaysSingleValue = alwaysSingleValue;
            }
        }
    }
}
