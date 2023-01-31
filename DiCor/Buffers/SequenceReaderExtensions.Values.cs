namespace System.Buffers
{
    internal static partial class SequenceReaderExtensions
    {


        //public static bool TryReadValue(ref this SequenceReader<byte> reader, int length, out ASValue value)
        //{
        //    // Short values
        //    if (reader.Remaining < length)
        //    {
        //        value = default;
        //        return false;
        //    }

        //    scoped ReadOnlySpan<byte> span = reader.UnreadSpan;
        //    if (span.Length < 4)
        //    {
        //        Span<byte> copy = stackalloc byte[4];
        //        span = copy;
        //        reader.TryCopyTo(copy);
        //    }

        //    AgeUnit unit;
        //    if (!Utf8Parser.TryParse(span.Slice(0, 3), out short number, out int bytesConsumed) || bytesConsumed != 3 ||
        //        (unit = ParseAgeUnit(span[3])) == default)
        //    {
        //        throw new Exception("invalid format");
        //    }

        //    reader.Advance(4);

        //    value = new ASValue(new Age(number, unit));
        //    return true;

        //    static AgeUnit ParseAgeUnit(byte b)
        //    {
        //        AgeUnit unit = (AgeUnit)b;
        //        return Enum.IsDefined(unit) ? unit : default;
        //    }
        //}

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
