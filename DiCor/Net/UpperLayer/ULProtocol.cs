using System;
using System.Buffers;
using Bedrock.Framework.Protocols;
using DiCor.Buffers;

namespace DiCor.Net.UpperLayer
{
    public class ULProtocol : IProtocolReader<ULMessage>, IProtocolWriter<ULMessage>
    {
        private readonly ULConnection _uLConnection;

        public ULProtocol(ULConnection uLConnection)
        {
            _uLConnection = uLConnection;
        }

        public bool TryParseMessage(in ReadOnlySequence<byte> input, out SequencePosition consumed, out SequencePosition examined, out ULMessage message)
        {
            var buffer = new SequenceReader<byte>(input);

            if (buffer.Remaining < 6)
                goto ReturnFalse;

            buffer.TryRead(out byte type);
            buffer.Advance(1);
            buffer.TryReadBigEndian(out uint length);

            if (buffer.Remaining < length)
                goto ReturnFalse;

            message = new ULMessage((Pdu.Type)type);
            if (!_uLConnection.CanReceive(message))
            {
                buffer.Advance(length);
            }
            else
            {
                buffer = new SequenceReader<byte>(input.Slice(buffer.Position, length));
                var reader = new PduReader(in buffer);
                switch (message.Type)
                {
                    case Pdu.Type.AAssociateAc:
                        reader.ReadAAssociateAc(_uLConnection.Association);
                        break;
                }
            }

            consumed = buffer.Position;
            examined = consumed;
            return true;

        ReturnFalse:
            consumed = input.Start;
            examined = input.End;
            message = default;
            return false;
        }

        public void WriteMessage(ULMessage message, IBufferWriter<byte> output)
        {
            var writer = new PduWriter(output);
            switch (message.Type)
            {
                case Pdu.Type.AAssociateRq:
                    writer.WriteAAssociateRq(_uLConnection.Association);
                    break;
                case Pdu.Type.AAssociateAc:
                    break;
                case Pdu.Type.AAbort:
                    writer.WriteAAbort((Pdu.AbortSource)message.B1, (Pdu.AbortReason)message.B2);
                    break;
                default:
                    break;
            }
        }

    }
}
