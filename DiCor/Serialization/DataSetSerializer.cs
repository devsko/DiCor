using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
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
                    throw new InvalidOperationException($"Unknown VR {vr} in item {tag}.");
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

                        ushort i = 0;
                        bool separatorFound = false;
                        SequenceReader<byte> singleReader;
                        do
                        {
                            singleReader = !vrDetails.BinaryValue &&
                                (separatorFound = valueReader.TryReadTo(out ReadOnlySequence<byte> singleSequence, Value.Backslash))
                                ? new SequenceReader<byte>(singleSequence)
                                : valueReader;

                            ValueRef valueRef = _store.Add(tag, i++, vr);
                            ReadValue(ref singleReader, valueRef, vr);
                        }
                        while (vrDetails.BinaryValue ? valueReader.Remaining > 0 : separatorFound);
                        valueReader = singleReader;
                    }

                    Debug.Assert(valueReader.End, $"Item {tag} VR {vr}: {valueReader.Remaining} remaining bytes");

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
                (VRCode.DA, false) => destination.Set(ReadDateTimeValue<DateOnly>(ref reader)),
                (VRCode.DA, true)  => destination.Set(ReadDateTimeQueryValue<DateOnly>(ref reader)),
                (VRCode.LO, false) => destination.Set(ReadStringValue<LongString, NotInQuery>(ref reader)),
                (VRCode.LO, true)  => destination.Set(ReadStringValue<LongString, InQuery>(ref reader)),
                (VRCode.LT, false) => destination.Set(ReadTextValue<LongText, NotInQuery>(ref reader)),
                (VRCode.LT, true)  => destination.Set(ReadTextValue<LongText, InQuery>(ref reader)),
                (VRCode.OB, _)     => destination.Set(ReadOtherBinaryValue<byte>(ref reader)),
                (VRCode.PN, false) => destination.Set(ReadPNValue<NotInQuery>(ref reader)),
                (VRCode.PN, true)  => destination.Set(ReadPNValue<InQuery>(ref reader)),
                (VRCode.SH, false) => destination.Set(ReadStringValue<ShortString, NotInQuery>(ref reader)),
                (VRCode.SH, true)  => destination.Set(ReadStringValue<ShortString, InQuery>(ref reader)),
                (VRCode.ST, false) => destination.Set(ReadTextValue<ShortText, NotInQuery>(ref reader)),
                (VRCode.ST, true)  => destination.Set(ReadTextValue<ShortText, InQuery>(ref reader)),
                (VRCode.TM, false) => destination.Set(ReadDateTimeValue<PartialTimeOnly>(ref reader)),
                (VRCode.TM, true)  => destination.Set(ReadDateTimeQueryValue<PartialTimeOnly>(ref reader)),
                (VRCode.UI, _)     => destination.Set(ReadUIValue(ref reader)),
                (VRCode.UL, _)     => destination.Set(ReadULValue(ref reader)),
                _ => throw new NotImplementedException(),
            };
