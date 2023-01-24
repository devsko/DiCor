using System;
using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DiCor
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public readonly partial struct Uid : IEquatable<Uid>
    {
        public AsciiString Value { get; }

        public Uid(AsciiString value, bool validate = true)
        {
            Value = value;
            if (validate & !IsValid)
                Throw(value);

            [DoesNotReturn]
            [StackTraceHidden]
            static void Throw(AsciiString value)
                => throw new ArgumentException($"{value} is not a valid UID.", nameof(value));
        }

        // PS 3.5 - 9.1 UID Encoding Rules
        private static readonly IndexOfAnyValues<byte> s_validBytes = IndexOfAnyValues.Create(".0123456789"u8);
        public bool IsValid
        {
            get
            {
                ReadOnlySpan<byte> value = Value.Bytes;
                if (value.Length is 0 or > 64 || value.IndexOfAnyExcept(s_validBytes) != -1)
                    return false;

                int index;
                while ((index = value.IndexOf((byte)'.')) != -1)
                {
                    if (!IsValidComponent(value.Slice(0, index)))
                        return false;

                    value = value.Slice(index + 1);
                }

                return IsValidComponent(value);

                static bool IsValidComponent(ReadOnlySpan<byte> component)
                    => component.Length > 0 && (component.Length == 1 || component[0] != (byte)'0');
            }
        }

        public bool IsDicomDefined
            => Value.Bytes.StartsWith(DicomOrgRoot);

        public bool Equals(Uid other)
            => Value.Equals(other.Value);

        public override bool Equals([NotNullWhen(true)] object? obj)
            => obj is Uid uid && Equals(uid);

        public override unsafe int GetHashCode()
            => Value.GetHashCode();

        public static bool operator ==(Uid left, Uid right)
            => left.Equals(right);

        public static bool operator !=(Uid left, Uid right)
            => !left.Equals(right);

        public override string ToString()
            => string.Create(CultureInfo.InvariantCulture, stackalloc char[Value.Length + 2], $"[{Value}]");

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string? DebuggerDisplay
        {
            get
            {
                if (!IsValid)
                    return $"INVALID {this}";

                if (!IsKnown(out Details? details))
                    return $"? {this}";

                return $"{(details.IsRetired ? "RETIRED " : "")} {this} {details.Type}: {details.Name}";
            }
        }

        // PS3.5 - 9 Unique Identifiers (UIDs)
        public static ReadOnlySpan<byte> DicomOrgRoot
            => "1.2.840.10008."u8;

        // Dear ...
        // I have pleasure in enclosing your UID prefix as requested.It is:
        // 1.2.826.0.1.3680043.10.386
        // You are hereby granted full permission to use this UID prefix as your own subject only to the following minimal conditions:
        // 1) You agree to operate a policy to ensure that UIDs subordinate to this prefix cannot be duplicated.
        // 2) You may sub-delegate ranges using your prefix, providing this is either on a not-for-profit basis, or as part of another product. i.e.you may not sell numbering space itself
        // I hope that this facility is useful to you, but if you have any questions, please do not hesitate to contact us.
        // Yours sincerely
        // The Medical Connections Team
        // Medical Connections Ltd
        // www.medicalconnections.co.uk
        // Company Tel: +44-1792-390209
        // Medical Connections Ltd is registered in England & Wales as Company Number 3680043 Medical Connections Ltd, Suite 10, Henley House, Queensway, Fforestfach, Swansea, SA5 4DJ
        public static ReadOnlySpan<byte> DiCorOrgRoot
            => "1.2.826.0.1.3680043.10.386."u8;

        // PS3.5 - B.2 UUID Derived UID
        public static ReadOnlySpan<byte> UUidRoot
            => "2.25."u8;

        public static Uid NewUid()
        {
            // PS3.5 - B.2 UUID Derived UID

            var guid = Guid.NewGuid();
            Span<byte> s = stackalloc byte[16];
            guid.TryWriteBytes(s);

            (s[0], s[1], s[2], s[3], s[4], s[5], s[6], s[7]) = (s[6], s[7], s[4], s[5], s[0], s[1], s[2], s[3]);
            s.Slice(8).Reverse();
            MemoryMarshal.Cast<byte, ulong>(s).Reverse();

            UInt128 intValue = Unsafe.As<byte, UInt128>(ref s[0]);

            Span<byte> value = stackalloc byte[39 + UUidRoot.Length];
            UUidRoot.CopyTo(value);

            // TODO .NET 8 Utf8Formatter.TryFormat
            //Utf8Formatter.TryFormat(intValue, value.Slice(UUidRoot.Length), out int bytesWritten);

            TryUInt128ToDecStr(intValue, value.Slice(UUidRoot.Length), out int bytesWritten);

            return new Uid(new AsciiString(value.Slice(0, UUidRoot.Length + bytesWritten), false));
        }
    }
}
