namespace DiCor.Buffers
{
    internal partial struct BufferWriter
    {
        //public void Write<TIsInQuery>(AEValue<TIsInQuery> value)
        //    where TIsInQuery : struct, IIsInQuery
        //{
        //    if (TIsInQuery.Value && value.IsEmptyValue)
        //    {
        //        Write(Value.DoubleQuotationMark);
        //    }
        //    else
        //    {
        //        Write(value.Ascii, -1);
        //    }
        //}

        //public void Write(ASValue value)
        //{
        //    Ensure(4);
        //    Age age = value.Age;
        //    Utf8Formatter.TryFormat(age.Value, Span, out _, new StandardFormat('D', 3));
        //    Span[3] = (byte)age.Unit;
        //    Advance(4);
        //}

        //public unsafe void Write(ATValue value)
        //    => Write(value.Tag);

        //public void Write(DAValue value)
        //    => Write(value.Date);

        //public void Write(DAQueryValue value)
        //{
        //    if (value.IsSingleDate)
        //    {
        //        Write(value.Date);
        //    }
        //    else if (value.IsDateRange)
        //    {
        //        (DateOnly date1, DateOnly date2) = value.DateRange;
        //        if (date1 > DateOnly.MinValue)
        //        {
        //            Write(date1);
        //        }
        //        Write((byte)'-');
        //        if (date2 < DateOnly.MaxValue)
        //        {
        //            Write(date2);
        //        }
        //    }
        //    else
        //    {
        //        Debug.Assert(value.IsEmptyValue);
        //        Write(Value.DoubleQuotationMark);
        //    }
        //}

        //public void Write(DSValue value)
        //    => Write(value.Decimal);
    }
}
