﻿using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DiCor
{
    public readonly partial record struct Uid
    {
        // PS3.5 - 9 Unique Identifiers (UIDs)
        public const string DicomOrgRoot = "1.2.840.10008.";

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
        public const string DiCorOrgRoot = "1.2.826.0.1.3680043.10.386.";

        // PS3.5 - B.2 UUID Derived UID
        public const string UUidRoot = "2.25.";

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

            Span<char> value = stackalloc char[39 + UUidRoot.Length];
            UUidRoot.CopyTo(value);
            intValue.TryFormat(value.Slice(UUidRoot.Length), out int charsWritten);

            return new Uid(value.Slice(0, charsWritten + UUidRoot.Length).ToString());
        }

        private static readonly char[] s_allowedChars = new char[] { '.', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

        private static bool IsValid(string value)
            // TODO .NET 8 IndexOfAnyValues
            => value.Length > 0 && value.Length <= 64 && value.AsSpan().IndexOfAnyExcept(s_allowedChars) == -1;

        public readonly string Value { get; private init; }

        public Uid(string value, bool validate = true)
        {
            ArgumentNullException.ThrowIfNull(value);

            // TODO .NET 8 ArgumentExcption.ThrowIf...
            if (validate & !IsValid(value))
                throw new ArgumentException($"{value} is not a valid uid.");

            Value = value;
        }

        public bool IsDicomDefined => Value.StartsWith(DicomOrgRoot);

        public override string ToString() => Value;
    }
}
