using System;
using System.Runtime.CompilerServices;

namespace DiCor.Values
{
    internal readonly struct SQValue : IValue<SQValue>
    {
        private readonly object _items;

        public SQValue(DataSet singleItem)
        {
            _items = singleItem;
        }

        public SQValue(DataSet[] multipleItems)
        {
            _items = multipleItems;
        }

        public int ItemCount
            => _items is null ? 0 : _items is DataSet[] multipleItems ? multipleItems.Length : 1;

        public static int PageSize => 5;

        public static SQValue Create<T>(T content)
        {
            if (typeof(T) == typeof(DataSet))
                return new SQValue(Unsafe.As<T, DataSet>(ref content));

            if (typeof(T) == typeof(DataSet[]))
                return new SQValue(Unsafe.As<T, DataSet[]>(ref content));

            Value.ThrowIncompatible<T>(nameof(SQValue));
            return default;
        }

        public static bool IsCompatible<T>()
            => typeof(T) == typeof(DataSet) ||
            typeof(T) == typeof(DataSet[]);

        public T Get<T>()
        {
            if (typeof(T) == typeof(DataSet) && _items is null or DataSet)
            {
                DataSet? set = _items as DataSet;
                return Unsafe.As<DataSet?, T>(ref set);
            }
            else if (typeof(T) == typeof(DataSet[]) && _items is null or DataSet[])
            {
                DataSet[] sets = _items as DataSet[] ?? Array.Empty<DataSet>();
                return Unsafe.As<DataSet[], T>(ref sets);
            }
            else if (typeof(T) == typeof(object))
            {
                return Unsafe.As<object, T>(ref Unsafe.AsRef(in _items));
            }

            Value.ThrowIncompatible<T>(nameof(SQValue));
            return default;
        }

        public override string ToString()
        {
            if (ItemCount == 1)
                return ((DataSet)_items).ToString();

            return $"({ItemCount} items)";
        }
    }
}
