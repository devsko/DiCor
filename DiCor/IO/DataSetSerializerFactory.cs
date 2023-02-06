using System.IO;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DiCor.IO
{
    public class DataSetSerializerFactory
    {
        private readonly ILoggerFactory _loggerFactory;

        public DataSetSerializerFactory(ILoggerFactory? loggerFactory = null)
        {
            _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        }

        public DataSetSerializer Create(Stream stream, CancellationToken cancellationToken)
            => new DataSetSerializer(stream, cancellationToken, _loggerFactory.CreateLogger<DataSetSerializer>());
    }
}
