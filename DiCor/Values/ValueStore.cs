using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace DiCor.Values
{
    internal sealed class ValueStore
    {
        private readonly record struct TagIndex(Tag Tag, ushort Index);
        private readonly record struct ValueIndex(VR VR, ushort Index);

        public const ushort SingleItemIndex = unchecked((ushort)-1);

        private readonly bool _isQuery;
        private readonly Dictionary<VR, IValueTable> _valueTables;
        private readonly Dictionary<TagIndex, ValueIndex> _valueIndices;

        public ValueStore(bool isQuery)
        {
            _isQuery = isQuery;
            _valueTables = new Dictionary<VR, IValueTable>();
            _valueIndices = new Dictionary<TagIndex, ValueIndex>();
        }

        public bool IsQuery
            => _isQuery;

        public void Set<T>(Tag tag, ushort itemIndex, VR vr, T content)
        {
            if (_valueIndices.ContainsKey(new TagIndex(tag, itemIndex == SingleItemIndex ? (ushort)0 : SingleItemIndex)))
                throw new InvalidOperationException();

            if (itemIndex is not SingleItemIndex and not 0 && !_valueIndices.ContainsKey(new TagIndex(tag, (ushort)(itemIndex - 1))))
                throw new InvalidOperationException();

            ref IValueTable? table = ref CollectionsMarshal.GetValueRefOrAddDefault(_valueTables, vr, out _);
            table ??= vr.CreateValueTable(_isQuery);

            SetCore(table, tag, itemIndex, vr, content);
        }

        public void SetMany<T>(Tag tag, VR vr, ReadOnlySpan<T> content)
        {
            if (_valueIndices.ContainsKey(new TagIndex(tag, 0)))
                throw new InvalidOperationException();

            ref IValueTable? table = ref CollectionsMarshal.GetValueRefOrAddDefault(_valueTables, vr, out _);
            table ??= vr.CreateValueTable(_isQuery);

            for (int i = 0; i < content.Length; i++)
            {
                SetCore(table, tag, (ushort)i, vr, content[i]);
            }
        }

        private void SetCore<T>(IValueTable table, Tag tag, ushort itemIndex, VR vr, T content)
        {
            AbstractValue value = vr.CreateValue(content, _isQuery);

            ref ValueIndex valueIndex = ref CollectionsMarshal.GetValueRefOrAddDefault(_valueIndices, new TagIndex(tag, itemIndex), out bool tagIndexExists);
            if (tagIndexExists)
            {
                table.SetValue(valueIndex.Index, value);
            }
            else
            {
                valueIndex = new ValueIndex(vr, table.Add(value));
            }
        }

        public bool TryGet<T>(Tag tag, ushort itemIndex, out T? content)
        {
            if (_valueIndices.TryGetValue(new TagIndex(tag, itemIndex), out ValueIndex valueIndex) &&
                _valueTables.TryGetValue(valueIndex.VR, out IValueTable? table))
            {
                AbstractValue value = table.GetValueRef(valueIndex.Index);
                content = valueIndex.VR.GetContent<T>(value, _isQuery);
                return true;
            }

            content = default;
            return false;
        }

        public bool TryGet(Tag tag, ushort itemIndex, out DataItem item)
        {
            if (_valueIndices.TryGetValue(new TagIndex(tag, itemIndex), out ValueIndex valueIndex) &&
                _valueTables.TryGetValue(valueIndex.VR, out IValueTable? table))
            {
                item = new DataItem(tag, valueIndex.VR, _isQuery, in table.GetValueRef(valueIndex.Index));
                return true;
            }

            item = default;
            return false;
        }
    }
}
