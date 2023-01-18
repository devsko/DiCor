using System.Diagnostics.CodeAnalysis;

namespace DiCor
{
    public partial struct Tag
    {
        public bool Equals(Tag other)
            => Value.Equals(other.Value);

        public override bool Equals([NotNullWhen(true)] object? obj)
            => obj is Tag tag && Equals(tag);

        public override int GetHashCode()
            => Value;

        public static bool operator ==(Tag left, Tag right)
            => left.Equals(right);

        public static bool operator !=(Tag left, Tag right)
            => !left.Equals(right);
    }
}
