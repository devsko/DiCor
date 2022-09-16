using System;
using System.Buffers;
using System.Runtime.CompilerServices;
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
                    buffer.TryReadBigEndian(out message.Length);

                    _ulConnection._logger.LogDebug($"<<< {message.Type} ({message.Length} bytes)");

                    if (buffer.Remaining >= message.Length)
                    {
                        buffer = new SequenceReader<byte>(input.Slice(buffer.Position, message.Length));
                        var reader = new PduReader(in buffer);

                        switch (message.Type)
                        {
                            case Pdu.Type.AAssociateRq:
                                reader.ReadAAssociateRq(ref Unsafe.As<long, AAssociateRqData>(ref message.Data));
                                break;

                            case Pdu.Type.AAssociateAc:
                                ref AAssociateAcData associationData = ref Unsafe.As<long, AAssociateAcData>(ref message.Data);
                                associationData.Association = _ulConnection.Association! with { };
                                reader.ReadAAssociateAc(ref associationData);
                                break;

                            case Pdu.Type.AAssociateRj:
                                reader.ReadAAssociateRj(ref Unsafe.As <long, AAssociateRjData>(ref message.Data));
                                break;

                            case Pdu.Type.AAbort:
                                reader.ReadAAbort(ref Unsafe.As <long, AAbortData>(ref message.Data));
                                break;
                        }

                        buffer.Advance(message.Length);
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

                scoped var writer = new PduWriter(output);
                switch (message.Type)
                {
                    case Pdu.Type.AAssociateRq:
                        writer.WriteAAssociateRq(ref Unsafe.As<long, AAssociateRqData>(ref message.Data));
                        break;

                    case Pdu.Type.AAssociateAc:
                        break;

                    case Pdu.Type.AAssociateRj:
                        break;

                    case Pdu.Type.AAbort:
                        writer.WriteAAbort(ref Unsafe.As<long, AAbortData>(ref message.Data));
                        break;

                    default:
                        throw new NotSupportedException($"Unknown Upper Layer PDU Type {message.Type}.");
                }
            }

        }
    }
}
