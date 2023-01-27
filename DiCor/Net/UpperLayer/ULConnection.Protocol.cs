using System;
using System.Buffers;
using System.Diagnostics;
using Bedrock.Framework.Protocols;

using Microsoft.Extensions.Logging;

namespace DiCor.Net.UpperLayer
{
    public partial class ULConnection
    {
        public class Protocol : IMessageReader<ULMessage>, IMessageWriter<ULMessage>
        {
            private readonly ILogger _logger;
            private Association? _association;

            public Protocol(ILogger logger)
            {
                _logger = logger;
            }

            public Association? Association
            {
                get => _association;
                set => _association = value;
            }

            public bool TryParseMessage(in ReadOnlySequence<byte> input, ref SequencePosition consumed, ref SequencePosition examined, out ULMessage message)
            {
                var buffer = new SequenceReader<byte>(input);

                if (buffer.Remaining >= 6)
                {
                    buffer.TryReadByte(out Pdu.Type messageType);
                    buffer.Advance(1);
                    buffer.TryReadBE(out uint length);

                    _logger.LogDebug($"<<< {messageType} ({length} bytes)");

                    // TODO
                    // Handle large PDataTf PDUs better (use a special ROSequence over PDV data blocks to create a DataSet)

                    if (buffer.Remaining >= length)
                    {
                        buffer = new SequenceReader<byte>(input.Slice(buffer.Position, length));
                        var reader = new PduReader(in buffer);

                        message = new ULMessage(messageType);
                        switch (messageType)
                        {
                            case Pdu.Type.AAssociateRq:
                                Debug.Assert(_association is not null);
                                reader.ReadAAssociateRq(ref _association);
                                break;

                            case Pdu.Type.AAssociateAc:
                                Debug.Assert(_association is not null);
                                reader.ReadAAssociateAc(_association);
                                break;

                            case Pdu.Type.AAssociateRj:
                                reader.ReadAAssociateRj(message.GetData<AAssociateRjData>());
                                break;

                            case Pdu.Type.PDataTf:
                                reader.ReadPDataTf(message.GetData<PDataTfData>());
                                break;

                            case Pdu.Type.AReleaseRq:
                                reader.ReadAReleaseRq();
                                break;

                            case Pdu.Type.AReleaseRp:
                                reader.ReadAReleaseRp();
                                break;

                            case Pdu.Type.AAbort:
                                reader.ReadAAbort(message.GetData<AAbortData>());
                                break;

                            default:
                                throw new NotSupportedException($"Unknown Upper Layer PDU Type {message.Type}.");
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
                _logger.LogDebug($">>> {message.Type}");

                var writer = new PduWriter(output);
                switch (message.Type)
                {
                    case Pdu.Type.AAssociateRq:
                        Debug.Assert(_association is not null);
                        writer.WriteAAssociateRq(_association);
                        break;

                    case Pdu.Type.AAssociateAc:
                        // TODO
                        break;

                    case Pdu.Type.AAssociateRj:
                        // TODO
                        break;

                    case Pdu.Type.PDataTf:
                        writer.WritePDataTf(message.GetData<PDataTfData>());
                        break;

                    case Pdu.Type.AReleaseRq:
                        writer.WriteAReleaseRq();
                        break;

                    case Pdu.Type.AReleaseRp:
                        writer.WriteAReleaseRp();
                        break;

                    case Pdu.Type.AAbort:
                        writer.WriteAAbort(message.GetData<AAbortData>());
                        break;

                    default:
                        throw new NotSupportedException($"Unknown Upper Layer PDU Type {message.Type}.");
                }
            }

        }
    }
}
