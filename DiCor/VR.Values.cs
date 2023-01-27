using System;
using DiCor.Values;

namespace DiCor
{
    public partial struct VR
    {
        internal IValueTable CreateValueTable(bool isQuery)
            => (_value, isQuery) switch
            {
                (VRCode.AE, false) => new ValueTable<AEValue<NotInQuery>>(),
                (VRCode.AE, true) => new ValueTable<AEValue<InQuery>>(),
                (VRCode.AS, _) => new ValueTable<ASValue>(),
                (VRCode.AT, _) => new ValueTable<ATValue>(),
                (VRCode.CS, false) => new ValueTable<CSValue<NotInQuery>>(),
                (VRCode.CS, true) => new ValueTable<CSValue<InQuery>>(),
                (VRCode.DA, false) => new ValueTable<DAValue>(),
                (VRCode.DA, true) => new ValueTable<DAQueryValue>(),
                _ => throw new NotImplementedException(),
            };

        internal AbstractValue CreateValue<T>(T content, bool isQuery)
            => (_value, isQuery) switch
            {
                (VRCode.AE, false) => AbstractValue.Of(AEValue<NotInQuery>.Create(content)),
                (VRCode.AE, true) => AbstractValue.Of(AEValue<InQuery>.Create(content)),
                (VRCode.AS, _) => AbstractValue.Of(ASValue.Create(content)),
                (VRCode.AT, _) => AbstractValue.Of(ATValue.Create(content)),
                (VRCode.CS, false) => AbstractValue.Of(CSValue<NotInQuery>.Create(content)),
                (VRCode.CS, true) => AbstractValue.Of(CSValue<InQuery>.Create(content)),
                (VRCode.DA, false) => AbstractValue.Of(DAValue.Create(content)),
                (VRCode.DA, true) => AbstractValue.Of(DAQueryValue.Create(content)),
                _ => throw new NotImplementedException(),
            };

        internal T GetContent<T>(AbstractValue value, bool isQuery)
            => (_value, isQuery) switch
            {
                (VRCode.AE, false) => value.As<AEValue<NotInQuery>>().Get<T>(),
                (VRCode.AE, true) => value.As<AEValue<InQuery>>().Get<T>(),
                (VRCode.AS, _) => value.As<ASValue>().Get<T>(),
                (VRCode.AT, _) => value.As<ATValue>().Get<T>(),
                (VRCode.CS, false) => value.As<CSValue<NotInQuery>>().Get<T>(),
                (VRCode.CS, true) => value.As<CSValue<InQuery>>().Get<T>(),
                (VRCode.DA, false) => value.As<DAValue>().Get<T>(),
                (VRCode.DA, true) => value.As<DAQueryValue>().Get<T>(),
                _ => throw new NotImplementedException(),
            };

        internal bool IsCompatible<T>(bool isQuery)
            => (_value, isQuery) switch
            {
                (VRCode.AE, false) => AEValue<NotInQuery>.IsCompatible<T>(),
                (VRCode.AE, true) => AEValue<InQuery>.IsCompatible<T>(),
                (VRCode.AS, _) => ASValue.IsCompatible<T>(),
                (VRCode.AT, _) => ATValue.IsCompatible<T>(),
                (VRCode.CS, false) => CSValue<NotInQuery>.IsCompatible<T>(),
                (VRCode.CS, true) => CSValue<InQuery>.IsCompatible<T>(),
                (VRCode.DA, false) => DAValue.IsCompatible<T>(),
                (VRCode.DA, true) => DAQueryValue.IsCompatible<T>(),
                _ => throw new NotImplementedException(),
            };
    }
}
