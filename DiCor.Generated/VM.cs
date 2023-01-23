using System;
using System.Globalization;

namespace DiCor
{
    public readonly struct VM
    {
        public byte Min { get; }

        public byte Max { get; }

        public byte Step { get; }

        public bool IsUnbounded
            => Max == 0;

        public VM(byte count)
            : this(count, count, 0)
        { }

        public VM(byte min, byte max)
            : this(min, max, 1)
        { }

        public VM(byte min, byte max, byte step = 1)
        {
            if (max != 0)
                ArgumentOutOfRangeException.ThrowIfLessThan(max, min);

            Min = min;
            Max = max;
            Step = step;
        }

        public override string ToString()
        {
            if (Min == Max)
                return Min.ToString(CultureInfo.InvariantCulture);

            Span<char> buffer = stackalloc char[8];
            if (IsUnbounded)
                if (Step == 1)
                    return string.Create(CultureInfo.InvariantCulture, buffer, $"{Min}-n");
                else
                    return string.Create(CultureInfo.InvariantCulture, buffer, $"{Min}-{Step}n");

            return string.Create(null, buffer, $"{Min}-{Max}");
        }
    }
}
