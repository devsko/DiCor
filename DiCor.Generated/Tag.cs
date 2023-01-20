using System;
using System.Globalization;

namespace DiCor
{
    public readonly partial struct Tag : IEquatable<Tag>
    {
        internal int Value { get; }

        public ushort Group => (ushort)(Value >> 16);

        public ushort Element => (ushort)(Value & 0x00FF);

        public Tag(ushort group, ushort element)
        {
            Value = group << 16 | element;
        }

        public override string ToString()
            => string.Create(CultureInfo.InvariantCulture, stackalloc char[11], $"[{Group:X4},{Element:X4}]");
    }
}
