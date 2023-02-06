using System;

namespace DiCor
{
    public struct GrowableArray<T>
        where T : class
    {
        private object? _value;

        public int Length
            => _value is null ? 0 : _value is T[] values ? values.Length : 1;

        public T SingleValue
            => _value is T value ? value : throw new InvalidOperationException();

        public T? SingleValueOrNull
            => (_value is T[]) ? throw new InvalidOperationException()
                : (T?)_value;

        public T[] MultipleValues
            => _value is T[] values ? values : _value is T value ? new T[] { value } : Array.Empty<T>();

        public object? Value
            => _value;

        public void Add(T value)
        {
            if (_value is null)
            {
                _value = value;
            }
            else if (_value is T existingValue)
            {
                _value = new T[] { existingValue, value };
            }
            else if (_value is T[] existingValues)
            {
                T[] values = new T[existingValues.Length + 1];
                existingValues.CopyTo(values, 0);
                values[^1] = value;
                _value = values;
            }
        }

        public static implicit operator GrowableArray<T>(T singleValue)
            => new() { _value = singleValue };

        public static implicit operator GrowableArray<T>(T[] multipleValues)
            => new() { _value = multipleValues };
    }
}