#pragma warning restore format
        }

        private static AEValue<TIsInQuery> ReadAEValue<TIsInQuery>(ref SequenceReader<byte> reader)
            where TIsInQuery : struct, IIsInQuery
        {
            if (TIsInQuery.Value && IsQueryEmptyValue(reader))
                return new AEValue<TIsInQuery>(Value.QueryEmpty);

            reader.TryRead(-1, out AsciiString ascii);
            return new AEValue<TIsInQuery>(ascii);
        }

        private static ASValue ReadASValue(ref SequenceReader<byte> reader)
        {
            return default;
        }

        private static ATValue ReadATValue(ref SequenceReader<byte> reader)
        {
            return default;
        }

        private static CSValue<TIsInQuery> ReadCSValue<TIsInQuery>(ref SequenceReader<byte> reader)
            where TIsInQuery : struct, IIsInQuery
        {
            if (TIsInQuery.Value && IsQueryEmptyValue(reader))
                return new CSValue<TIsInQuery>(Value.QueryEmpty);

            reader.TryRead(-1, out AsciiString ascii);
            return new CSValue<TIsInQuery>(ascii);
        }

        private static TDateTime ReadDateTime<TDateTime>(ref SequenceReader<byte> reader)
            where TDateTime : struct
        {
            if (typeof(TDateTime) == typeof(DateOnly))
            {
                reader.TryRead(out DateOnly value);
                return Unsafe.As<DateOnly, TDateTime>(ref value);
            }
            if (typeof(TDateTime) == typeof(PartialTimeOnly))
            {
                reader.TryRead(out PartialTimeOnly value);
                return Unsafe.As<PartialTimeOnly, TDateTime>(ref value);
            }
            if (typeof(TDateTime) == typeof(PartialDateTime))
            {
                reader.TryRead(out PartialDateTime value);
                return Unsafe.As<PartialDateTime, TDateTime>(ref value);
            }

            throw new InvalidOperationException();
        }

        private static DateTimeValue<TDateTime> ReadDateTimeValue<TDateTime>(ref SequenceReader<byte> reader)
            where TDateTime : struct
            => new DateTimeValue<TDateTime>(ReadDateTime<TDateTime>(ref reader));

        private static DateTimeQueryValue<TDateTime> ReadDateTimeQueryValue<TDateTime>(ref SequenceReader<byte> reader)
            where TDateTime : struct, IComparable<TDateTime>, IEquatable<TDateTime>
        {
            if (IsQueryEmptyValue(reader))
                return new DateTimeQueryValue<TDateTime>(Value.QueryEmpty);

            // PS3.4 - C.2.2.2.5.1 Range Matching of Attributes of VR of DA
            // https://dicom.nema.org/medical/dicom/current/output/html/part04.html#sect_C.2.2.2.5.1
            // PS3.4 - C.2.2.2.5.2 Range Matching of Attributes of VR of TM
            // https://dicom.nema.org/medical/dicom/current/output/html/part04.html#sect_C.2.2.2.5.2

            if (reader.IsNext((byte)'-', true))
                return new DateTimeQueryValue<TDateTime>(QueryDateTime<TDateTime>.FromHi(ReadDateTime<TDateTime>(ref reader)));

            TDateTime value1 = ReadDateTime<TDateTime>(ref reader);

            return reader.IsNext((byte)'-', true)
                ? reader.Remaining >= 1 && !reader.IsNext((byte)' ')
                    ? new DateTimeQueryValue<TDateTime>(QueryDateTime<TDateTime>.FromRange(value1, ReadDateTime<TDateTime>(ref reader)))
                    : new DateTimeQueryValue<TDateTime>(QueryDateTime<TDateTime>.FromLo(value1))
                : new DateTimeQueryValue<TDateTime>(QueryDateTime<TDateTime>.FromSingle(value1));
        }

        private static OtherBinaryValue<TBinary> ReadOtherBinaryValue<TBinary>(ref SequenceReader<byte> reader)
            where TBinary : unmanaged
        {
            // TODO Undefined Length
            // TODO Endianess
            TBinary[] array = new TBinary[reader.Remaining];
            reader.TryCopyTo(MemoryMarshal.AsBytes<TBinary>(array));
            reader.AdvanceToEnd();

            return new OtherBinaryValue<TBinary>(array);
        }

        private static PNValue<TIsInQuery> ReadPNValue<TIsInQuery>(ref SequenceReader<byte> reader)
            where TIsInQuery : struct, IIsInQuery
        {
            if (TIsInQuery.Value && IsQueryEmptyValue(reader))
                return new PNValue<TIsInQuery>(Value.QueryEmpty);

            // TODO Encoding
            reader.TryRead(-1, out AsciiString ascii);
            return new PNValue<TIsInQuery>(ascii.ToString());
        }

        private static StringValue<TStringMaxLength, TIsInQuery> ReadStringValue<TStringMaxLength, TIsInQuery>(ref SequenceReader<byte> reader)
            where TStringMaxLength : struct, IStringMaxLength
            where TIsInQuery : struct, IIsInQuery
        {
            if (TIsInQuery.Value && IsQueryEmptyValue(reader))
                return new StringValue<TStringMaxLength, TIsInQuery>(Value.QueryEmpty);

            // TODO Encoding
            reader.TryRead(-1, out AsciiString ascii);
            return new StringValue<TStringMaxLength, TIsInQuery>(ascii.ToString());
        }

        private static TextValue<TTextMaxLength, TIsInQuery> ReadTextValue<TTextMaxLength, TIsInQuery>(ref SequenceReader<byte> reader)
            where TTextMaxLength : struct, ITextMaxLength
            where TIsInQuery : struct, IIsInQuery
        {
            if (TIsInQuery.Value && IsQueryEmptyValue(reader))
                return new TextValue<TTextMaxLength, TIsInQuery>(Value.QueryEmpty);

            // TODO Encoding
            // TODO encode from SR directly
            reader.TryRead(-1, out AsciiString ascii);
            return new TextValue<TTextMaxLength, TIsInQuery>(ascii.ToString());
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
