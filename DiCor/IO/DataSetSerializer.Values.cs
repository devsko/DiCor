using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DiCor.Values;

namespace DiCor.IO
{
    public partial class DataSetSerializer
    {
        private void ReadValue(ref SequenceReader<byte> reader, ValueRef destination, VR vr)
        {
#pragma warning disable format
            _ = (vr.Code, _state.Store.IsQuery) switch
            {
                (VRCode.AE, false) => destination.Set(ReadAEValue<NotInQuery>(ref reader)),
                (VRCode.AE, true)  => destination.Set(ReadAEValue<InQuery>(ref reader)),
                (VRCode.AS, _)     => destination.Set(ReadASValue(ref reader)),
                (VRCode.AT, _)     => destination.Set(ReadATValue(ref reader)),
                (VRCode.CS, false) => destination.Set(ReadCSValue<NotInQuery>(ref reader)),
                (VRCode.CS, true)  => destination.Set(ReadCSValue<InQuery>(ref reader)),
                (VRCode.DA, false) => destination.Set(ReadDateTimeValue<DateOnly>(ref reader)),
                (VRCode.DA, true)  => destination.Set(ReadDateTimeQueryValue<DateOnly>(ref reader)),
                (VRCode.DS, _)     => destination.Set(ReadDSValue(ref reader)),
                (VRCode.FD, _)     => destination.Set(ReadFDValue(ref reader)),
                (VRCode.FL, _)     => destination.Set(ReadFLValue(ref reader)),
                (VRCode.IS, _)     => destination.Set(ReadISValue(ref reader)),
                (VRCode.LO, false) => destination.Set(ReadStringValue<LongString, NotInQuery>(ref reader)),
                (VRCode.LO, true)  => destination.Set(ReadStringValue<LongString, InQuery>(ref reader)),
                (VRCode.LT, false) => destination.Set(ReadTextValue<LongText, NotInQuery>(ref reader)),
                (VRCode.LT, true)  => destination.Set(ReadTextValue<LongText, InQuery>(ref reader)),
                (VRCode.OB, _)     => destination.Set(ReadOtherBinaryValue<byte>(ref reader)),
                (VRCode.PN, false) => destination.Set(ReadPNValue<NotInQuery>(ref reader)),
                (VRCode.PN, true)  => destination.Set(ReadPNValue<InQuery>(ref reader)),
                (VRCode.SH, false) => destination.Set(ReadStringValue<ShortString, NotInQuery>(ref reader)),
                (VRCode.SH, true)  => destination.Set(ReadStringValue<ShortString, InQuery>(ref reader)),
                (VRCode.SL, _)     => destination.Set(ReadSLValue(ref reader)),
                (VRCode.SQ, _)     => destination.Set(default(SQValue)), // Gets replaced afterwards
                (VRCode.SS, _)     => destination.Set(ReadSSValue(ref reader)),
                (VRCode.ST, false) => destination.Set(ReadTextValue<ShortText, NotInQuery>(ref reader)),
                (VRCode.ST, true)  => destination.Set(ReadTextValue<ShortText, InQuery>(ref reader)),
                (VRCode.TM, false) => destination.Set(ReadDateTimeValue<PartialTimeOnly>(ref reader)),
                (VRCode.TM, true)  => destination.Set(ReadDateTimeQueryValue<PartialTimeOnly>(ref reader)),
                (VRCode.UI, _)     => destination.Set(ReadUIValue(ref reader)),
                (VRCode.UL, _)     => destination.Set(ReadULValue(ref reader)),
                (VRCode.US, _)     => destination.Set(ReadUSValue(ref reader)),
                _ => throw new NotImplementedException(),
            };
#pragma warning restore format
        }

        private static AEValue<TIsInQuery> ReadAEValue<TIsInQuery>(ref SequenceReader<byte> reader)
            where TIsInQuery : struct, IIsInQuery
        {
            if (TIsInQuery.Value && reader.IsQueryEmptyValue())
                return new AEValue<TIsInQuery>(Value.QueryEmpty);

            reader.TryRead(-1, out AsciiString ascii);
            return new AEValue<TIsInQuery>(ascii);
        }

        private static ASValue ReadASValue(ref SequenceReader<byte> reader)
        {
            reader.TryRead(out Age age);
            return new ASValue(age);
        }

        private static ATValue ReadATValue(ref SequenceReader<byte> reader)
        {
            // TODO
            return default;
        }

