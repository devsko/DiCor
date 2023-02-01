using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Threading.Tasks;
using DiCor.Serialization;

namespace DiCor.IO
{
    // PS.10 - Media Storage and File Format for Media Interchange
    // https://dicom.nema.org/medical/dicom/current/output/html/part10.html
    internal class FileReader
    {
        private static ReadOnlySpan<byte> Prefix => "DICM"u8;

        private readonly FileStream _stream;
        private readonly PipeReader _reader;

        public static async Task ReadAsync(FileStream stream)
        {
            ArgumentNullException.ThrowIfNull(stream);

            FileReader reader = new(stream);
            await reader.ReadMetaInformation().ConfigureAwait(false);
        }

        private FileReader(FileStream stream)
        {
            Debug.Assert(stream is not null);

            _stream = stream;
            _reader = PipeReader.Create(_stream);
        }

        private async Task ReadMetaInformation()
        {
            CheckPreamble();

            await new DataSetSerializer().DeserializeFileAsync(_stream).ConfigureAwait(false);

            void ThrowInvalid()
            {
                throw new InvalidOperationException("Invalid DICOM file.");
            }

            void CheckPreamble()
            {
                Span<byte> prefix = stackalloc byte[4];

                if (_stream.Length < 132)
                    ThrowInvalid();
                _stream.Position = 128;
                _stream.ReadExactly(prefix);

                if (!prefix.SequenceEqual(Prefix))
                    ThrowInvalid();
            }
        }
    }
}
