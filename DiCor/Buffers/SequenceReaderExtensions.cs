using DiCor.Values;

namespace System.Buffers
{
    internal static partial class SequenceReaderExtensions
    {
        public static bool TryAdvance(this ref SequenceReader<byte> reader, long count)
        {
            if (reader.Remaining < count)
                return false;

            reader.Advance(count);

            return true;
        }

        public static bool IsQueryEmptyValue(this ref SequenceReader<byte> reader)
            => reader.Remaining == 2 && reader.IsNext(Value.DoubleQuotationMark, true);
    }
}
