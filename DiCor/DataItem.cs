using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using DiCor.Values;

namespace DiCor
{
    public readonly ref struct DataItem
    {
        private readonly bool _isQuery;
        private readonly ref readonly AbstractValue _value;

        public Tag Tag { get; }

        public VR VR { get; }

        internal DataItem(Tag tag, VR vr, bool isQuery, in AbstractValue value)
        {
            Debug.Assert(!Unsafe.IsNullRef(ref Unsafe.AsRef(in value)));

            _isQuery = isQuery;
            _value = ref value;
            Tag = tag;
            VR = vr;
        }

        public T GetValue<T>()
            => VR.GetContent<T>(_value, _isQuery);
    }
}
