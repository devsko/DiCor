using System.Diagnostics;
using DiCor.Values;

namespace System.Buffers
{
    public static partial class SequenceReaderExtensions
    {
        public static bool TryReadValue<TIsQueryContext>(ref this SequenceReader<byte> reader, int length, out DAValue<TIsQueryContext> value)
            where TIsQueryContext : struct, IRuntimeConst
        {
            Debug.Assert(length <= 18 && length >= 8);

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

                if (reader.IsNext(Value.DoubleQuotationMark, true))
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
