using System;
using System.Text.Unicode;

namespace DiCor
{
    partial record struct Uid
    {
        public static readonly Uid ImplementationClass = GetImplementationClass();

        private static Uid GetImplementationClass()
        {
            byte[] value = new byte[Uid.DiCorOrgRoot.Length + 2 + Implementation.Version.Length];
            Span<byte> span = value;

            Uid.DiCorOrgRoot.CopyTo(span);
            int length = Uid.DiCorOrgRoot.Length;
            "1."u8.CopyTo(span.Slice(length));
            length += 2;
            Implementation.Version.CopyTo(span.Slice(length));

            return new Uid(value);
        }
    }

    public class Implementation
    {
        public static readonly byte[] Version = GetVersion();
        public static readonly byte[] VersionName = GetVersionName();

        public static Uid ClassUid
            => Uid.ImplementationClass;

        private static byte[] GetVersion()
            => ToAscii((typeof(Implementation).Assembly.GetName().Version ?? new Version()).ToString());

        private static byte[] GetVersionName()
        {
            byte[] value = new byte[Version.Length + 6];
            "DiCor "u8.CopyTo(value);
            Version.CopyTo(value, 6);

            return value;
        }

        private static byte[] ToAscii(string utf16)
        {
            byte[] buffer = new byte[utf16.Length];
            Utf8.FromUtf16(utf16, buffer, out _, out _);

            return buffer;
        }
    }
}
