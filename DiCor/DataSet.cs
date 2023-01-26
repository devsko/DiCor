using System;
using System.Linq;
using DiCor.Values;

namespace DiCor
{
    public sealed class DataSet
    {
        private readonly ValueStore _store;

        public DataSet(bool isQuery)
        {
            _store = new ValueStore(isQuery);
        }

        public void Set<T>(Tag tag, T content)
        {
            if (!tag.IsKnown(out Tag.Details? details))
                throw new InvalidOperationException($"Tag {tag} is not known. Try to specify a VR explicitly.");

            if (!details.TryGetCompatibleVR<T>(_store.IsQuery, out VR vr))
                throw new InvalidOperationException($"Could not find a compatible VR.");

            _store.Set(tag, ValueStore.SingleItemIndex, vr, content);
        }

        public void Set<T>(Tag tag, ushort itemIndex, T content)
        {
            if (!tag.IsKnown(out Tag.Details? details))
                throw new InvalidOperationException($"Tag {tag} is not known. Try to specify a VR explicitly.");

            if (!details.TryGetCompatibleVR<T>(_store.IsQuery, out VR vr))
                throw new InvalidOperationException($"Could not find a compatible VR.");

            _store.Set(tag, itemIndex, vr, content);
        }

        public void Set<T>(Tag tag, VR vr, T content)
        {
            if (tag.IsKnown(out Tag.Details? details) &&
                (details.MultipleVRs is not null ? !details.MultipleVRs.Contains(vr) : details.SingleVR != vr))
                throw new InvalidOperationException($"Known Tag {tag} does not support VR {vr}.");

            if (!vr.IsCompatible<T>(_store.IsQuery))
                throw new InvalidOperationException($"The VR is not compatible.");

            _store.Set(tag, ValueStore.SingleItemIndex, vr, content);
        }

        public void Set<T>(Tag tag, ushort itemIndex, VR vr, T content)
        {
            if (tag.IsKnown(out Tag.Details? details) &&
                (details.MultipleVRs is not null ? !details.MultipleVRs.Contains(vr) : details.SingleVR != vr))
                throw new InvalidOperationException($"Known Tag {tag} does not support VR {vr}.");

            if (!vr.IsCompatible<T>(_store.IsQuery))
                throw new InvalidOperationException($"The VR is not compatible.");

            _store.Set(tag, itemIndex, vr, content);
        }

        public void SetMany<T>(Tag tag, ReadOnlySpan<T> content)
        {
            if (!tag.IsKnown(out Tag.Details? details))
                throw new InvalidOperationException($"Tag {tag} is not known. Try to specify a VR explicitly.");

            if (!details.TryGetCompatibleVR<T>(_store.IsQuery, out VR vr))
                throw new InvalidOperationException($"Could not find a compatible VR.");

            _store.SetMany(tag, vr, content);
        }

        public void SetMany<T>(Tag tag, VR vr, ReadOnlySpan<T> content)
        {
            if (tag.IsKnown(out Tag.Details? details) &&
                (details.MultipleVRs is not null ? !details.MultipleVRs.Contains(vr) : details.SingleVR != vr))
                throw new InvalidOperationException($"Known Tag {tag} does not support VR {vr}.");

            if (!vr.IsCompatible<T>(_store.IsQuery))
                throw new InvalidOperationException($"The VR is not compatible.");

            _store.SetMany(tag, vr, content);
        }



        //public void AddVRValue<TValue>(Tag tag, TValue value)
        //    where TValue : struct, IValue<TValue>
        //{
        //    VR vr = TValue.VR;

        //    ValueTable<TValue> tableT;
        //    ref IValueTable? table = ref CollectionsMarshal.GetValueRefOrAddDefault(_valueTables, vr, out _);
        //    if (table is null)
        //    {
        //        table = (tableT = new ValueTable<TValue>());
        //    }
        //    else
        //    {
        //        tableT = Unsafe.As<IValueTable, ValueTable<TValue>>(ref table);
        //    }

        //    tableT.AddDefault(out ushort index) = value;

        //    _valueIndices.Add(tag, new ValueIndex(vr, index));
        //}

        public bool TryGet<T>(Tag tag, out T? content)
            => _store.TryGet(tag, ValueStore.SingleItemIndex, out content);

        public bool TryGet<T>(Tag tag, ushort itemIndex, out T? content)
            => _store.TryGet(tag, itemIndex, out content);

        //public bool TryGetVRValue<TValue>(Tag tag, out TValue value)
        //    where TValue : struct, IValue<TValue>
        //{
        //    VR vr = TValue.VR;
        //    if (_valueIndices.TryGetValue(tag, out ValueIndex index) &&
        //        _valueTables.TryGetValue(vr, out IValueTable? table))
        //    {
        //        ValueTable<TValue> tableT = Unsafe.As<IValueTable, ValueTable<TValue>>(ref table);

        //        value = tableT[index.Index];
        //        return true;
        //    }

        //    value = default;
        //    return false;
        //}

        public bool TryGet(Tag tag, out DataItem item)
            => _store.TryGet(tag, ValueStore.SingleItemIndex, out item);

        public bool TryGet(Tag tag, ushort itemIndex, out DataItem item)
            => _store.TryGet(tag, itemIndex, out item);

        // TODO C# 12 - where ref struct
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
