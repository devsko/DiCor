using System;
using DiCor.Values;

namespace DiCor
{
    public partial struct VR
    {
#pragma warning disable format
        internal IValueTable CreateValueTable(bool isQuery)
            => (_code, isQuery) switch
            {
                (VRCode.AE, false) => new ValueTable<AEValue<NotInQuery>>(),
                (VRCode.AE, true)  => new ValueTable<AEValue<InQuery>>(),
                (VRCode.AS, _)     => new ValueTable<ASValue>(),
                (VRCode.AT, _)     => new ValueTable<ATValue>(),
                (VRCode.CS, false) => new ValueTable<CSValue<NotInQuery>>(),
                (VRCode.CS, true)  => new ValueTable<CSValue<InQuery>>(),
                (VRCode.DA, false) => new ValueTable<DateTimeValue<DateOnly>>(),
                (VRCode.DA, true)  => new ValueTable<DateTimeQueryValue<DateOnly>>(),
                (VRCode.LO, false) => new ValueTable<StringValue<LongString, NotInQuery>>(),
                (VRCode.LO, true)  => new ValueTable<StringValue<LongString, InQuery>>(),
                (VRCode.LT, false) => new ValueTable<TextValue<LongText, NotInQuery>>(),
                (VRCode.LT, true)  => new ValueTable<TextValue<LongText, InQuery>>(),
                (VRCode.OB, _)     => new ValueTable<OtherBinaryValue<byte>>(),
                (VRCode.PN, false) => new ValueTable<PNValue<NotInQuery>>(),
                (VRCode.PN, true)  => new ValueTable<PNValue<InQuery>>(),
                (VRCode.SH, false) => new ValueTable<StringValue<ShortString, NotInQuery>>(),
                (VRCode.SH, true)  => new ValueTable<StringValue<ShortString, InQuery>>(),
                (VRCode.SQ, _)     => new ValueTable<SQValue>(),
                (VRCode.ST, false) => new ValueTable<TextValue<ShortText, NotInQuery>>(),
                (VRCode.ST, true)  => new ValueTable<TextValue<ShortText, InQuery>>(),
                (VRCode.TM, false) => new ValueTable<DateTimeValue<PartialTimeOnly>>(),
                (VRCode.TM, true)  => new ValueTable<DateTimeQueryValue<PartialTimeOnly>>(),
                (VRCode.UI, _)     => new ValueTable<UIValue>(),
                (VRCode.UL, _)     => new ValueTable<ULValue>(),
                _ => throw new NotImplementedException(),
            };

        internal bool CreateValue<T>(ValueRef valueRef, T content, bool isQuery)
            => (_code, isQuery) switch
            {
                (VRCode.AE, false) => valueRef.Set(AEValue<NotInQuery>.Create(content)),
                (VRCode.AE, true)  => valueRef.Set(AEValue<InQuery>.Create(content)),
                (VRCode.AS, _)     => valueRef.Set(ASValue.Create(content)),
                (VRCode.AT, _)     => valueRef.Set(ATValue.Create(content)),
                (VRCode.CS, false) => valueRef.Set(CSValue<NotInQuery>.Create(content)),
                (VRCode.CS, true)  => valueRef.Set(CSValue<InQuery>.Create(content)),
                (VRCode.DA, false) => valueRef.Set(DateTimeValue<DateOnly>.Create(content)),
                (VRCode.DA, true)  => valueRef.Set(DateTimeQueryValue<DateOnly>.Create(content)),
                (VRCode.LO, false) => valueRef.Set(StringValue<LongString, NotInQuery>.Create(content)),
                (VRCode.LO, true)  => valueRef.Set(StringValue<LongString, InQuery>.Create(content)),
                (VRCode.LT, false) => valueRef.Set(TextValue<LongText, NotInQuery>.Create(content)),
                (VRCode.LT, true)  => valueRef.Set(TextValue<LongText, InQuery>.Create(content)),
                (VRCode.OB, _)     => valueRef.Set(OtherBinaryValue<byte>.Create(content)),
                (VRCode.PN, false) => valueRef.Set(PNValue<NotInQuery>.Create(content)),
                (VRCode.PN, true)  => valueRef.Set(PNValue<InQuery>.Create(content)),
                (VRCode.SH, false) => valueRef.Set(StringValue<ShortString, NotInQuery>.Create(content)),
                (VRCode.SH, true)  => valueRef.Set(StringValue<ShortString, InQuery>.Create(content)),
                (VRCode.SQ, _)     => valueRef.Set(SQValue.Create(content)),
                (VRCode.ST, false) => valueRef.Set(TextValue<ShortText, NotInQuery>.Create(content)),
                (VRCode.ST, true)  => valueRef.Set(TextValue<ShortText, InQuery>.Create(content)),
                (VRCode.TM, false) => valueRef.Set(DateTimeValue<PartialTimeOnly>.Create(content)),
                (VRCode.TM, true)  => valueRef.Set(DateTimeQueryValue<PartialTimeOnly>.Create(content)),
                (VRCode.UI, _)     => valueRef.Set(UIValue.Create(content)),
                (VRCode.UL, _)     => valueRef.Set(ULValue.Create(content)),
                _ => throw new NotImplementedException(),
            };

