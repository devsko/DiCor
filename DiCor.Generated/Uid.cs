using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace DiCor
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public readonly partial struct Uid : IEquatable<Uid>
    {
        public byte[] Value { get; }

        public Uid(ReadOnlySpan<byte> value, bool validate = true)
        {
            Value = value.ToArray();

            // TODO .NET 8 ArgumentExcption.ThrowIf...
            if (validate & !IsValid)
            {
                throw new ArgumentException($"[{Encoding.ASCII.GetString(Value.AsSpan())}] is not a valid UID.", nameof(value));
            }
        }

        // PS 3.5 - 9.1 UID Encoding Rules
        public bool IsValid
        {
            get
            {
                Span<byte> span = Value.AsSpan();
                if (span.Length is 0 or > 64 || span.IndexOfAnyExcept(".0123456789"u8) != -1)
                {
                    return false;
                }

                int index;
                while ((index = span.IndexOf((byte)'.')) != -1)
                {
                    if (!IsValidComponent(span.Slice(0, index)))
                    {
                        return false;
                    }
                    span = span.Slice(index + 1);
                }

                return IsValidComponent(span);

                static bool IsValidComponent(Span<byte> component)
                {
                    return component.Length > 0 && (component.Length == 1 || component[0] != (byte)'0');
                }
            }
        }

        public bool IsDicomDefined => Value.AsSpan().StartsWith(DicomOrgRoot);

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string? DebuggerDisplay
        {
            get
            {
                string value = $"[{Encoding.ASCII.GetString(Value.AsSpan())}]";
                if (!IsValid)
                {
                    return $"INVALID {value}";
                }
                Details? details = GetDetails();
                if (details is null)
                {
                    return $"* {value}";
                }

                return $"{(details.Value.IsRetired ? "RETIRED " : "")} {value} {details.Value.Type}: {details.Value.Name}";
            }
        }

        // PS3.5 - 9 Unique Identifiers (UIDs)
        public static ReadOnlySpan<byte> DicomOrgRoot => "1.2.840.10008."u8;

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
        public static ReadOnlySpan<byte> DiCorOrgRoot => "1.2.826.0.1.3680043.10.386."u8;

        // PS3.5 - B.2 UUID Derived UID
        public static ReadOnlySpan<byte> UUidRoot => "2.25."u8;

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

            TryUInt128ToDecStr(intValue, value.Slice(UUidRoot.Length), out int bytesWritten);

            // TODO .NET 8 Utf8Formatter.TryFormat
            //Utf8Formatter.TryFormat(intValue, value.Slice(UUidRoot.Length), out int charsWritten);

            return new Uid(value.Slice(0, UUidRoot.Length + bytesWritten));
        }
    }
}
