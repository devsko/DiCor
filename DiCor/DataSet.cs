using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DiCor.Values;

namespace DiCor
{
    public sealed class DataSet
    {
        private readonly record struct TagIndex(Tag Tag, ushort Index);
        private readonly record struct ValueIndex(VR VR, ushort Index);

        private const ushort SingleItemIndex = unchecked((ushort)-1);

        private readonly Dictionary<VR, IValueTable> _valueTables;
        private readonly Dictionary<TagIndex, ValueIndex> _valueIndices;
        private readonly bool _isQuery;

        public DataSet(bool isQuery)
        {
            _valueTables = new Dictionary<VR, IValueTable>();
            _valueIndices = new Dictionary<TagIndex, ValueIndex>();
            _isQuery = isQuery;
        }

        public void Add<T>(Tag tag, T content)
        {
            if (!tag.IsKnown(out Tag.Details? details))
                throw new InvalidOperationException($"Tag {tag} is not known. Try to specify a VR explicitly.");

            if (!details.TryGetCompatibleVR<T>(_isQuery, out VR vr))
                throw new InvalidOperationException($"Could not find a compatible VR.");

            AddCore(tag, SingleItemIndex, vr, content);
        }

        public void Add<T>(Tag tag, VR vr, T content)
        {
            if (tag.IsKnown(out Tag.Details? details) &&
                (details.MultipleVRs is not null ? !details.MultipleVRs.Contains(vr) : details.SingleVR != vr))
                throw new InvalidOperationException($"Known Tag {tag} does not support VR {vr}.");

            if (!vr.IsCompatible<T>(_isQuery))
                throw new InvalidOperationException($"The VR is not compatible.");

            AddCore(tag, SingleItemIndex, vr, content);
        }

        public void AddMany<T>(Tag tag, ReadOnlySpan<T> content)
        {
            if (!tag.IsKnown(out Tag.Details? details))
                throw new InvalidOperationException($"Tag {tag} is not known. Try to specify a VR explicitly.");

            if (!details.TryGetCompatibleVR<T>(_isQuery, out VR vr))
                throw new InvalidOperationException($"Could not find a compatible VR.");

            AddManyCore(tag, vr, content);
        }

        public void AddMany<T>(Tag tag, VR vr, ReadOnlySpan<T> content)
        {
            if (tag.IsKnown(out Tag.Details? details) &&
                (details.MultipleVRs is not null ? !details.MultipleVRs.Contains(vr) : details.SingleVR != vr))
                throw new InvalidOperationException($"Known Tag {tag} does not support VR {vr}.");

            if (!vr.IsCompatible<T>(_isQuery))
                throw new InvalidOperationException($"The VR is not compatible.");

            AddManyCore(tag, vr, content);
        }

        internal void AddManyCore<T>(Tag tag, VR vr, ReadOnlySpan<T> content)
        {
            for (int i = 0; i < content.Length; i++)
            {
                AddCore(tag, (ushort)i, vr, content[i]);
            }
        }

        internal void AddCore<T>(Tag tag, ushort itemIndex, VR vr, T content)
        {
            ref IValueTable? table = ref CollectionsMarshal.GetValueRefOrAddDefault(_valueTables, vr, out _);
            table ??= vr.CreateValueTable(_isQuery);

            ushort tableIndex = table.Add(vr.CreateValue(content, _isQuery));

            _valueIndices.Add(new TagIndex(tag, itemIndex), new ValueIndex(vr, tableIndex));
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
        {
            if (!tag.IsKnown(out Tag.Details? details) || details.MultipleVRs is not null)
                throw new InvalidOperationException();

            // TODO MultipleVRs
            VR vr = details.SingleVR;

            if (_valueIndices.TryGetValue(tag, out ValueIndex index) &&
                _valueTables.TryGetValue(vr, out IValueTable? table))
            {
                AbstractValue value = table[index.Index];
                content = vr.GetContent<T>(value, _isQuery);
                return true;
            }

            content = default;
            return false;
        }

        public bool TryGet<T>(Tag tag, out T? content)
        {

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
