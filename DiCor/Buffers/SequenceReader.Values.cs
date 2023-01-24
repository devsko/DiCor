﻿using System.Buffers.Text;
using DiCor;
using DiCor.Values;

namespace System.Buffers
{
    public static partial class SequenceReaderExtensions
    {
        private static bool IsEmptyValue(ref this SequenceReader<byte> reader, int length)
        {
            if (reader.IsNext(Value.DoubleQuotationMark, true))
            {
                int i = length - 2;
                while (i > 0 && reader.TryRead(out byte b) && b == (byte)' ')
                    i--;

                if (i == 0)
                    return true;

                reader.Rewind(length - i + 1);
            }

            return false;
        }

        public static bool TryReadValue<TIsQueryContext>(ref this SequenceReader<byte> reader, int length, out AEValue<TIsQueryContext> value)
            where TIsQueryContext : struct, IRuntimeConst
        {
            // Short values
            if (reader.Remaining < length)
            {
                value = default;
                return false;
            }

            if (TIsQueryContext.Value && reader.IsEmptyValue(length))
            {
                value = new AEValue<TIsQueryContext>(default(EmptyValue));
                return true;
            }

            reader.TryRead(Math.Min(16, length), out AsciiString ascii);
            value = new AEValue<TIsQueryContext>(ascii);
            return true;
        }

        public static bool TryReadValue(ref this SequenceReader<byte> reader, int length, out ASValue value)
        {
            // Short values
            if (reader.Remaining < length)
            {
                value = default;
                return false;
            }

            scoped ReadOnlySpan<byte> span = reader.UnreadSpan;
            if (span.Length < 4)
            {
                Span<byte> copy = stackalloc byte[4];
                span = copy;
                reader.TryCopyTo(copy);
            }

            AgeUnit unit;
            if (!Utf8Parser.TryParse(span.Slice(0, 3), out short number, out int bytesConsumed) || bytesConsumed != 3 ||
                (unit = ParseAgeUnit(span[3])) == default)
            {
                throw new Exception("invalid format");
            }

            reader.Advance(4);

            value = new ASValue(new Age(number, unit));
            return true;

            static AgeUnit ParseAgeUnit(byte b)
            {
                AgeUnit unit = (AgeUnit)b;
                return Enum.IsDefined(unit) ? unit : default;
            }
        }

        public static bool TryReadValue<TIsQueryContext>(ref this SequenceReader<byte> reader, int length, out DAValue<TIsQueryContext> value)
            where TIsQueryContext : struct, IRuntimeConst
        {
            // Short values
            if (reader.Remaining < length)
            {
                value = default;
                return false;
            }

            DateOnly date1;
            if (!TIsQueryContext.Value)
            {
                reader.TryRead(out date1);
            }
            else
            {
                // PS3.4 - C.2.2.2.5.1 Range Matching of Attributes of VR of DA
                // https://dicom.nema.org/medical/dicom/current/output/html/part04.html#sect_C.2.2.2.5.1

                if (reader.IsEmptyValue(length))
                {
                    value = new DAValue<TIsQueryContext>(new EmptyValue());
                    return true;
                }

                if (reader.IsNext((byte)'-', true))
                {
                    reader.TryRead(out DateOnly date);
                    value = new DAValue<TIsQueryContext>(DateOnly.MinValue, date);
                    return true;
                }

                reader.TryRead(out date1);
                if (reader.IsNext((byte)'-', true))
                {
                    DateOnly date2 = DateOnly.MaxValue;
                    if (reader.Remaining >= 8 && !reader.IsNext((byte)' '))
                    {
                        reader.TryRead(out date2);
                    }
                    value = new DAValue<TIsQueryContext>(date1, date2);
                    return true;
                }
            }

            value = new DAValue<TIsQueryContext>(date1);
            return true;
        }
    }
}