        private static CSValue<TIsInQuery> ReadCSValue<TIsInQuery>(ref SequenceReader<byte> reader)
            where TIsInQuery : struct, IIsInQuery
        {
            if (TIsInQuery.Value && reader.IsQueryEmptyValue())
                return new CSValue<TIsInQuery>(Value.QueryEmpty);

            reader.TryRead(-1, out AsciiString ascii);
            return new CSValue<TIsInQuery>(ascii);
        }

        private static DateTimeValue<TDateTime> ReadDateTimeValue<TDateTime>(ref SequenceReader<byte> reader)
            where TDateTime : struct
            => new DateTimeValue<TDateTime>(ReadDateTime<TDateTime>(ref reader));

        private static DateTimeQueryValue<TDateTime> ReadDateTimeQueryValue<TDateTime>(ref SequenceReader<byte> reader)
            where TDateTime : struct, IComparable<TDateTime>, IEquatable<TDateTime>
        {
            if (reader.IsQueryEmptyValue())
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

        private static OtherBinaryValue<TBinary> ReadOtherBinaryValue<TBinary>(ref SequenceReader<byte> reader)
            where TBinary : unmanaged
        {
            // TODO Undefined Length without copy
            // TODO Endianess
            TBinary[] array = new TBinary[reader.Remaining];
            reader.TryCopyTo(MemoryMarshal.AsBytes<TBinary>(array));
            reader.AdvanceToEnd();

            return new OtherBinaryValue<TBinary>(array);
        }

        private PNValue<TIsInQuery> ReadPNValue<TIsInQuery>(ref SequenceReader<byte> reader)
            where TIsInQuery : struct, IIsInQuery
        {
            if (TIsInQuery.Value && reader.IsQueryEmptyValue())
                return new PNValue<TIsInQuery>(Value.QueryEmpty);

            return new PNValue<TIsInQuery>(ReadAndDecodeString(ref reader, PNValueExtensionResetBytes));
        }

        private StringValue<TStringMaxLength, TIsInQuery> ReadStringValue<TStringMaxLength, TIsInQuery>(ref SequenceReader<byte> reader)
            where TStringMaxLength : struct, IStringMaxLength
            where TIsInQuery : struct, IIsInQuery
        {
            if (TIsInQuery.Value && reader.IsQueryEmptyValue())
                return new StringValue<TStringMaxLength, TIsInQuery>(Value.QueryEmpty);

            return new StringValue<TStringMaxLength, TIsInQuery>(ReadAndDecodeString(ref reader));
        }

        private TextValue<TTextMaxLength, TIsInQuery> ReadTextValue<TTextMaxLength, TIsInQuery>(ref SequenceReader<byte> reader)
            where TTextMaxLength : struct, ITextMaxLength
            where TIsInQuery : struct, IIsInQuery
        {
            if (TIsInQuery.Value && reader.IsQueryEmptyValue())
                return new TextValue<TTextMaxLength, TIsInQuery>(Value.QueryEmpty);

            return new TextValue<TTextMaxLength, TIsInQuery>(ReadAndDecodeString(ref reader, ControlExtensionResetBytes));
        }

        private static UIValue ReadUIValue(ref SequenceReader<byte> reader)
        {
            reader.TryRead(-1, out AsciiString ascii, '\0');
            return new UIValue(new Uid(ascii));
        }

        private static ULValue ReadULValue(ref SequenceReader<byte> reader)
        {
            reader.TryReadLittleEndian(out uint integer);
            return new ULValue(integer);
        }

        private static USValue ReadUSValue(ref SequenceReader<byte> reader)
        {
            reader.TryReadLittleEndian(out ushort integer);
            return new USValue(integer);
        }

        private static SLValue ReadSLValue(ref SequenceReader<byte> reader)
        {
            reader.TryReadLittleEndian(out int integer);
            return new SLValue(integer);
        }

        private static SSValue ReadSSValue(ref SequenceReader<byte> reader)
        {
            reader.TryReadLittleEndian(out short integer);
            return new SSValue(integer);
        }

        private static DSValue ReadDSValue(ref SequenceReader<byte> reader)
        {
            reader.TryRead(out decimal @decimal);
            return new DSValue(@decimal);
        }

        private static ISValue ReadISValue(ref SequenceReader<byte> reader)
        {
            reader.TryRead(out int @int);
            return new ISValue(@int);
        }

        private static FLValue ReadFLValue(ref SequenceReader<byte> reader)
        {
            reader.TryReadLittleEndian(out float @float);
            return new FLValue(@float);
        }

        private static FDValue ReadFDValue(ref SequenceReader<byte> reader)
        {
            reader.TryReadLittleEndian(out double @double);
            return new FDValue(@double);
        }
    }
}
