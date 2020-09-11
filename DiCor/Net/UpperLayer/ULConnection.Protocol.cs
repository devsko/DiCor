using System;
using System.Buffers;

using Bedrock.Framework.Protocols;

namespace DiCor.Net.UpperLayer
{
    public partial class ULConnection
    {
        public class Protocol : IMessageReader<ULMessage>, IMessageWriter<ULMessage>
        {
            private readonly ULConnection _uLConnection;

            public Protocol(ULConnection uLConnection)
            {
                _uLConnection = uLConnection;
            }

            public bool TryParseMessage(in ReadOnlySequence<byte> input, ref SequencePosition consumed, ref SequencePosition examined, out ULMessage message)
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
                buffer = new SequenceReader<byte>(input.Slice(buffer.Position, length));
                var reader = new PduReader(in buffer);

                switch (message.Type)
                {
                    case Pdu.Type.AAssociateAc:
                        // TODO copy association and undo when _state is not
                        if (_uLConnection._state == ULConnectionState.Sta5_AwaitingAssociateResponse)
                            reader.ReadAAssociateAc(_uLConnection.Association);
                        break;

                    case Pdu.Type.AAssociateRj:
                        reader.ReadAAssociateRj(ref message);
                        break;

                    case Pdu.Type.AAbort:
                        reader.ReadAAbort(ref message);
                        break;

                    default:
                        // PS3.8 - 9.3 DICOM Upper Layer Protocol for TCP/IP Data Units Structure
                        // ... Items of unrecognized types shall be ignored and skipped. ...
                        break;
                }

                buffer.Advance(length);
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

                    case Pdu.Type.AAssociateRj:
                        break;

                    case Pdu.Type.AAbort:
                        writer.WriteAAbort((Pdu.AbortSource)message.B1, (Pdu.AbortReason)message.B2);
                        break;

                    default:
                        throw new NotSupportedException($"Unknown Upper Layer PDU Type {message.Type}.");
                }
            }

        }
    }
}
