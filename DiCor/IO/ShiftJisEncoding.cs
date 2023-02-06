using System;
using System.Text;

namespace DiCor.IO
{
    // PS 3.5 - 6.1.2.3 Encoding of Character Repertoires
    // ...
    // The 8-bit code table of [JIS X 0201] includes ISO-IR 14 (romaji alphanumeric characters) as
    // the G0 code element and ISO-IR 13 (katakana phonetic characters) as the G1 code element.
    // ISO-IR 14 is identical to ISO-IR 6, except that bit combination 05/12 represents a "¥" (YEN SIGN)
    // and bit combination 07/14 represents an over-line.
    // ...
    // When the Value of the Attribute Specific Character Set (0008,0005) is either "ISO_IR 13" or
    // "ISO 2022 IR 13", the graphic character represented by the bit combination 05/12 is a "¥" (YEN SIGN)
    // in the character set of ISO-IR 14.
    // https://dicom.nema.org/medical/dicom/current/output/html/part05.html#sect_6.1.2.3
    // https://en.wikipedia.org/wiki/Shift_JIS

    internal sealed class ShiftJisEncoding : Encoding
    {
        private static readonly Encoding s_base = GetEncoding(932);

        public static readonly ShiftJisEncoding Instance = new(null, null);

        public ShiftJisEncoding(EncoderFallback? encoderFallback, DecoderFallback? decoderFallback)
            : base(932, encoderFallback ?? s_base.EncoderFallback, decoderFallback ?? s_base.DecoderFallback)
        { }

        public sealed override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            int charStart = charIndex;
            Span<char> span = chars.AsSpan(charIndex, charCount);
            int byteStart = byteIndex;
            int index;
            while ((index = span.IndexOf('‾')) != -1)
            {
                byteIndex += 1 + s_base.GetBytes(chars, charIndex, index, bytes, byteIndex);
                charIndex += index + 1;
                bytes[byteIndex - 1] = 0x7E;
                span = span.Slice(index + 1);
            }
            byteIndex += s_base.GetBytes(chars, charIndex, charCount - charIndex + charStart, bytes, byteIndex);

            return byteIndex - byteStart;
        }

        public sealed override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            int byteStart = byteIndex;
            Span<byte> span = bytes.AsSpan(byteIndex, byteCount);
            int charStart = charIndex;
            int index;
            while ((index = span.IndexOfAny((byte)0x5C, (byte)0x7E)) != -1)
            {
                charIndex += 1 + s_base.GetChars(bytes, byteIndex, index, chars, charIndex);
                byteIndex += index + 1;
                chars[charIndex - 1] = span[index] == 0x5C ? '¥' : '‾';
                span = span.Slice(index + 1);
            }
            charIndex += s_base.GetChars(bytes, byteIndex, byteCount - byteIndex + byteStart, chars, charIndex);

            return charIndex - charStart;
        }

        public sealed override string BodyName
            => s_base.BodyName;

        public sealed override string EncodingName
            => s_base.EncodingName;

        public sealed override string HeaderName
            => s_base.HeaderName;

        public sealed override string WebName
            => s_base.WebName;

        public sealed override int GetByteCount(char[] chars, int index, int count)
            => s_base.GetByteCount(chars, index, count);

        public sealed override int GetCharCount(byte[] bytes, int index, int count)
            => s_base.GetCharCount(bytes, index, count);

        public sealed override int GetMaxByteCount(int charCount)
            => s_base.GetMaxByteCount(charCount);

        public sealed override int GetMaxCharCount(int byteCount)
            => s_base.GetMaxCharCount(byteCount);
    }
}
