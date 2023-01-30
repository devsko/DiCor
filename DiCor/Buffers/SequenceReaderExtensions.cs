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
    }
}
