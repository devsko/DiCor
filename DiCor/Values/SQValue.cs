using System.Runtime.CompilerServices;

namespace DiCor.Values
{
    internal readonly struct SQValue : IValue<SQValue>
    {
        private readonly GrowableArray<DataSet> _items;

        public SQValue(GrowableArray<DataSet> items)
            => _items = items;

        public SQValue(DataSet singleItem)
            => _items = singleItem;

        public SQValue(DataSet[] multipleItems)
            => _items = multipleItems;

        public int ItemCount
            => _items.Length;

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
            if (typeof(T) == typeof(DataSet) && _items.Length < 2)
            {
                DataSet? set = _items.SingleValueOrNull;
                return Unsafe.As<DataSet?, T>(ref set);
            }
            else if (typeof(T) == typeof(DataSet[]))
            {
                DataSet[] sets = _items.MultipleValues;
                return Unsafe.As<DataSet[], T>(ref sets);
            }
            else if (typeof(T) == typeof(object))
            {
                object? value = _items.Value;
                return Unsafe.As<object?, T>(ref value);
            }

            Value.ThrowIncompatible<T>(nameof(SQValue));
            return default;
        }

        public override string ToString()
            => ItemCount == 1 ? _items.SingleValue.ToString() : $"({ItemCount} items)";
    }
}