        internal T GetContent<T>(ValueRef valueRef, bool isQuery)
            => (_code, isQuery) switch
            {
                (VRCode.AE, false) => valueRef.As<AEValue<NotInQuery>>().Get<T>(),
                (VRCode.AE, true)  => valueRef.As<AEValue<InQuery>>().Get<T>(),
                (VRCode.AS, _)     => valueRef.As<ASValue>().Get<T>(),
                (VRCode.AT, _)     => valueRef.As<ATValue>().Get<T>(),
                (VRCode.CS, false) => valueRef.As<CSValue<NotInQuery>>().Get<T>(),
                (VRCode.CS, true)  => valueRef.As<CSValue<InQuery>>().Get<T>(),
                (VRCode.DA, false) => valueRef.As<DateTimeValue<DateOnly>>().Get<T>(),
                (VRCode.DA, true)  => valueRef.As<DateTimeQueryValue<DateOnly>>().Get<T>(),
                (VRCode.LO, false) => valueRef.As<StringValue<LongString, NotInQuery>>().Get<T>(),
                (VRCode.LO, true)  => valueRef.As<StringValue<LongString, InQuery>>().Get<T>(),
                (VRCode.LT, false) => valueRef.As<TextValue<LongText, NotInQuery>>().Get<T>(),
                (VRCode.LT, true)  => valueRef.As<TextValue<LongText, InQuery>>().Get<T>(),
                (VRCode.OB, _)     => valueRef.As<OtherBinaryValue<byte>>().Get<T>(),
                (VRCode.PN, false) => valueRef.As<PNValue<NotInQuery>>().Get<T>(),
                (VRCode.PN, true)  => valueRef.As<PNValue<InQuery>>().Get<T>(),
                (VRCode.SH, false) => valueRef.As<StringValue<ShortString, NotInQuery>>().Get<T>(),
                (VRCode.SH, true)  => valueRef.As<StringValue<ShortString, InQuery>>().Get<T>(),
                (VRCode.SQ, _)     => valueRef.As<SQValue>().Get<T>(),
                (VRCode.ST, false) => valueRef.As<TextValue<ShortText, NotInQuery>>().Get<T>(),
                (VRCode.ST, true)  => valueRef.As<TextValue<ShortText, InQuery>>().Get<T>(),
                (VRCode.TM, false) => valueRef.As<DateTimeValue<PartialTimeOnly>>().Get<T>(),
                (VRCode.TM, true)  => valueRef.As<DateTimeQueryValue<PartialTimeOnly>>().Get<T>(),
                (VRCode.UI, _)     => valueRef.As<UIValue>().Get<T>(),
                (VRCode.UL, _)     => valueRef.As<ULValue>().Get<T>(),
                _ => throw new NotImplementedException(),
            };

        internal bool IsCompatible<T>(bool isQuery)
            => (_code, isQuery) switch
            {
                (VRCode.AE, false) => AEValue<NotInQuery>.IsCompatible<T>(),
                (VRCode.AE, true)  => AEValue<InQuery>.IsCompatible<T>(),
                (VRCode.AS, _)     => ASValue.IsCompatible<T>(),
                (VRCode.AT, _)     => ATValue.IsCompatible<T>(),
                (VRCode.CS, false) => CSValue<NotInQuery>.IsCompatible<T>(),
                (VRCode.CS, true)  => CSValue<InQuery>.IsCompatible<T>(),
                (VRCode.DA, false) => DateTimeValue<DateOnly>.IsCompatible<T>(),
                (VRCode.DA, true)  => DateTimeQueryValue<DateOnly>.IsCompatible<T>(),
                (VRCode.LO, false) => StringValue<LongString, NotInQuery>.IsCompatible<T>(),
                (VRCode.LO, true)  => StringValue<LongString, InQuery>.IsCompatible<T>(),
                (VRCode.LT, false) => TextValue<LongText, NotInQuery>.IsCompatible<T>(),
                (VRCode.LT, true)  => TextValue<LongText, InQuery>.IsCompatible<T>(),
                (VRCode.OB, _)     => OtherBinaryValue<byte>.IsCompatible<T>(),
                (VRCode.PN, false) => PNValue<NotInQuery>.IsCompatible<T>(),
                (VRCode.PN, true)  => PNValue<InQuery>.IsCompatible<T>(),
                (VRCode.SH, false) => StringValue<ShortString, NotInQuery>.IsCompatible<T>(),
                (VRCode.SH, true)  => StringValue<ShortString, InQuery>.IsCompatible<T>(),
                (VRCode.SQ, _)     => SQValue.IsCompatible<T>(),
                (VRCode.ST, false) => TextValue<ShortText, NotInQuery>.IsCompatible<T>(),
                (VRCode.ST, true)  => TextValue<ShortText, InQuery>.IsCompatible<T>(),
                (VRCode.TM, false) => DateTimeValue<PartialTimeOnly>.IsCompatible<T>(),
                (VRCode.TM, true)  => DateTimeValue<PartialTimeOnly>.IsCompatible<T>(),
                (VRCode.UI, _)     => UIValue.IsCompatible<T>(),
                (VRCode.UL, _)     => ULValue.IsCompatible<T>(),
                _ => throw new NotImplementedException(),
            };
#pragma warning restore format
    }
}
