using System;
using System.Buffers;
using System.Threading;
using DiCor.Net.Protocol;

namespace DiCor
{
    public ref struct PduReader
    {
        private SequenceReader<byte> _input;

        public PduReader(in SequenceReader<byte> input)
        {
            _input = input;
        }

        public void ReadAAssociateRq()
        {
            // TODO
        }

        public void ReadAAssociateAc(Association association)
        {
        }

        public ReadOnlySequence<byte> Sequence => _input.Sequence;

        public SequencePosition Position => _input.Position;

        public long Consumed => _input.Consumed;

        public bool End => _input.End;
    }
}
