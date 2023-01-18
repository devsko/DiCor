using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Threading;

namespace DiCor.Generator
{
    public class FileSaveStream : Stream
    {
        private readonly AsyncReaderWriterLock _asyncLock = new(Generator.JoinableTaskFactory.Context);
        private readonly Stream _stream;
        private readonly string _path;

        private MemoryStream? _memory;
        private FileStream? _file;

        public FileSaveStream(Stream stream, string path)
        {
            _stream = stream;
            _memory = new MemoryStream();
            _path = path;
        }

        public async Task StopBufferingAsync(CancellationToken cancellationToken = default)
        {
            if (_file != null || _memory == null)
                throw new InvalidOperationException();

            FileStream file = File.OpenWrite(_path);
            using (await _asyncLock.WriteLockAsync(cancellationToken))
            {
                _memory.Position = 0;
                await _memory.CopyToAsync(file).ConfigureAwait(false);
                _memory = null;
                _file = file;
            }
        }

        public override bool CanRead => _stream.CanRead;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => _stream.Length;

        public override long Position
        {
            get => _stream.Position;
            set => throw new NotSupportedException();
        }

        public override void Flush() => _stream.Flush();
        public override int Read(byte[] buffer, int offset, int count)
        {
            int result = _stream.Read(buffer, offset, count);
            if (result > 0)
            {
                if (_file != null)
                {
                    _file.Write(buffer, offset, result);
                }
                else
                {
                    AsyncReaderWriterLock.Awaiter awaiter = _asyncLock.WriteLockAsync().GetAwaiter();
                    awaiter.UnsafeOnCompleted(
                        () =>
                        {
                            ((Stream?)_file ?? _memory)!.Write(buffer, offset, result);
                            awaiter.GetResult().Dispose();
                        });
                }
            }

            return result;
        }

        public override long Seek(long offset, SeekOrigin origin)
            => throw new NotSupportedException();

        public override void SetLength(long value)
            => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
            => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_file != null)
                {
                    AsyncReaderWriterLock.Awaiter awaiter = _asyncLock.WriteLockAsync().GetAwaiter();
                    awaiter.UnsafeOnCompleted(
                        () =>
                        {
                            _stream.CopyTo(_file);

                            _file.Dispose();
                            _file = null;
                            _stream.Dispose();

                            awaiter.GetResult().Dispose();
                        });
                }
                else
                {
                    _stream.Dispose();
                }
            }
            base.Dispose(disposing);
        }
    }
}
