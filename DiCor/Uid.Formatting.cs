using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DiCor
{
    public partial struct Uid
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

            // TODO: Replace with log2ToPow10[BitOperations.Log2(value)] once https://github.com/dotnet/runtime/issues/79257 is fixed
            uint index = Unsafe.Add(ref MemoryMarshal.GetReference(log2ToPow10), BitOperations.Log2(value));

            // Read the associated power of 10.
            ReadOnlySpan<ulong> powersOf10 = new ulong[]
            {
                0, // unused entry to avoid needing to subtract
                0,
                10,
                100,
                1000,
                10000,
                100000,
                1000000,
                10000000,
                100000000,
                1000000000,
                10000000000,
                100000000000,
                1000000000000,
                10000000000000,
                100000000000000,
                1000000000000000,
                10000000000000000,
                100000000000000000,
                1000000000000000000,
                10000000000000000000,
            };
            ulong powerOf10 = Unsafe.Add(ref MemoryMarshal.GetReference(powersOf10), index);

            // Return the number of digits based on the power of 10, shifted by 1
            // if it falls below the threshold.
            bool lessThan = value < powerOf10;
            return (int)(index - Unsafe.As<bool, byte>(ref lessThan)); // while arbitrary bools may be non-0/1, comparison operators are expected to return 0/1
        }

    }
}
