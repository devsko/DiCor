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
                (VRCode.OB, _)     => new ValueTable<OxValue<byte>>(),
                (VRCode.SH, false) => new ValueTable<SHValue<NotInQuery>>(),
                (VRCode.SH, true)  => new ValueTable<SHValue<InQuery>>(),
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
                (VRCode.OB, _)     => valueRef.Set(OxValue<byte>.Create(content)),
                (VRCode.SH, false) => valueRef.Set(SHValue<NotInQuery>.Create(content)),
                (VRCode.SH, true)  => valueRef.Set(SHValue<InQuery>.Create(content)),
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
                (VRCode.OB, _)     => valueRef.As<OxValue<byte>>().Get<T>(),
                (VRCode.SH, false) => valueRef.As<SHValue<NotInQuery>>().Get<T>(),
                (VRCode.SH, true)  => valueRef.As<SHValue<InQuery>>().Get<T>(),
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
                (VRCode.OB, _)     => OxValue<byte>.IsCompatible<T>(),
                (VRCode.SH, false) => SHValue<NotInQuery>.IsCompatible<T>(),
                (VRCode.SH, true)  => SHValue<InQuery>.IsCompatible<T>(),
                (VRCode.TM, false) => DateTimeValue<PartialTimeOnly>.IsCompatible<T>(),
                (VRCode.TM, true)  => DateTimeValue<PartialTimeOnly>.IsCompatible<T>(),
                (VRCode.UI, _)     => UIValue.IsCompatible<T>(),
                (VRCode.UL, _)     => ULValue.IsCompatible<T>(),
                _ => throw new NotImplementedException(),
            };
#pragma warning restore format
    }
}
