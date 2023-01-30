using System.Diagnostics;
using DiCor.Values;

namespace DiCor
{
    public readonly ref struct DataItem
    {
        private readonly bool _isQuery;
        private readonly ValueRef _valueRef;

        public Tag Tag { get; }

        public VR VR { get; }

        internal DataItem(Tag tag, VR vr, bool isQuery, ValueRef valueRef)
        {
            Debug.Assert(!valueRef.IsNullRef());

            _isQuery = isQuery;
            _valueRef = valueRef;
            Tag = tag;
            VR = vr;
        }

        public T GetValue<T>()
            => VR.GetContent<T>(_valueRef, _isQuery);
    }
}
