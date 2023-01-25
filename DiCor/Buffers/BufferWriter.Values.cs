using System;
using System.Buffers.Text;
using System.Buffers;
using DiCor.Values;

namespace DiCor.Buffers
{
    public partial struct BufferWriter
    {
        public void Write<TIsQueryContext>(AEValue<TIsQueryContext> value)
            where TIsQueryContext : struct, IIsQueryContext
        {
            if (TIsQueryContext.Value && value.IsEmptyValue)
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

        public void Write<TIsQueryContext>(DAValue<TIsQueryContext> value)
            where TIsQueryContext : struct, IIsQueryContext
        {
            if (!TIsQueryContext.Value || value.IsSingleDate)
            {
                Write(value.Date);
            }
            else
            {
                if (value.IsEmptyValue)
                {
                    Write(Value.DoubleQuotationMark);
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
            }
        }
    }
}
