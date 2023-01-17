using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;

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

        unsafe public override int GetHashCode()
        {
            byte[] value = Value;
            if (value is null)
            {
                return 0;
            }
            int length = value.Length;
            fixed (byte* src = &MemoryMarshal.GetArrayDataReference(value))
            {
                uint hash1 = (5381 << 16) + 5381;
                uint hash2 = hash1;

                uint* ptrUInt32 = (uint*)src;
                while (length > 7)
                {
                    hash1 = BitOperations.RotateLeft(hash1, 5) + hash1 ^ ptrUInt32[0];
                    hash2 = BitOperations.RotateLeft(hash2, 5) + hash2 ^ ptrUInt32[1];
                    ptrUInt32 += 2;
                    length -= 8;
                }

                byte* ptrByte = (byte*)ptrUInt32;
                while (length-- > 0)
                {
                    hash2 = BitOperations.RotateLeft(hash2, 5) + hash2 ^ *ptrByte++;
                }

                return (int)(hash1 + (hash2 * 1_566_083_941));
            }
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
