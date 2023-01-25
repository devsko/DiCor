using System;
using System.Buffers.Text;

namespace DiCor
{
    public partial struct Uid
    {
        public static readonly Uid ImplementationClass = GetImplementationClass();

        private static Uid GetImplementationClass()
        {
            ReadOnlySpan<byte> version = Implementation.Version.Bytes;
            Span<byte> buffer = stackalloc byte[DiCorOrgRoot.Length + 2 + version.Length];
            Span<byte> span = buffer;
            DiCorOrgRoot.CopyTo(span);
            span = span.Slice(DiCorOrgRoot.Length);
            span[0] = (byte)'1';
            span[1] = (byte)'.';
            version.CopyTo(span.Slice(2));

            return new Uid(new AsciiString(buffer, false));
        }

        public static class Implementation
        {
            public static readonly AsciiString Version = GetVersion();
            public static readonly AsciiString VersionName = GetVersionName();

            public static Uid ClassUid => ImplementationClass;

            private static AsciiString GetVersion()
            {
                Version? version = typeof(Implementation).Assembly.GetName().Version;
                if (version is null)
                {
                    return new AsciiString("1.0"u8, false);
                }

                Span<byte> buffer = stackalloc byte[2 * 10 + 1];
                int length = 1;
                Utf8Formatter.TryFormat(version.Major, buffer, out int bytesWritten);
                buffer[bytesWritten] = (byte)'.';
                length += bytesWritten;
                Utf8Formatter.TryFormat(version.Minor, buffer.Slice(length), out bytesWritten);
                length += bytesWritten;

                return new AsciiString(buffer.Slice(0, length), false);
            }

            private static AsciiString GetVersionName()
            {
                ReadOnlySpan<byte> version = Version.Bytes;
                Span<byte> buffer = stackalloc byte[version.Length + 6];
                "DiCor "u8.CopyTo(buffer);
                version.CopyTo(buffer.Slice(6));

                return new AsciiString(buffer, false);
            }
        }
    }
}
