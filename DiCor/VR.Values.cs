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
                (0x4541, true)  => new ValueTable<AEValue<IsQueryContext>>(),
                (0x5351, _)     => new ValueTable<ASValue>(),
                (0x4144, false) => new ValueTable<DAValue<IsNotQueryContext>>(),
                (0x4144, true)  => new ValueTable<DAValue<IsQueryContext>>(),
                _               => throw new NotImplementedException(),
            };

        internal AbstractValue CreateValue<T>(T content, bool isQueryContext)
        {
            return (_value, isQueryContext) switch
            {
                (0x4541, false) => CreateValue<AEValue<IsNotQueryContext>>(content),
                (0x4541, true)  => CreateValue<AEValue<IsQueryContext>>(content),
                (0x5351, _)     => CreateValue<ASValue>(content),
                (0x4144, false) => CreateValue<DAValue<IsNotQueryContext>>(content),
                (0x4144, true)  => CreateValue<DAValue<IsQueryContext>>(content),
                _               => throw new NotImplementedException(),
            };

            static AbstractValue CreateValue<TValue>(T content) where TValue : struct, IValue<TValue>
                => IValue<TValue>.AsAbstract(TValue.Create(content));
        }

        internal T GetContent<T>(AbstractValue value, bool isQueryContext)
            => (_value, isQueryContext) switch
            {
                (0x4541, false) => value.As<AEValue<IsNotQueryContext>>().Get<T>(),
                (0x4541, true)  => value.As<AEValue<IsQueryContext>>().Get<T>(),
                (0x5351, _)     => value.As<ASValue>().Get<T>(),
                (0x4144, false) => value.As<DAValue<IsNotQueryContext>>().Get<T>(),
                (0x4144, true)  => value.As<DAValue<IsQueryContext>>().Get<T>(),
                _               => throw new NotImplementedException(),
            };
    }
}
