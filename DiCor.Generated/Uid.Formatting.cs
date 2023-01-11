using System;
using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DiCor
{
    partial struct Uid
    {
        private static unsafe bool TryUInt128ToDecStr(UInt128 value, Span<byte> destination, out int charsWritten)
        {
            int countedDigits = CountDigits(value);
            int bufferLength = countedDigits;
            if (bufferLength <= destination.Length)
            {
                charsWritten = bufferLength;
                fixed (byte* buffer = &MemoryMarshal.GetReference(destination))
                {
                    byte* p = buffer + bufferLength;
                    p = UInt128ToDecChars(p, value);
                }
                return true;
            }

            charsWritten = 0;
            return false;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe byte* UInt128ToDecChars(byte* bufferEnd, UInt128 value)
        {
            while ((ulong)(value >> 64) != 0)
            {
                bufferEnd = UInt64ToDecChars(bufferEnd, Int128DivMod1E19(ref value), 19);
            }
            return UInt64ToDecChars(bufferEnd, (ulong)value);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe byte* UInt64ToDecChars(byte* bufferEnd, ulong value)
        {
            if (value >= 10)
            {
                // Handle all values >= 100 two-digits at a time so as to avoid expensive integer division operations.
                while (value >= 100)
                {
                    bufferEnd -= 2;
                    (value, ulong remainder) = Math.DivRem(value, 100);
                    WriteTwoDigits(bufferEnd, (uint)remainder);
                }

                // If there are two digits remaining, store them.
                if (value >= 10)
                {
                    bufferEnd -= 2;
                    WriteTwoDigits(bufferEnd, (uint)value);
                    return bufferEnd;
                }
            }

            // Otherwise, store the single digit remaining.
            *(--bufferEnd) = (byte)(value + '0');
            return bufferEnd;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe byte* UInt64ToDecChars(byte* bufferEnd, ulong value, int digits)
        {
            ulong remainder;
            while (value >= 100)
            {
                bufferEnd -= 2;
                digits -= 2;
                (value, remainder) = Math.DivRem(value, 100);
                WriteTwoDigits(bufferEnd, (uint)remainder);
            }

            while (value != 0 || digits > 0)
            {
                digits--;
                (value, remainder) = Math.DivRem(value, 10);
                *(--bufferEnd) = (byte)(remainder + '0');
            }

            return bufferEnd;
        }
        private static ReadOnlySpan<byte> TwoDigitsBytes =>
            "00010203040506070809"u8 +
            "10111213141516171819"u8 +
            "20212223242526272829"u8 +
            "30313233343536373839"u8 +
            "40414243444546474849"u8 +
            "50515253545556575859"u8 +
            "60616263646566676869"u8 +
            "70717273747576777879"u8 +
            "80818283848586878889"u8 +
            "90919293949596979899"u8;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe void WriteTwoDigits(byte* ptr, uint value)
        {
            Unsafe.WriteUnaligned(ptr,
                Unsafe.ReadUnaligned<ushort>(
                    ref Unsafe.Add(ref MemoryMarshal.GetReference(TwoDigitsBytes), (int)value * 2)));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong Int128DivMod1E19(ref UInt128 value)
        {
            UInt128 divisor = new(0, 10_000_000_000_000_000_000);
            (value, UInt128 remainder) = UInt128.DivRem(value, divisor);
            return (ulong)remainder;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CountDigits(UInt128 value)
        {
            ulong upper = (ulong)(value >> 64);

            // 1e19 is    8AC7_2304_89E8_0000
            // 1e20 is  5_6BC7_5E2D_6310_0000
            // 1e21 is 36_35C9_ADC5_DEA0_0000

            if (upper == 0)
            {
                // We have less than 64-bits, so just return the lower count
                return CountDigits((ulong)value);
            }

            // We have more than 1e19, so we have at least 20 digits
            int digits = 20;

            if (upper > 5)
            {
                // ((2^128) - 1) / 1e20 < 34_02_823_669_209_384_635 which
                // is 18.5318 digits, meaning the result definitely fits
                // into 64-bits and we only need to add the lower digit count

                value /= new UInt128(0x5, 0x6BC7_5E2D_6310_0000); // value /= 1e20

                digits += CountDigits((ulong)value);
            }
            else if ((upper == 5) && ((ulong)value >= 0x6BC75E2D63100000))
            {
                // We're greater than 1e20, but definitely less than 1e21
                // so we have exactly 21 digits

                digits++;
            }

            return digits;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CountDigits(ulong value)
        {
            // Map the log2(value) to a power of 10.
            ReadOnlySpan<byte> log2ToPow10 = new byte[]
            {
                1,  1,  1,  2,  2,  2,  3,  3,  3,  4,  4,  4,  4,  5,  5,  5,
                6,  6,  6,  7,  7,  7,  7,  8,  8,  8,  9,  9,  9,  10, 10, 10,
                10, 11, 11, 11, 12, 12, 12, 13, 13, 13, 13, 14, 14, 14, 15, 15,
                15, 16, 16, 16, 16, 17, 17, 17, 18, 18, 18, 19, 19, 19, 19, 20
            };
            uint index = Unsafe.Add(ref MemoryMarshal.GetReference(log2ToPow10), BitOperations.Log2(value));

            // TODO https://github.com/dotnet/runtime/issues/60948: Use ReadOnlySpan<ulong> instead of ReadOnlySpan<byte>.
            // Read the associated power of 10.
            ReadOnlySpan<byte> powersOf10 = new byte[]
            {
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // unused entry to avoid needing to subtract
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // 0
                0x0A, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // 10
                0x64, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // 100
                0xE8, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // 1000
                0x10, 0x27, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // 10000
                0xA0, 0x86, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, // 100000
                0x40, 0x42, 0x0F, 0x00, 0x00, 0x00, 0x00, 0x00, // 1000000
                0x80, 0x96, 0x98, 0x00, 0x00, 0x00, 0x00, 0x00, // 10000000
                0x00, 0xE1, 0xF5, 0x05, 0x00, 0x00, 0x00, 0x00, // 100000000
                0x00, 0xCA, 0x9A, 0x3B, 0x00, 0x00, 0x00, 0x00, // 1000000000
                0x00, 0xE4, 0x0B, 0x54, 0x02, 0x00, 0x00, 0x00, // 10000000000
                0x00, 0xE8, 0x76, 0x48, 0x17, 0x00, 0x00, 0x00, // 100000000000
                0x00, 0x10, 0xA5, 0xD4, 0xE8, 0x00, 0x00, 0x00, // 1000000000000
                0x00, 0xA0, 0x72, 0x4E, 0x18, 0x09, 0x00, 0x00, // 10000000000000
                0x00, 0x40, 0x7A, 0x10, 0xF3, 0x5A, 0x00, 0x00, // 100000000000000
                0x00, 0x80, 0xC6, 0xA4, 0x7E, 0x8D, 0x03, 0x00, // 1000000000000000
                0x00, 0x00, 0xC1, 0x6F, 0xF2, 0x86, 0x23, 0x00, // 10000000000000000
                0x00, 0x00, 0x8A, 0x5D, 0x78, 0x45, 0x63, 0x01, // 100000000000000000
                0x00, 0x00, 0x64, 0xA7, 0xB3, 0xB6, 0xE0, 0x0D, // 1000000000000000000
                0x00, 0x00, 0xE8, 0x89, 0x04, 0x23, 0xC7, 0x8A, // 10000000000000000000
            };
            ulong powerOf10 = Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref MemoryMarshal.GetReference(powersOf10), index * sizeof(ulong)));
            if (!BitConverter.IsLittleEndian)
            {
                powerOf10 = BinaryPrimitives.ReverseEndianness(powerOf10);
            }

            // Return the number of digits based on the power of 10, shifted by 1
            // if it falls below the threshold.
            bool lessThan = value < powerOf10;
            return (int)(index - Unsafe.As<bool, byte>(ref lessThan)); // while arbitrary bools may be non-0/1, comparison operators are expected to return 0/1
        }

    }
}
