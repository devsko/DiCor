using System;
using System.Buffers;

using Bedrock.Framework.Protocols;

using Microsoft.Extensions.Logging;

namespace DiCor.Net.UpperLayer
{
    partial class ULConnection
    {
        public class Protocol : IMessageReader<ULMessage>, IMessageWriter<ULMessage>
        {
            private readonly ULConnection _ulConnection;

            public Protocol(ULConnection ulConnection)
            {
                _ulConnection = ulConnection;
            }

            public bool TryParseMessage(in ReadOnlySequence<byte> input, ref SequencePosition consumed, ref SequencePosition examined, out ULMessage message)
            {
                var buffer = new SequenceReader<byte>(input);

                if (buffer.Remaining >= 6)
                {
                    message = default;
                    buffer.TryReadEnumFromByte(out message.Type);
                    buffer.Advance(1);
                    buffer.TryReadBigEndian(out uint length);
                    message.Length = length;

                    _ulConnection._logger.LogDebug($"<<< {message.Type} ({message.Length} bytes)");

                    if (buffer.Remaining >= length)
                    {
                        buffer = new SequenceReader<byte>(input.Slice(buffer.Position, length));
                        var reader = new PduReader(in buffer);

                        switch (message.Type)
                        {
                            case Pdu.Type.AAssociateRq:
                                reader.ReadAAssociateRq(ref message.To<AAssociateRqData>());
                                break;

                            case Pdu.Type.AAssociateAc:
                                ref ULMessage<AAssociateAcData> associationMessage = ref message.To<AAssociateAcData>();
                                associationMessage.Data.Association = _ulConnection.Association! with { };
                                reader.ReadAAssociateAc(ref associationMessage);
                                break;

                            case Pdu.Type.AAssociateRj:
                                reader.ReadAAssociateRj(ref message.To<AAssociateRjData>());
                                break;

                            case Pdu.Type.AAbort:
                                reader.ReadAAbort(ref message.To<AAbortData>());
                                break;
                        }

                        buffer.Advance(length);
                        consumed = buffer.Position;
                        examined = consumed;
                        return true;
                    }
                }

                consumed = input.Start;
                examined = input.End;
                message = default;
                return false;
            }

            public void WriteMessage(ULMessage message, IBufferWriter<byte> output)
            {
                _ulConnection._logger.LogDebug($">>> {message.Type}");

                var writer = new PduWriter(output);
                switch (message.Type)
                {
                    case Pdu.Type.AAssociateRq:
                        writer.WriteAAssociateRq(ref message.To<AAssociateRqData>());
                        break;

                    case Pdu.Type.AAssociateAc:
                        break;

                    case Pdu.Type.AAssociateRj:
                        break;

                    case Pdu.Type.AAbort:
                        writer.WriteAAbort(ref message.To<AAbortData>());
                        break;

                    default:
                        throw new NotSupportedException($"Unknown Upper Layer PDU Type {message.Type}.");
                }
            }

        }
    }
}
