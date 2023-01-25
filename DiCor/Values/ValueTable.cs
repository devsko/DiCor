using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace DiCor.Values
{
    internal interface IValueTable
    {
        ushort Add(AbstractValue value);
        ref AbstractValue this[ushort index] { get; }
    }

    internal sealed class ValueTable<TValue> : IValueTable
        where TValue : struct, IValue<TValue>
    {
        private static readonly int s_pageSize = TValue.PageSize;

        private readonly List<TValue[]> _pages;
        private TValue[] _currentPage;
        private int _index;

        public ValueTable()
        {
            _pages = new List<TValue[]>(1);
            AddPage();
        }

        public ushort Count
            => (ushort)((_pages.Count - 1) * s_pageSize + _index);

        public ref TValue this[int index]
        {
            get
            {
                (int q, int r) = Math.DivRem(index, s_pageSize);
                TValue[] page = _pages[q];

                if (page == _currentPage)
                    ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(r, _index);

                return ref page[r];
            }
        }

        ushort IValueTable.Add(AbstractValue value)
        {
            AddDefault(out ushort index) = Unsafe.As<AbstractValue, TValue>(ref value);
            return index;
        }

        ref AbstractValue IValueTable.this[ushort index]
            => ref Unsafe.As<TValue, AbstractValue>(ref this[index]);

        [MemberNotNull(nameof(_currentPage))]
        private void AddPage()
            => _pages.Add(_currentPage = new TValue[s_pageSize]);

        public ref TValue AddDefault(out ushort index)
        {
            if (_index == s_pageSize)
            {
                AddPage();
            }
            index = Count;

            return ref _currentPage[_index++];
        }
    }
}
