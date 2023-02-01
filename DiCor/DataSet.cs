using System;
using System.Diagnostics;
using System.Linq;
using DiCor.Values;

namespace DiCor
{
    [DebuggerTypeProxy(typeof(DataSetDebugView))]
    public sealed class DataSet
    {
        private readonly ValueStore _store;

        public DataSet(bool isQuery)
        {
            _store = new ValueStore(isQuery);
        }

        internal ValueStore Store
            => _store;

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

        public bool TryGet<T>(Tag tag, out T? content)
            => _store.TryGet(tag, ValueStore.SingleItemIndex, out content);

        public bool TryGet<T>(Tag tag, ushort itemIndex, out T? content)
            => _store.TryGet(tag, itemIndex, out content);

        public override string ToString()
            => $"Dataset {_store}";
    }
}
