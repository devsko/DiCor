using System;
using System.Buffers.Text;

namespace DiCor
{
    partial struct Uid
    {
        public static readonly Uid ImplementationClass = GetImplementationClass();

        private static Uid GetImplementationClass()
        {
            byte[] value = new byte[Uid.DiCorOrgRoot.Length + 2 + Implementation.Version.Length];

            Span<byte> span = value;
            Uid.DiCorOrgRoot.CopyTo(span);
            span = span.Slice(Uid.DiCorOrgRoot.Length);
            span[0] = (byte)'1';
            span[1] = (byte)'.';
            Implementation.Version.CopyTo(span.Slice(2));

            return new Uid(value);
        }

        public static class Implementation
        {
            public static readonly byte[] Version = GetVersion();
            public static readonly byte[] VersionName = GetVersionName();

            public static Uid ClassUid => Uid.ImplementationClass;

            private static byte[] GetVersion()
            {
                Version? version = typeof(Implementation).Assembly.GetName().Version;
                if (version is null)
                {
                    return "1.0"u8.ToArray();
                }

                Span<byte> buffer = stackalloc byte[2 * 10 + 1];
                int length = 1;
                Utf8Formatter.TryFormat(version.Major, buffer, out int bytesWritten);
                buffer[bytesWritten] = (byte)'.';
                length += bytesWritten;
                Utf8Formatter.TryFormat(version.Minor, buffer.Slice(length), out bytesWritten);
                length += bytesWritten;

                return buffer.Slice(0, length).ToArray();
            }

            private static byte[] GetVersionName()
            {
                byte[] value = new byte[Version.Length + 6];
                "DiCor "u8.CopyTo(value);
                Version.CopyTo(value, 6);

                return value;
            }
        }
    }
}
