using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace DiCor.IO
{
    public partial struct CharacterEncoding
    {
        private static readonly FrozenDictionary<AsciiString, Details> s_dictionary = InitializeDictionary();

        private static FrozenDictionary<AsciiString, Details> InitializeDictionary()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            return EnumerateDetails().ToFrozenDictionary(optimizeForReading: true);

#pragma warning disable format
            static IEnumerable<KeyValuePair<AsciiString, Details>> EnumerateDetails()
            {
                // PS 3.3 - C.12.1.1.2 Specific Character Set
                // https://dicom.nema.org/medical/dicom/current/output/html/part03.html#table_C.12-2

                yield return new(new("ISO_IR 100"u8), new(Encoding.Latin1));
                yield return new(new("ISO_IR 101"u8), new(Encoding.GetEncoding(28592)));
                yield return new(new("ISO_IR 109"u8), new(Encoding.GetEncoding(28593)));
                yield return new(new("ISO_IR 110"u8), new(Encoding.GetEncoding(28594)));
                yield return new(new("ISO_IR 144"u8), new(Encoding.GetEncoding(28595)));
                yield return new(new("ISO_IR 127"u8), new(Encoding.GetEncoding(28596)));
                yield return new(new("ISO_IR 126"u8), new(Encoding.GetEncoding(28597)));
                yield return new(new("ISO_IR 138"u8), new(Encoding.GetEncoding(28598)));
                yield return new(new("ISO_IR 148"u8), new(Encoding.GetEncoding(28599)));
                yield return new(new("ISO_IR 203"u8), new(Encoding.GetEncoding(28605)));
                yield return new(new("ISO_IR 13"u8),  new(ShiftJisEncoding.Instance));
                yield return new(new("ISO_IR 166"u8), new(Encoding.GetEncoding(874)));

                // https://dicom.nema.org/medical/dicom/current/output/html/part03.html#table_C.12-3

                yield return new(new("ISO 2022 IR 6"u8),   new(Encoding.ASCII,              "\x28\x42"u8));
                yield return new(new("ISO 2022 IR 100"u8), new(Encoding.Latin1,             "\x2D\x41"u8));
                yield return new(new("ISO 2022 IR 101"u8), new(Encoding.GetEncoding(28592), "\x2D\x42"u8));
                yield return new(new("ISO 2022 IR 109"u8), new(Encoding.GetEncoding(28593), "\x2D\x43"u8));
                yield return new(new("ISO 2022 IR 110"u8), new(Encoding.GetEncoding(28594), "\x2D\x44"u8));
                yield return new(new("ISO 2022 IR 144"u8), new(Encoding.GetEncoding(28595), "\x2D\x4C"u8));
                yield return new(new("ISO 2022 IR 127"u8), new(Encoding.GetEncoding(28596), "\x2D\x47"u8));
                yield return new(new("ISO 2022 IR 126"u8), new(Encoding.GetEncoding(28597), "\x2D\x46"u8));
                yield return new(new("ISO 2022 IR 138"u8), new(Encoding.GetEncoding(28598), "\x2D\x48"u8));
                yield return new(new("ISO 2022 IR 148"u8), new(Encoding.GetEncoding(28599), "\x2D\x4D"u8));
                yield return new(new("ISO 2022 IR 203"u8), new(Encoding.GetEncoding(28605), "\x2D\x62"u8));
                yield return new(new("ISO 2022 IR 13"u8),  new(ShiftJisEncoding.Instance,   "\x29\x49"u8));
                yield return new(new(""u8),                new(ShiftJisEncoding.Instance,   "\x29\x4A"u8)); // IR 14 G0 only
                yield return new(new("ISO 2022 IR 166"u8), new(Encoding.GetEncoding(874),   "\x2D\x54"u8));

                // https://dicom.nema.org/medical/dicom/current/output/html/part03.html#table_C.12-4

                yield return new(new("ISO 2022 IR 87"u8),  new(Encoding.GetEncoding(50220), "\x24\x42"u8));     // Same cp...
                yield return new(new("ISO 2022 IR 159"u8), new(Encoding.GetEncoding(50220), "\x24\x28\x44"u8)); // set mode with esc seq
                yield return new(new("ISO 2022 IR 149"u8), new(Encoding.GetEncoding(20949), "\x24\x29\x43"u8));
                yield return new(new("ISO 2022 IR 58"u8),  new(Encoding.GetEncoding(936),   "\x24\x29\x41"u8));

                // https://dicom.nema.org/medical/dicom/current/output/html/part03.html#table_C.12-5

                yield return new(new("ISO_IR 192"u8), new(Encoding.UTF8));
                yield return new(new("GB18030"u8),    new(Encoding.GetEncoding(54936)));
                yield return new(new("GBK"u8),        new(Encoding.GetEncoding(936)));
            }
        }
#pragma warning restore format

        public bool IsKnown([NotNullWhen(true)] out Details? details)
            => s_dictionary.TryGetValue(_name, out details);

        public Details GetDetails()
            => s_dictionary.TryGetValue(_name, out Details? details) ? details : throw new ArgumentException($"{this} is not a valid character encoding name.", "value");

        public sealed class Details
        {
            public static readonly Details Default = new(Encoding.ASCII);
            public static readonly Details DefaultIso2022 = new CharacterEncoding("ISO 2022 IR 6"u8).GetDetails();

            public Encoding Encoding { get; }

            public byte[] EscapeSequence { get; }

            public Decoder Decoder { get; }

            public bool EncodingIsIso2022Aware
                => Encoding.CodePage is 50220 or 936;

            internal Details(Encoding encoding, ReadOnlySpan<byte> escapeSequence = default)
            {
                Encoding = encoding;
                EscapeSequence = escapeSequence.ToArray();
                Decoder = encoding.GetDecoder();
            }
        }
    }
}
