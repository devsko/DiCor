using System;
using System.Buffers.Text;
using System.Buffers;
using DiCor.Values;
using System.Diagnostics;

namespace DiCor.Buffers
{
    internal partial struct BufferWriter
    {
        public void Write<TIsQuery>(AEValue<TIsQuery> value)
            where TIsQuery : struct, IIsInQuery
        {
            if (TIsQuery.Value && value.IsEmptyValue)
            {
                Write(Value.DoubleQuotationMark);
            }
            else
            {
                Write(value.Ascii, -1);
            }
        }

        public void Write(ASValue value)
        {
            Age age = value.Age;

            Ensure(4);
            Utf8Formatter.TryFormat(age.Value, Span, out _, new StandardFormat('D', 3));
            Span[3] = (byte)age.Unit;
            Advance(4);
        }

        public void Write(DAValue value)
            => Write(value.Date);

        public void Write(DAQueryValue value)
        {
            if (value.IsSingleDate)
            {
                Write(value.Date);
            }
            else if (value.IsDateRange)
            {
                (DateOnly date1, DateOnly date2) = value.DateRange;
                if (date1 > DateOnly.MinValue)
                {
                    Write(date1);
                }
                Write((byte)'-');
                if (date2 < DateOnly.MaxValue)
                {
                    Write(date2);
                }
            }
            else
            {
                Debug.Assert(value.IsEmptyValue);
                Write(Value.DoubleQuotationMark);
            }
        }
    }
}
