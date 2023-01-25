using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DiCor.Values;

namespace DiCor
{
    public sealed class DataSet
    {
        private readonly record struct ValueIndex(VR VR, ushort Index);

        private readonly Dictionary<VR, IValueTable> _valueTables;
        private readonly Dictionary<Tag, ValueIndex> _valueIndices;
        private readonly bool _isQueryContext;

        public DataSet(bool isQueryContext)
        {
            _valueTables = new Dictionary<VR, IValueTable>();
            _valueIndices = new Dictionary<Tag, ValueIndex>();
            _isQueryContext = isQueryContext;
        }

        public void Add<T>(Tag tag, T content)
        {
            if (!tag.IsKnown(out Tag.Details? details) || details.MultipleVRs is not null)
                throw new InvalidOperationException();

            // TODO MultipleVRs
            VR vr = details.SingleVR;

            ref IValueTable? table = ref CollectionsMarshal.GetValueRefOrAddDefault(_valueTables, vr, out _);
            table ??= vr.CreateValueTable(_isQueryContext);

            ushort index = table.Add(vr.CreateValue(content, _isQueryContext));

            _valueIndices.Add(tag, new ValueIndex(vr, index));
        }

        public void AddVRValue<TValue>(Tag tag, TValue value)
            where TValue : struct, IValue<TValue>
        {
            VR vr = TValue.VR;

            ValueTable<TValue> tableT;
            ref IValueTable? table = ref CollectionsMarshal.GetValueRefOrAddDefault(_valueTables, vr, out _);
            if (table is null)
            {
                table = (tableT = new ValueTable<TValue>());
            }
            else
            {
                tableT = Unsafe.As<IValueTable, ValueTable<TValue>>(ref table);
            }

            tableT.AddDefault(out ushort index) = value;

            _valueIndices.Add(tag, new ValueIndex(vr, index));
        }

        public bool TryGet<T>(Tag tag, out T? content)
        {
            if (!tag.IsKnown(out Tag.Details? details) || details.MultipleVRs is not null)
                throw new InvalidOperationException();

            // TODO MultipleVRs
            VR vr = details.SingleVR;

            if (_valueIndices.TryGetValue(tag, out ValueIndex index) &&
                _valueTables.TryGetValue(vr, out IValueTable? table))
            {
                AbstractValue value = table[index.Index];
                content = vr.GetContent<T>(value, _isQueryContext);
                return true;
            }

            content = default;
            return false;
        }

        public bool TryGetVRValue<TValue>(Tag tag, out TValue value)
            where TValue : struct, IValue<TValue>
        {
            VR vr = TValue.VR;
            if (_valueIndices.TryGetValue(tag, out ValueIndex index) &&
                _valueTables.TryGetValue(vr, out IValueTable? table))
            {
                ValueTable<TValue> tableT = Unsafe.As<IValueTable, ValueTable<TValue>>(ref table);

                value = tableT[index.Index];
                return true;
            }

            value = default;
            return false;
        }

        public bool TryGet(Tag tag, out DataItem item)
        {
            if (_valueIndices.TryGetValue(tag, out ValueIndex index))
            {
                if (_valueTables.TryGetValue(index.VR, out IValueTable? table))
                {
                    item = new DataItem(tag, index.VR, ref table[index.Index]);
                    return true;
                }
            }

            item = default;
            return false;
        }

        // TODO C# 12 - ref struct as type parameter
        //public IEnumerable<DataItem> Items
        //{
        //    get
        //    {
        //        foreach (KeyValuePair<Tag, ValueIndex> tagIndex in _valueIndices)
        //        {
        //            yield return new DataItem(tagIndex.Key, tagIndex.Value.VR, ref _valueTables[tagIndex.Value.VR][tagIndex.Value.Index]);
        //        }
        //    }
        //}
    }
}
