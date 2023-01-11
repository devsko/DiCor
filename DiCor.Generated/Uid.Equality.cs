using System;
using System.Diagnostics.CodeAnalysis;

namespace DiCor
{
    partial struct Uid
    {
        public bool Equals(Uid other)
        {
            return object.ReferenceEquals(Value, other.Value) || Value.AsSpan().SequenceEqual(other.Value.AsSpan());
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is Uid uid && Equals(uid);
        }

        public override int GetHashCode()
        {
            HashCode hash = new();
            hash.AddBytes(Value.AsSpan());

            return hash.ToHashCode();
        }

        public static bool operator ==(Uid left, Uid right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Uid left, Uid right)
        {
            return !left.Equals(right);
        }
    }
}
