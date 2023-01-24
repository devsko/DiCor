using System;
using System.Buffers.Text;
using System.Buffers;
using DiCor.Values;

namespace DiCor.Buffers
{
    public partial struct BufferWriter
    {
        public void Write<TIsQueryContext>(AEValue<TIsQueryContext> value)
            where TIsQueryContext : struct, IRuntimeConst
        {
            if (TIsQueryContext.Value && value.IsEmptyValue)
            {
                Write(Value.DoubleQuotationMark);
            }
            else
            {
                Write(value.Get<AsciiString>(), -1);
            }
        }

        public void Write(ASValue value)
        {
            Age age = value.Get<Age>();

            Ensure(4);
            Utf8Formatter.TryFormat(age.Value, Span, out _, new StandardFormat('D', 3));
            Span[3] = (byte)age.Unit;
            Advance(4);
        }

        public void Write<TIsQueryContext>(DAValue<TIsQueryContext> value)
            where TIsQueryContext : struct, IRuntimeConst
        {
            if (!TIsQueryContext.Value || value.IsSingleValue)
            {
                Write(value.Get<DateOnly>());
            }
            else
            {
                if (value.IsEmptyValue)
                {
                    Write(Value.DoubleQuotationMark);
                }
                else if (value.IsRange)
                {
                    (DateOnly date1, DateOnly date2) = value.Get<(DateOnly, DateOnly)>();
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
