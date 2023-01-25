using System;
using System.Globalization;

namespace DiCor
{
    public enum AgeUnit : byte
    {
        Days = (byte)'D',
        Weeks = (byte)'W',
        Months = (byte)'M',
        Years = (byte)'Y',
    }
    public readonly struct Age
    {
        private readonly short _value;
        private readonly AgeUnit _unit;

        public short Value
            => _value;

        public AgeUnit Unit
            => _unit;

        public Age(short value, AgeUnit unit)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 999);
            ArgumentOutOfRangeException.ThrowIfGreaterThan((byte)unit, (byte)AgeUnit.Years, nameof(unit));

            (_value, _unit) = (value, unit);
        }

        public override string ToString()
            => string.Create(CultureInfo.InvariantCulture, stackalloc char[4], $"{_value:D3}{_unit,1}");
    }
}
