using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace DiCor
{
    internal enum VRCode : ushort
    {
        AE = 0x4541,
        AS = 0x5341,
        AT = 0x5441,
        CS = 0x5343,
        DA = 0x4144,
        DS = 0x5344,
        DT = 0x5444,
        FD = 0x0000,
        FL = 0x0000,
        IS = 0x0000,
        LO = 0x4F4C,
        LT = 0x544C,
        OB = 0x424F,
        OD = 0x0000,
        OF = 0x0000,
        OL = 0x0000,
        OV = 0x0000,
        OW = 0x0000,
        PN = 0x4E50,
        SH = 0x4853,
        SL = 0x0000,
        SQ = 0x5153,
        SS = 0x0000,
        ST = 0x5453,
        SV = 0x0000,
        TM = 0x4D54,
        UC = 0x0000,
        UI = 0x4955,
        UL = 0x4C55,
        UN = 0x0000,
        UR = 0x0000,
        US = 0x0000,
        UT = 0x0000,
        UV = 0x0000,
    }

    public readonly partial struct VR : IEquatable<VR>
    {
        ///<summary>Application Entity</summary>
        public static readonly VR AE = new(VRCode.AE);
        ///<summary>Age String</summary>
        public static readonly VR AS = new(VRCode.AS);
        ///<summary>Attribute Tag</summary>
        public static readonly VR AT = new(VRCode.AT);
        ///<summary>Code String</summary>
        public static readonly VR CS = new(VRCode.CS);
        ///<summary>Date</summary>
        public static readonly VR DA = new(VRCode.DA);
        ///<summary>Decimal String</summary>
        public static readonly VR DS = new(VRCode.DS);
        ///<summary>Date Time</summary>
        public static readonly VR DT = new(VRCode.DT);
        ///<summary>Floating Point Double</summary>
        public static readonly VR FD = new(VRCode.FD);
        ///<summary>Floating Point Single</summary>
        public static readonly VR FL = new(VRCode.FL);
        ///<summary>Integer String</summary>
        public static readonly VR IS = new(VRCode.IS);
        ///<summary>Long String</summary>
        public static readonly VR LO = new(VRCode.LO);
        ///<summary>Long Text</summary>
        public static readonly VR LT = new(VRCode.LT);
        ///<summary>Other Byte</summary>
        public static readonly VR OB = new(VRCode.OB);
        ///<summary>Other Double</summary>
        public static readonly VR OD = new(VRCode.OD);
        ///<summary>Other Float</summary>
        public static readonly VR OF = new(VRCode.OF);
        ///<summary>Other Long</summary>
        public static readonly VR OL = new(VRCode.OL);
        ///<summary>Other 64-bit Very Long</summary>
        public static readonly VR OV = new(VRCode.OV);
        ///<summary>Other Word</summary>
        public static readonly VR OW = new(VRCode.OW);
        ///<summary>Person Name</summary>
        public static readonly VR PN = new(VRCode.PN);
        ///<summary>Short String</summary>
        public static readonly VR SH = new(VRCode.SH);
        ///<summary>Signed Long</summary>
        public static readonly VR SL = new(VRCode.SL);
        ///<summary>Sequence of Items</summary>
        public static readonly VR SQ = new(VRCode.SQ);
        ///<summary>Signed Short</summary>
        public static readonly VR SS = new(VRCode.SS);
        ///<summary>Short Text</summary>
        public static readonly VR ST = new(VRCode.ST);
        ///<summary>Signed 64-bit Very Long</summary>
        public static readonly VR SV = new(VRCode.SV);
        ///<summary>Time</summary>
        public static readonly VR TM = new(VRCode.TM);
        ///<summary>Unlimited Characters</summary>
        public static readonly VR UC = new(VRCode.UC);
        ///<summary>Unique Identifier (UID)</summary>
        public static readonly VR UI = new(VRCode.UI);
        ///<summary>Unsigned Long</summary>
        public static readonly VR UL = new(VRCode.UL);
        ///<summary>Unknown</summary>
        public static readonly VR UN = new(VRCode.UN);
        ///<summary>Universal Resource Identifier or Universal Resource Locator (URI/URL)</summary>
        public static readonly VR UR = new(VRCode.UR);
        ///<summary>Unsigned Short</summary>
        public static readonly VR US = new(VRCode.US);
        ///<summary>Unlimited Text</summary>
        public static readonly VR UT = new(VRCode.UT);
        ///<summary>Unsigned 64-bit Very Long</summary>
        public static readonly VR UV = new(VRCode.UV);

        private readonly VRCode _code;

        internal VR(VRCode code)
        {
            Debug.Assert(Enum.IsDefined(code));

            _code = code;
        }

        internal VR(ReadOnlySpan<byte> value)
        {
            Debug.Assert(value.Length == 2);

            _code = (VRCode)Unsafe.ReadUnaligned<ushort>(ref MemoryMarshal.GetReference(value));
        }

        internal VRCode Code
            => _code;

        public bool Equals(VR other)
            => _code == other._code;

        public override bool Equals([NotNullWhen(true)] object? obj)
            => obj is VR other && Equals(other);

        public override int GetHashCode()
            => (ushort)_code;

        public static bool operator ==(VR left, VR right)
            => left.Equals(right);

        public static bool operator !=(VR left, VR right)
            => !left.Equals(right);

        public override string ToString()
            => string.Create(2, _code,
                static (span, value) => Ascii.ToUtf16(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref value, 1)), span, out _));
    }
}
