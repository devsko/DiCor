using System;
using System.Buffers;
using System.Threading;

namespace DiCor
{
    public ref struct PduReader
    {
        private readonly SequenceReader<byte> _reader;

        public PduReader(in ReadOnlySequence<byte> sequence)
        {
            _reader = new SequenceReader<byte>(sequence);
            CancellationToken = default;
        }

        public CancellationToken CancellationToken { get; set; }

        public ReadOnlySequence<byte> Sequence => _reader.Sequence;

        public SequencePosition Position => _reader.Position;

        public long Consumed => _reader.Consumed;

        public bool End => _reader.End;


    }
}
