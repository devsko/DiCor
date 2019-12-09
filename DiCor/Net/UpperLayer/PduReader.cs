using System;
using System.Buffers;
using System.Threading;

namespace DiCor.Net.UpperLayer
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

    }
}
