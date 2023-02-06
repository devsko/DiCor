using System;
using System.Buffers;
using System.Diagnostics;
using DiCor.Values;

namespace DiCor.IO
{
    public partial class DataSetSerializer
    {
        // TODO Endianess
        private static ReadOnlySpan<byte> SequenceDelimitationItem => new byte[] { 0xFE, 0xFF, 0xDD, 0xE0, 0x00, 0x00, 0x00, 0x00 };

        // PS3.5 - 7.1 Data Elements
        // https://dicom.nema.org/medical/dicom/current/output/html/part05.html#sect_7.1

        public unsafe bool TryParseMessage(in ReadOnlySequence<byte> input, ref SequencePosition consumed, ref SequencePosition examined, out ElementMessage message)
        {
            SequenceReader<byte> reader = new(input);

            if (reader.TryRead(out Tag tag))
            {
                if (tag.Group == 0xFFFE)
                {
                    // The VR for Data Elements, Item (FFFE,E000), Item Delimitation Item (FFFE,E00D),
                    // and Sequence Delimitation Item (FFFE,E0DD) do not exist. See PS3.5 for explanation.
                    // https://dicom.nema.org/medical/dicom/current/output/html/part06.html#note_6_2
                    // PS3.5 - 7.5 Nesting of Data Sets
                    // https://dicom.nema.org/medical/dicom/current/output/html/part05.html#sect_7.5

                    if (reader.TryReadLittleEndian(out uint length))
                    {
                        message = new(tag, default, length);
                        goto Success;
                    }
                }
                else if (reader.TryRead(out VR vr))
                {
                    if (!vr.IsKnown(out VR.Details? vrDetails))
                        throw new InvalidOperationException($"Unknown VR {vr} in item {tag}.");

                    bool isSingleValue = vrDetails.AlwaysSingleValue ||
                        (tag.IsKnown(out Tag.Details? tagDetails) && tagDetails.VM.IsSingleValue);

                    if (TryReadLength(ref reader, vrDetails.Length32bit, out uint length))
                    {
                        // for VRs of OB, OD, OF, OL, OV, OW, SQ and UN, if the Value Field has an Explicit
                        // Length, then the Value Length Field shall contain a value equal to the length
                        // (in bytes) of the Value Field, otherwise, the Value Field has an Undefined Length
                        // and a Sequence Delimitation Item marks the end of the Value Field.
                        // https://dicom.nema.org/medical/dicom/current/output/html/part05.html#sect_7.1.2
                        // https://dicom.nema.org/medical/dicom/current/output/html/part05.html#sect_7.1.3

                        bool isUndefinedLength = length == UndefinedLength;
                        SequenceReader<byte> valueReader = default;
                        if (isUndefinedLength)
                        {
                            // VR SQ is handled outside TryParseMessage
                            if (vr != VR.SQ)
                            {
                                if (!reader.TryReadTo(out ReadOnlySequence<byte> valueSequence, SequenceDelimitationItem))
                                {
                                    goto NeedMoreData;
                                }
                                valueReader = new SequenceReader<byte>(valueSequence);
                            }
                        }
                        else if (vr != VR.SQ)
                        {
                            if (reader.Remaining < length)
                            {
                                goto NeedMoreData;
                            }
                            valueReader = new SequenceReader<byte>(reader.UnreadSequence.Slice(0, length));
                            reader.Advance(length);
                        }

                        if (isSingleValue)
                        {
                            ValueRef valueRef = _state.Store.Add(tag, ValueStore.SingleItemIndex, vr);
                            ReadValue(ref valueReader, valueRef, vr);
                        }
                        else
                        {
                            // PS3.5 - 6.4 Value Multiplicity (VM) and Delimitation
                            // https://dicom.nema.org/medical/dicom/current/output/html/part05.html#sect_6.4

                            ushort i = 0;
                            if (vrDetails.IsBinary)
                            {
                                do
                                {
                                    ValueRef valueRef = _state.Store.Add(tag, i++, vr);
                                    ReadValue(ref valueReader, valueRef, vr);
                                }
                                while (!valueReader.End);
                            }
                            else
                            {
                                SequenceReader<byte> singleReader = valueReader;
                                bool separatorFound;
                                do
                                {
                                    separatorFound = valueReader.TryReadTo(out ReadOnlySequence<byte> singleSequence, Value.Backslash);
                                    singleReader = separatorFound ? new SequenceReader<byte>(singleSequence) : valueReader;

                                    ValueRef valueRef = _state.Store.Add(tag, i++, vr);
                                    ReadValue(ref singleReader, valueRef, vr);
                                }
                                while (separatorFound);
                                valueReader = singleReader;
                            }
                        }

                        if (!vrDetails.IsBinary && !valueReader.End && valueReader.UnreadSpan.TrimEnd((byte)' ').Length == 0)
                        {
                            valueReader.AdvanceToEnd();
                        }

                        Debug.Assert(vr == VR.SQ || valueReader.End, $"Item {tag} VR {vr}: {valueReader.Remaining} remaining bytes");

                        message = new(tag, vr, length);
                        goto Success;
                    }
                }
            }

        NeedMoreData:
            message = default;
            consumed = input.Start;
            examined = input.End;
            return false;

        Success:
            consumed = reader.Position;
            examined = consumed;
            return true;
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

            if (length != UndefinedLength && length % 2 != 0)
                length++;

            return true;
        }
    }
}
