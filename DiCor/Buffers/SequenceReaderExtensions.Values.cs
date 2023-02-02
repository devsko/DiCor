namespace System.Buffers
{
    internal static partial class SequenceReaderExtensions
    {



        //public static bool TryReadValue(ref this SequenceReader<byte> reader, int length, out ATValue value)
        //{
        //    // Short values
        //    if (reader.Remaining < length)
        //    {
        //        value = default;
        //        return false;
        //    }

        //    reader.TryRead(out Tag tag);

        //    value = new ATValue(tag);
        //    return true;
        //}

        //public static bool TryReadValue(ref this SequenceReader<byte> reader, int length, out DSValue value)
        //{
        //    // Short values
        //    if (reader.Remaining < length)
        //    {
        //        value = default;
        //        return false;
        //    }

        //    reader.TryRead(out decimal @decimal);

        //    value = new DSValue(@decimal);
        //    return true;
        //}
    }
}
