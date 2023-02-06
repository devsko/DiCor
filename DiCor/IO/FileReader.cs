using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DiCor.IO
{
    // PS.10 - Media Storage and File Format for Media Interchange
    // https://dicom.nema.org/medical/dicom/current/output/html/part10.html
    public class FileReader
    {
        private static ReadOnlySpan<byte> Prefix => "DICM"u8;

        private readonly DataSetSerializerFactory _dataSetSerializerFactory;
        private readonly ILoggerFactory _loggerFactory;

        public FileReader(DataSetSerializerFactory dataSetSerializerFactory, ILoggerFactory? loggerFactory = default)
        {
            _dataSetSerializerFactory = dataSetSerializerFactory;
            _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        }

        public async Task<DataSet> ReadAsync(FileStream stream, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(stream);

            CheckPreamble();

            DataSetSerializer serializer;
            await using ((serializer = _dataSetSerializerFactory.Create(stream, cancellationToken)).ConfigureAwait(false))
            {
                DataSet fileMetaInfoSet = await serializer.DeserializeAsync(HandleFileMetaInfo).ConfigureAwait(false);
                DataSet dataSet = await serializer.DeserializeAsync(null).ConfigureAwait(false);

                return dataSet;

                ValueTask HandleFileMetaInfo(ElementMessage message)
                {
                    if (message.Tag == Tag.FileMetaInformationGroupLength)
                    {
                        serializer.SetEndIndex(serializer.Store.Get<uint>(Tag.FileMetaInformationGroupLength));
                    }

                    return ValueTask.CompletedTask;
                }
            }

            void CheckPreamble()
            {
                Span<byte> prefix = stackalloc byte[4];

                if (stream.Length < 132)
                    ThrowInvalid();
                stream.Position = 128;
                stream.ReadExactly(prefix);

                if (!prefix.SequenceEqual(Prefix))
                    ThrowInvalid();
            }

            void ThrowInvalid()
            {
                throw new InvalidOperationException("Invalid DICOM file.");
            }
        }
    }
}
