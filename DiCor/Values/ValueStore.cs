using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace DiCor.Values
{
    [DebuggerTypeProxy(typeof(DataSetDebugView))]
    internal sealed class ValueStore
    {
        private readonly record struct TagIndex(Tag Tag, ushort Index);
        private readonly record struct ValueIndex(VR VR, ushort Index);

        public const ushort SingleItemIndex = unchecked((ushort)-1);

        private readonly bool _isQuery;
        private readonly Dictionary<VR, IValueTable> _tables;
        private readonly Dictionary<TagIndex, ValueIndex> _indices;

        public ValueStore(bool isQuery)
        {
            _isQuery = isQuery;
            _tables = new Dictionary<VR, IValueTable>();
            _indices = new Dictionary<TagIndex, ValueIndex>();
        }

        public bool IsQuery
            => _isQuery;

        internal ValueRef Add(Tag tag, ushort itemIndex, VR vr)
        {
            ValueRef valueRef = EnsureTable(vr).AddDefault(out ushort tableIndex);
            _indices.Add(new TagIndex(tag, itemIndex), new ValueIndex(vr, tableIndex));

            return valueRef;
        }

        internal void SetSequence(Tag tag, SQValue sequence)
            => _tables[VR.SQ].GetRef(_indices[new TagIndex(tag, SingleItemIndex)].Index).Set(sequence);

        public void Set<T>(Tag tag, ushort itemIndex, VR vr, T content)
        {
            if (_indices.ContainsKey(new TagIndex(tag, itemIndex == SingleItemIndex ? (ushort)0 : SingleItemIndex)))
                throw new InvalidOperationException();

            if (itemIndex is not SingleItemIndex and not 0 && !_indices.ContainsKey(new TagIndex(tag, (ushort)(itemIndex - 1))))
                throw new InvalidOperationException();

            SetCore(EnsureTable(vr), tag, itemIndex, vr, content);
        }

        public void SetMany<T>(Tag tag, VR vr, ReadOnlySpan<T> content)
        {
            if (_indices.ContainsKey(new TagIndex(tag, 0)))
                throw new InvalidOperationException();

            IValueTable table = EnsureTable(vr);

            for (int i = 0; i < content.Length; i++)
            {
                SetCore(table, tag, (ushort)i, vr, content[i]);
            }
        }

        private void SetCore<T>(IValueTable table, Tag tag, ushort itemIndex, VR vr, T content)
        {
            ref ValueIndex valueIndex = ref CollectionsMarshal.GetValueRefOrAddDefault(_indices, new TagIndex(tag, itemIndex), out bool tagIndexExists);
            ValueRef valueRef;
            if (tagIndexExists)
            {
                valueRef = table.GetRef(valueIndex.Index);
            }
            else
            {
                valueRef = table.AddDefault(out ushort tableIndex);
                valueIndex = new ValueIndex(vr, tableIndex);
            }
            vr.CreateValue(valueRef, content, _isQuery);
        }

        private IValueTable EnsureTable(VR vr)
            => CollectionsMarshal.GetValueRefOrAddDefault(_tables, vr, out _) ??= vr.CreateValueTable(_isQuery);

        public bool TryGet<T>(Tag tag, ushort itemIndex, out T? content)
        {
            if (_indices.TryGetValue(new TagIndex(tag, itemIndex), out ValueIndex valueIndex) &&
                _tables.TryGetValue(valueIndex.VR, out IValueTable? table))
            {
                content = valueIndex.VR.GetContent<T>(table.GetRef(valueIndex.Index), _isQuery);
                return true;
            }

            content = default;
            return false;
        }

        internal T Get<T>(Tag tag, ushort itemIndex = SingleItemIndex)
        {
            ValueIndex valueIndex = _indices[new TagIndex(tag, itemIndex)];
            return valueIndex.VR.GetContent<T>(_tables[valueIndex.VR].GetRef(valueIndex.Index), _isQuery);
        }

        /// <summary>
        /// This kills performance. Don't use it.
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public IEnumerable<(Tag Tag, VR VR, object? BoxedValue)> EnumerateBoxed()
        {
            // TODO _indices must be sorted by Tag/Index

            Dictionary<TagIndex, ValueIndex>.Enumerator enumerator = _indices.GetEnumerator();
            if (!enumerator.MoveNext())
                yield break;

            KeyValuePair<TagIndex, ValueIndex> previousPair = enumerator.Current;
            Tag previousTag = default;
            bool hasMore;
            do
            {
                hasMore = enumerator.MoveNext();
                KeyValuePair<TagIndex, ValueIndex> currentPair = hasMore ? enumerator.Current : default;
                Tag currentTag = currentPair.Key.Tag;
                if (currentTag != previousTag)
                {
                    yield return (previousTag, previousPair.Value.VR, CreateContentObject(previousPair.Key, previousPair.Value));
                    previousTag = currentTag;
                }
                previousPair = currentPair;
            }
            while (hasMore);

            object? CreateContentObject(TagIndex tagIndex, ValueIndex valueIndex)
            {
                VR vr = valueIndex.VR;
                IValueTable table = _tables[vr];
                if (tagIndex.Index == SingleItemIndex)
                {
                    return vr.GetContent<object?>(table.GetRef(valueIndex.Index), _isQuery);
                }
                else
                {
                    object?[] values = new object?[tagIndex.Index + 1]; // Array.CreateInstance(item.Value.VR.ContentType, item.Key.Index + 1);
                    for (ushort i = 0; i < values.Length; i++)
                    {
                        values[i] = vr.GetContent<object?>(table.GetRef(_indices[new TagIndex(tagIndex.Tag, i)].Index), _isQuery);
                    }
                    return values;
                }
            }
        }

        public override string ToString()
            => $"({_indices.Count} elements)";

        internal IEnumerable<(Tag, object)> EnumerateValuesForDebugger()
        {
            IEnumerable<IGrouping<Tag, TagIndex>> tags = _indices.Keys.GroupBy(tagIndex => tagIndex.Tag);

            foreach (IGrouping<Tag, TagIndex> tag in tags)
            {
                TagIndex[] tagIndices = tag.ToArray();
                int count = tagIndices.Length;
                if (count == 1)
                {
                    ValueIndex valueIndex = _indices[tagIndices[0]];
                    yield return (tag.Key, _tables[valueIndex.VR].GetValueForDebugger(valueIndex.Index));
                }
                else
                {
                    IValueTable table = _tables[_indices[tagIndices[0]].VR];
                    Array values = table.CreateValueArray(count);
                    for (int i = 0; i < count; i++)
                    {
                        ValueIndex valueIndex = _indices[tagIndices[i]];
                        values.SetValue(table.GetValueForDebugger(valueIndex.Index), i);
                    }
                    yield return (tag.Key, values);
                }
            }
        }
    }
}
