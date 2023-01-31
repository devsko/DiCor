using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace DiCor
{
    public enum TimeOnlyParts
    {
        OnlyHour,
        UpToMinute,
        UpToSecond,
        UpToFraction1,
        UpToFraction2,
        UpToFraction3,
        UpToFraction4,
        UpToFraction5,
        UpToFraction6,
    }

    public readonly struct PartialTimeOnly : IComparable<PartialTimeOnly>, IEquatable<PartialTimeOnly>
    {
        private const ulong TimeMask = 0xFF_FFFF_FFFF;

        private readonly ulong _value;

        public PartialTimeOnly(TimeOnly time, TimeOnlyParts parts = TimeOnlyParts.UpToFraction6)
        {
            Time = time;
            Parts = parts;
        }

        public TimeOnly Time
        {
            get
            {
                ulong time = _value & TimeMask;
                return Unsafe.As<ulong, TimeOnly>(ref time);
            }
            init
                => _value |= Unsafe.As<TimeOnly, ulong>(ref value);
        }

        public TimeOnlyParts Parts
        {
            get => (TimeOnlyParts)(_value >> 40);
            init => _value |= ((ulong)value << 40);
        }

        private int StringLength
            => Parts switch
            {
                TimeOnlyParts.OnlyHour => 2,
                TimeOnlyParts.UpToMinute => 4,
                TimeOnlyParts.UpToSecond => 6,
                TimeOnlyParts.UpToFraction1 => 8,
                TimeOnlyParts.UpToFraction2 => 9,
                TimeOnlyParts.UpToFraction3 => 10,
                TimeOnlyParts.UpToFraction4 => 11,
                TimeOnlyParts.UpToFraction5 => 12,
                TimeOnlyParts.UpToFraction6 => 13,
                _ => throw new NotImplementedException(),
            };

        public int CompareTo(PartialTimeOnly other)
            => Time.CompareTo(other.Time);

        public bool Equals(PartialTimeOnly other)
            => Time.Equals(other.Time) && Parts.Equals(other.Parts);

        public override bool Equals([NotNullWhen(true)] object? obj)
            => obj is PartialTimeOnly time && Equals(time);

        public override int GetHashCode()
            => Time.GetHashCode();

        public override string ToString()
            => string.Create(StringLength, this, (span, value) =>
            {
                TimeOnlyParts parts = value.Parts;
                TimeOnly time = value.Time;
                CultureInfo invariant = CultureInfo.InvariantCulture;

                time.Hour.TryFormat(span, out _, "D2", invariant);
                if (parts >= TimeOnlyParts.UpToMinute)
                {
                    time.Minute.TryFormat(span.Slice(2), out _, "D2", invariant);
                    if (parts >= TimeOnlyParts.UpToSecond)
                    {
                        time.Second.TryFormat(span.Slice(4), out _, "D2", invariant);
                        int fractions = parts - TimeOnlyParts.UpToSecond;
                        if (fractions > 0)
                        {
                            span[6] = '.';
                            Span<char> format = stackalloc char[2];
                            format[0] = 'D';
                            format[1] = (char)('0' + fractions);
                            (time.Millisecond * 1000 + time.Microsecond).TryFormat(span.Slice(7), out _, format, invariant);
                        }
                    }
                }
            });
    }
}
