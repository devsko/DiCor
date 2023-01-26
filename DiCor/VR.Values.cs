using System;
using DiCor.Values;

namespace DiCor
{
    public partial struct VR
    {
        internal IValueTable CreateValueTable(bool isQuery)
            => (_value, isQuery) switch
            {
                (0x4541, false) => new ValueTable<AEValue<NotInQuery>>(),
                (0x4541, true) => new ValueTable<AEValue<InQuery>>(),
                (0x5351, _) => new ValueTable<ASValue>(),
                (0x4144, false) => new ValueTable<DAValue<NotInQuery>>(),
                (0x4144, true) => new ValueTable<DAValue<InQuery>>(),
                _ => throw new NotImplementedException(),
            };

        internal AbstractValue CreateValue<T>(T content, bool isQuery)
            => (_value, isQuery) switch
            {
                (0x4541, false) => AbstractValue.Of(AEValue<NotInQuery>.Create(content)),
                (0x4541, true) => AbstractValue.Of(AEValue<InQuery>.Create(content)),
                (0x5351, _) => AbstractValue.Of(ASValue.Create(content)),
                (0x4144, false) => AbstractValue.Of(DAValue<NotInQuery>.Create(content)),
                (0x4144, true) => AbstractValue.Of(DAValue<InQuery>.Create(content)),
                _ => throw new NotImplementedException(),
            };

        internal T GetContent<T>(AbstractValue value, bool isQuery)
            => (_value, isQuery) switch
            {
                (0x4541, false) => value.As<AEValue<NotInQuery>>().Get<T>(),
                (0x4541, true) => value.As<AEValue<InQuery>>().Get<T>(),
                (0x5351, _) => value.As<ASValue>().Get<T>(),
                (0x4144, false) => value.As<DAValue<NotInQuery>>().Get<T>(),
                (0x4144, true) => value.As<DAValue<InQuery>>().Get<T>(),
                _ => throw new NotImplementedException(),
            };

        internal bool IsCompatible<T>(bool isQuery)
            => (_value, isQuery) switch
            {
                (0x4541, false) => AEValue<NotInQuery>.IsCompatible<T>(),
                (0x4541, true) => value.As<AEValue<InQuery>>().Get<T>(),
                (0x5351, _) => value.As<ASValue>().Get<T>(),
                (0x4144, false) => value.As<DAValue<NotInQuery>>().Get<T>(),
                (0x4144, true) => value.As<DAValue<InQuery>>().Get<T>(),
                _ => throw new NotImplementedException(),
            };
    }
}
