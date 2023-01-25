using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using DiCor.Values;

namespace DiCor
{
    public ref struct DataItem
    {
        private ref AbstractValue _value;

        public Tag Tag { get; }

        public VR VR { get; }

        internal DataItem(Tag tag, VR vr, ref AbstractValue value)
        {
            Tag = tag;
            VR = vr;
            _value = ref value;
        }

        [UnscopedRef]
        public ref TValue ValueRef<TValue>()
            where TValue : struct, IValue<TValue>
        {
            if (Unsafe.IsNullRef(ref _value))
            {
                throw new NullReferenceException();
            }

            return ref Unsafe.As<AbstractValue, TValue>(ref _value);
        }
    }
}
