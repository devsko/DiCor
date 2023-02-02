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

        public FileReader(FileStream stream)
        {
            Debug.Assert(stream is not null);

            _stream = stream;
            _reader = PipeReader.Create(_stream);
        }

        public async Task<DataSet> ReadAsync()
        {
            CheckPreamble();

            (DataSet fileMetaInfoSet, DataSet dataSet) = await new DataSetSerializer().DeserializeFileAsync(_stream).ConfigureAwait(false);

            return dataSet;

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
