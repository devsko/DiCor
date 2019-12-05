using System;
using System.Buffers;
using System.Threading;
using DiCor.Net.Protocol;

namespace DiCor
{
    public ref struct PduReader
    {
        private SequenceReader<byte> _input;

        public PduReader(in ReadOnlySequence<byte> sequence)
        {
            _input = new SequenceReader<byte>(sequence);
        }

        public bool TryRead()
        {
            if (_input.Remaining < 6)
                return false;

            _input.TryRead(out byte type);
            _input.Advance(1);
            _input.TryReadBigEndian(out uint length);

            if (_input.Remaining < length)
                return false;

            switch (type)
            {
                case Pdu.PduTypeAAssociateReq:
                    ReadAAssociateReq();
                    break;
                case Pdu.PduTypeAAssociateAcc:
                    ReadAAssociateAcc();

                    // RAUS
                    _input.Advance(length);


                    break;
                default:
                    throw new InvalidOperationException();
            }

            return true;
        }

        private void ReadAAssociateReq()
        {
            // TODO
        }

        private void ReadAAssociateAcc()
        {
        }

        public ReadOnlySequence<byte> Sequence => _input.Sequence;

        public SequencePosition Position => _input.Position;

        public long Consumed => _input.Consumed;

        public bool End => _input.End;
    }
}
