using System;
using System.Globalization;

namespace DiCor
{
    public enum DateTimeParts : ushort
    {
        OnlyYear = 0,
        UpToMonth = 1,
        UpToDay = 2,
        UpToHour = 3,
        UpToMinute = 4,
        UpToSecond = 5,
        UpToFraction1 = 6,
        UpToFraction2 = 7,
        UpToFraction3 = 8,
        UpToFraction4 = 9,
        UpToFraction5 = 10,
        UpToFraction6 = 11,
    }
    public readonly struct PartialDateTime
    {
        private readonly struct Union
        {
            private const ushort PartsMask = 0b0000_0000_0001_1111;

            private readonly ushort _value;

            public DateTimeParts Parts
            {
                get => (DateTimeParts)(_value & PartsMask);
                init => _value = (ushort)((ushort)value | (_value & ~PartsMask));
            }
            public short OffsetMinutes
            {
                get => (short)(unchecked((short)_value) >> 5);
                init => _value = (ushort)(value << 5 | (_value & PartsMask));
            }
        }

        private static readonly TimeSpan MaxOffset = TimeSpan.FromHours(12);
        private static readonly TimeSpan MinOffset = TimeSpan.FromHours(-14);

        private readonly DateTime _dateTime;
        private readonly Union _offsetAndFlags;

        public PartialDateTime(DateTime dateTime, TimeSpan offset, DateTimeParts parts = DateTimeParts.UpToFraction6, bool containsUtcOffset = true)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan((ushort)parts, (ushort)DateTimeParts.UpToFraction6, nameof(parts));
            if (containsUtcOffset)
            {
                ArgumentOutOfRangeException.ThrowIfGreaterThan(offset, MaxOffset);
                ArgumentOutOfRangeException.ThrowIfLessThan(offset, MinOffset);

                parts |= (DateTimeParts)0x10;
            }
            else
            {
                offset = default;
            }

            _dateTime = dateTime;
            _offsetAndFlags = new Union { OffsetMinutes = (short)Math.Round(offset.TotalMinutes), Parts = parts, };
        }

        public DateTime DateTime
            => _dateTime;

        public TimeSpan Offset
            => TimeSpan.FromMinutes(_offsetAndFlags.OffsetMinutes);

        public DateTimeParts Parts
            => _offsetAndFlags.Parts & (DateTimeParts)0x0F;

        public bool ContainsUtcOffset
            => (_offsetAndFlags.Parts & (DateTimeParts)0x10) != 0;

        private int StringLength
            => ContainsUtcOffset ? 5 : 0 + Parts switch
            {
                DateTimeParts.OnlyYear => 4,
                DateTimeParts.UpToMonth => 6,
                DateTimeParts.UpToDay => 8,
                DateTimeParts.UpToHour => 10,
                DateTimeParts.UpToMinute => 12,
                DateTimeParts.UpToSecond => 14,
                DateTimeParts.UpToFraction1 => 16,
                DateTimeParts.UpToFraction2 => 17,
                DateTimeParts.UpToFraction3 => 18,
                DateTimeParts.UpToFraction4 => 19,
                DateTimeParts.UpToFraction5 => 20,
                DateTimeParts.UpToFraction6 => 21,
                _ => throw new NotImplementedException(),
            };

        public override string ToString()
            => string.Create(StringLength, this, (span, value) =>
            {
                DateTimeParts parts = value.Parts;
                DateTime dateTime = value.DateTime;
                CultureInfo invariant = CultureInfo.InvariantCulture;

                dateTime.Year.TryFormat(span, out _, "D4", invariant);
                if (parts >= DateTimeParts.UpToMonth)
                {
                    dateTime.Month.TryFormat(span.Slice(4), out _, "D2", invariant);
                    if (parts >= DateTimeParts.UpToDay)
                    {
                        dateTime.Day.TryFormat(span.Slice(6), out _, "D2", invariant);
                        if (parts >= DateTimeParts.UpToHour)
                        {
                            dateTime.Hour.TryFormat(span.Slice(8), out _, "D2", invariant);
                            if (parts >= DateTimeParts.UpToMinute)
                            {
                                dateTime.Minute.TryFormat(span.Slice(10), out _, "D2", invariant);
                                if (parts >= DateTimeParts.UpToSecond)
                                {
                                    dateTime.Second.TryFormat(span.Slice(12), out _, "D2", invariant);
                                    int fractions = parts - DateTimeParts.UpToSecond;
                                    if (fractions > 0)
                                    {
                                        span[14] = '.';
                                        (dateTime.Millisecond * 1000 + dateTime.Microsecond).TryFormat(span.Slice(15), out _, "000000".AsSpan(0, fractions), invariant);
                                    }
                                }
                            }
                        }
                    }
                }
                if (value.ContainsUtcOffset)
                {
                    span = span.Slice(span.Length - 5);
                    TimeSpan offset = value.Offset;
                    span[0] = (offset < TimeSpan.Zero) ? '-' : '+';
                    Math.Abs(offset.Hours).TryFormat(span.Slice(1), out _, "D2", invariant);
                    Math.Abs(offset.Minutes).TryFormat(span.Slice(3), out _, "D2", invariant);
                }
            });
    }
}
