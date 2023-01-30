using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Bedrock.Framework.Protocols;
using DiCor.Values;

namespace DiCor.Serialization
{
    internal class DataSetSerializer : IMessageReader<Tag>
    {
        private ValueStore _store = null!;

        public async ValueTask<DataSet> DeserializeAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            DataSet set = new(false);
            _store = set.Store;

            ProtocolReader reader;
            await using ((reader = new ProtocolReader(stream)).ConfigureAwait(false))
            {
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    ProtocolReadResult<Tag> result = await reader.ReadAsync(this, null, cancellationToken).ConfigureAwait(false);
                    if (result.IsCompleted || result.IsCanceled)
                    {
                        break;
                    }
                    reader.Advance();

                    Tag tag = result.Message;
                }
            }

            return set;
        }

        // PS3.5 - 7.1 Data Elements
        // https://dicom.nema.org/medical/dicom/current/output/html/part05.html#sect_7.1

        public unsafe bool TryParseMessage(in ReadOnlySequence<byte> input, ref SequencePosition consumed, ref SequencePosition examined, out Tag message)
        {
            SequenceReader<byte> buffer = new(input);

            if (buffer.TryRead(out Tag tag) && buffer.TryRead(out VR vr))
            {
                if (!vr.IsKnown(out VR.Details? vrDetails))
                {
                    // TODO
                    throw new InvalidOperationException();
                }
                bool isSingleValue = vrDetails.AlwaysSingleValue ||
                    (tag.IsKnown(out Tag.Details? tagDetails) && tagDetails.VM.IsSingleValue);

                if (TryReadLength(ref buffer, vrDetails.Length32bit, out uint length) &&
                    buffer.Remaining >= length) // TODO always? Very large values?
                {
                    SequenceReader<byte> valueReader = new(buffer.UnreadSequence.Slice(0, length));

                    if (isSingleValue)
                    {
                        ValueRef valueRef = _store.Add(tag, ValueStore.SingleItemIndex, vr);
                        ReadValue(ref valueReader, valueRef, vr);
                    }
                    else
                    {
                        // PS3.5 - 6.4 Value Multiplicity (VM) and Delimitation
                        // https://dicom.nema.org/medical/dicom/current/output/html/part05.html#sect_6.4

                        bool hasMore = true;
                        ushort i = 0;
                        while (hasMore)
                        {
                            SequenceReader<byte> singleReader = valueReader;
                            bool separatorFound = false;
                            if (!vrDetails.BinaryValue &&
                                valueReader.TryReadTo(out ReadOnlySequence<byte> singleSequence, Value.Backslash))
                            {
                                separatorFound = true;
                                singleReader = new SequenceReader<byte>(singleSequence);
                            }

                            ValueRef valueRef = _store.Add(tag, i++, vr);
                            ReadValue(ref singleReader, valueRef, vr);

                            hasMore = vrDetails.BinaryValue ? valueReader.Remaining > 0 : separatorFound;
                        }
                    }

                    Debug.Assert(valueReader.End);

                    message = tag;
                    consumed = valueReader.Position;
                    examined = consumed;
                    return true;
                }
            }

            message = default;
            consumed = input.Start;
            examined = input.End;
            return false;
        }

        private static bool TryReadLength(ref SequenceReader<byte> buffer, bool is32BitLength, out uint length)
        {
            if (is32BitLength)
            {
                if (!buffer.TryAdvance(2) ||
                    !buffer.TryReadLittleEndian(out length))
                {
                    length = default;
                    return false;
                }
            }
            else
            {
                if (!buffer.TryReadLittleEndian(out ushort shortLength))
                {
                    length = default;
                    return false;
                }
                length = shortLength;
            }

            if (length % 2 != 0)
                length++;

            return true;
        }

        private void ReadValue(ref SequenceReader<byte> reader, ValueRef destination, VR vr)
        {
#pragma warning disable format
            _ = (vr.Code, _store.IsQuery) switch
            {
                (VRCode.AE, false) => destination.Set(ReadAEValue<NotInQuery>(ref reader)),
                (VRCode.AE, true)  => destination.Set(ReadAEValue<InQuery>(ref reader)),
                (VRCode.AS, _)     => destination.Set(ReadASValue(ref reader)),
                (VRCode.AT, _)     => destination.Set(ReadATValue(ref reader)),
                (VRCode.CS, false) => destination.Set(ReadCSValue<NotInQuery>(ref reader)),
                (VRCode.CS, true)  => destination.Set(ReadCSValue<InQuery>(ref reader)),
                (VRCode.DA, false) => destination.Set(ReadDAValue(ref reader)),
                (VRCode.DA, true)  => destination.Set(ReadDAQueryValue(ref reader)),
                (VRCode.OB, _)     => destination.Set(ReadOValue<byte>(ref reader)),
                (VRCode.UI, _)     => destination.Set(ReadUIValue(ref reader)),
                (VRCode.UL, _)     => destination.Set(ReadULValue(ref reader)),
                _ => throw new NotImplementedException(),
            };
#pragma warning restore format
        }

        private static AEValue<TIsQuery> ReadAEValue<TIsQuery>(ref SequenceReader<byte> reader)
            where TIsQuery : struct, IIsInQuery
        {
            if (TIsQuery.Value && IsQueryEmptyValue(reader))
                return new AEValue<TIsQuery>(Value.QueryEmpty);

            reader.TryRead(-1, out AsciiString ascii);
            return new AEValue<TIsQuery>(ascii);
        }

        private static ASValue ReadASValue(ref SequenceReader<byte> reader)
        {
            return default;
        }

        private static ATValue ReadATValue(ref SequenceReader<byte> reader)
        {
            return default;
        }

        private static CSValue<TIsQuery> ReadCSValue<TIsQuery>(ref SequenceReader<byte> reader)
            where TIsQuery : struct, IIsInQuery
        {
            return default;
        }

        private static DAValue ReadDAValue(ref SequenceReader<byte> reader)
        {
            return default;
        }

        private static DAQueryValue ReadDAQueryValue(ref SequenceReader<byte> reader)
        {
            return default;
        }

        private static OValue<TBinary> ReadOValue<TBinary>(ref SequenceReader<byte> reader)
            where TBinary : unmanaged
        {
            // TODO Undefined Length
            // TODO Endianess
            TBinary[] array = new TBinary[reader.Remaining];
            reader.TryCopyTo(MemoryMarshal.AsBytes<TBinary>(array));
            reader.AdvanceToEnd();

            return new OValue<TBinary>(array);
        }

        private static UIValue ReadUIValue(ref SequenceReader<byte> reader)
        {
            reader.TryRead(-1, out AsciiString ascii, '\x0');
            return new UIValue(new Uid(ascii));
        }

        private static ULValue ReadULValue(ref SequenceReader<byte> reader)
        {
            reader.TryReadLittleEndian(out uint integer);
            return new ULValue(integer);
        }

        private static bool IsQueryEmptyValue(SequenceReader<byte> reader)
            => reader.Remaining == 2 && reader.IsNext(Value.DoubleQuotationMark, true);
    };
}
