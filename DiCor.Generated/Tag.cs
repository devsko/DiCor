using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace DiCor
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public readonly partial struct Tag : IEquatable<Tag>
    {
        private readonly int _value;

        public ushort Group
            => (ushort)(_value >> 16);

        public ushort Element
            => unchecked((ushort)_value);

        internal int Value
            => _value;

        public Tag(ushort group, ushort element)
        {
            _value = group << 16 | element;
        }

        public bool Equals(Tag other)
            => _value == other._value;

        public override bool Equals([NotNullWhen(true)] object? obj)
            => obj is Tag other && Equals(other);

        public override int GetHashCode()
            => _value;

        public static bool operator ==(Tag left, Tag right)
            => left.Equals(right);

        public static bool operator !=(Tag left, Tag right)
            => !left.Equals(right);

        public override string ToString()
            => string.Create(CultureInfo.InvariantCulture, stackalloc char[11], $"[{Group:X4},{Element:X4}]");

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string? DebuggerDisplay
        {
            get
            {
                Details? details = GetDetails();

                if (details is null)
                    return $"? {this}";

                return $"{(details.IsRetired ? "RETIRED " : "")} {this} {details.Name} VM={details.VM}";
            }
        }
    }
}
