using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace DiCor
{
    public readonly partial struct VR : IEquatable<VR>
    {
        ///<summary>Application Entity</summary>
        public static readonly VR AE = new("AE"u8);
        ///<summary>Age String</summary>
        public static readonly VR AS = new("AS"u8);
        ///<summary>Attribute Tag</summary>
        public static readonly VR AT = new("AT"u8);
        ///<summary>Code String</summary>
        public static readonly VR CS = new("CS"u8);
        ///<summary>Date</summary>
        public static readonly VR DA = new("DA"u8);
        ///<summary>Decimal String</summary>
        public static readonly VR DS = new("DS"u8);
        ///<summary>Date Time</summary>
        public static readonly VR DT = new("DT"u8);
        ///<summary>Floating Point Single</summary>
        public static readonly VR FL = new("FL"u8);
        ///<summary>Floating Point Double</summary>
        public static readonly VR FD = new("FD"u8);
        ///<summary>Integer String</summary>
        public static readonly VR IS = new("IS"u8);
        ///<summary>Long String</summary>
        public static readonly VR LO = new("LO"u8);
        ///<summary>Long Text</summary>
        public static readonly VR LT = new("LT"u8);
        ///<summary>Other Byte String</summary>
        public static readonly VR OB = new("OB"u8);
        ///<summary>Other Double String</summary>
        public static readonly VR OD = new("OD"u8);
        ///<summary>Other Float String</summary>
        public static readonly VR OF = new("OF"u8);
        ///<summary>Other Long</summary>
        public static readonly VR OL = new("OL"u8);
        ///<summary>Other 64-bit Very Long</summary>
        public static readonly VR OV = new("OV"u8);
        ///<summary>Other Word String</summary>
        public static readonly VR OW = new("OW"u8);
        ///<summary>Person Name</summary>
        public static readonly VR PN = new("PN"u8);
        ///<summary>Short String</summary>
        public static readonly VR SH = new("SH"u8);
        ///<summary>Signed Long</summary>
        public static readonly VR SL = new("SL"u8);
        ///<summary>Sequence of Items</summary>
        public static readonly VR SQ = new("SQ"u8);
        ///<summary>Signed Short</summary>
        public static readonly VR SS = new("SS"u8);
        ///<summary>Short Text</summary>
        public static readonly VR ST = new("ST"u8);
        ///<summary>Signed 64-bit Very Long</summary>
        public static readonly VR SV = new("SV"u8);
        ///<summary>Time</summary>
        public static readonly VR TM = new("TM"u8);
        ///<summary>Unlimited Characters</summary>
        public static readonly VR UC = new("UC"u8);
        ///<summary>Unique Identifier (UID)</summary>
        public static readonly VR UI = new("UI"u8);
        ///<summary>Unsigned Long</summary>
        public static readonly VR UL = new("UL"u8);
        ///<summary>Unknown</summary>
        public static readonly VR UN = new("UN"u8);
        ///<summary>Universal Resource Identifier or Universal Resource Locator (URI/URL)</summary>
        public static readonly VR UR = new("UR"u8);
        ///<summary>Unsigned Short</summary>
        public static readonly VR US = new("US"u8);
        ///<summary>Unlimited Text</summary>
        public static readonly VR UT = new("UT"u8);
        ///<summary>Unsigned 64-bit Very Long</summary>
        public static readonly VR UV = new("UV"u8);

        private readonly ushort _value;

        public VR(ReadOnlySpan<byte> value)
        {
            if (value.Length != sizeof(short))
                Throw();

            _value = Unsafe.ReadUnaligned<ushort>(ref MemoryMarshal.GetReference(value));

            [DoesNotReturn]
            [StackTraceHidden]
            static void Throw()
                => throw new ArgumentOutOfRangeException(nameof(value), "The given value must have a length of 2.");
        }

        public bool Equals(VR other)
            => _value == other._value;

        public override bool Equals([NotNullWhen(true)] object? obj)
            => obj is VR other && Equals(other);

        public override int GetHashCode()
            => _value;

        public static bool operator ==(VR left, VR right)
            => left.Equals(right);

        public static bool operator !=(VR left, VR right)
            => !left.Equals(right);

        public override string ToString()
            => string.Create(2, _value,
                static (span, value) => Ascii.ToUtf16(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref value, 1)), span, out _));
    }
}
