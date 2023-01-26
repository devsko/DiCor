using System;
using DiCor.Values;

namespace DiCor
{
    public partial struct VR
    {
        internal IValueTable CreateValueTable(bool isQueryContext)
            => (_value, isQueryContext) switch
            {
                (0x4541, false) => new ValueTable<AEValue<IsNotQueryContext>>(),
                (0x4541, true) => new ValueTable<AEValue<IsQueryContext>>(),
                (0x5351, _) => new ValueTable<ASValue>(),
                (0x4144, false) => new ValueTable<DAValue<IsNotQueryContext>>(),
                (0x4144, true) => new ValueTable<DAValue<IsQueryContext>>(),
                _ => throw new NotImplementedException(),
            };

        internal AbstractValue CreateValue<T>(T content, bool isQueryContext)
            => (_value, isQueryContext) switch
            {
                (0x4541, false) => AbstractValue.Of(AEValue<IsNotQueryContext>.Create(content)),
                (0x4541, true) => AbstractValue.Of(AEValue<IsQueryContext>.Create(content)),
                (0x5351, _) => AbstractValue.Of(ASValue.Create(content)),
                (0x4144, false) => AbstractValue.Of(DAValue<IsNotQueryContext>.Create(content)),
                (0x4144, true) => AbstractValue.Of(DAValue<IsQueryContext>.Create(content)),
                _ => throw new NotImplementedException(),
            };

        internal T GetContent<T>(AbstractValue value, bool isQueryContext)
            => (_value, isQueryContext) switch
            {
                (0x4541, false) => value.As<AEValue<IsNotQueryContext>>().Get<T>(),
                (0x4541, true) => value.As<AEValue<IsQueryContext>>().Get<T>(),
                (0x5351, _) => value.As<ASValue>().Get<T>(),
                (0x4144, false) => value.As<DAValue<IsNotQueryContext>>().Get<T>(),
                (0x4144, true) => value.As<DAValue<IsQueryContext>>().Get<T>(),
                _ => throw new NotImplementedException(),
            };
    }
}
