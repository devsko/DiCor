using System;

namespace DiCor
{
    internal struct GrowingArray<T>
        where T : class
    {
        private object? _value;

        public int Length
            => _value is null ? 0 : _value is T ? 1 : ((T[])_value).Length;

        public T SingleValue
            => _value is T value ? value : throw new InvalidOperationException();

        public T[] MultipleValues
            => _value is T[] values ? values : _value is T value ? new T[] { value } : Array.Empty<T>();

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
    }
}
