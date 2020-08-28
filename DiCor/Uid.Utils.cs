using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace DiCor
{
    public partial struct Uid
    {
        public static Uid Get(string value)
            => s_uids.TryGetValue(new Uid(value), out Uid uid)
                ? uid
                : new Uid(value, string.Empty, UidType.Other);

        public static Uid NewUid(string name = "", UidType type = UidType.SOPInstance)
        {
            Span<byte> span = stackalloc byte[16];
            Guid.NewGuid().TryWriteBytes(span);
            Swap(ref span[7], ref span[6]);
            Swap(ref span[5], ref span[4]);
            Swap(ref span[3], ref span[0]);
            Swap(ref span[1], ref span[2]);

            return new Uid("2.25." + new BigInteger(span, isUnsigned: true, isBigEndian: true).ToString(), name, type);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Swap(ref byte b1, ref byte b2)
        {
            byte temp = b2;
            b2 = b1;
            b1 = temp;
        }
    }
}
