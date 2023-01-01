using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DiCor
{
    public partial struct Uid
    {
        public static Uid Get(string value)
            => s_uids.TryGetValue(new Uid(value), out Uid uid) ? uid : new Uid(value, string.Empty, UidType.Other);

        public static Uid NewUid(string name = "", UidType type = UidType.SOPInstance)
        {
            // PS3.5 - B.2 UUID Derived UID

            var guid = Guid.NewGuid();
            Span<byte> s = stackalloc byte[16];
            guid.TryWriteBytes(s);

            (s[0], s[1], s[2], s[3], s[4], s[5], s[6], s[7]) = (s[6], s[7], s[4], s[5], s[0], s[1], s[2], s[3]);
            s.Slice(8).Reverse();
            MemoryMarshal.Cast<byte, ulong>(s).Reverse();

            UInt128 intValue = Unsafe.As<byte, UInt128>(ref s[0]);

            Span<char> value = stackalloc char[39 + UUidRoot.Length];
            UUidRoot.CopyTo(value);
            intValue.TryFormat(value.Slice(UUidRoot.Length), out int charsWritten);

            return new Uid(value.Slice(0, charsWritten + UUidRoot.Length).ToString(), name, type);
        }

    }
}
