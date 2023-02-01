using System;
using System.Runtime.CompilerServices;

namespace DiCor.Values
{
    internal readonly struct DateTimeQueryValue<TDateTime> : IQueryableValue<DateTimeQueryValue<TDateTime>>
        where TDateTime : struct, IComparable<TDateTime>, IEquatable<TDateTime>
    {
        private readonly QueryDateTime<TDateTime> _queryDT;

        public DateTimeQueryValue(QueryDateTime<TDateTime> queryRange)
            => _queryDT = queryRange;

        public DateTimeQueryValue(QueryEmpty _)
            => _queryDT = QueryDateTime<TDateTime>.FromQueryEmpty();

        public bool IsEmptyValue
            => _queryDT.IsQueryEmpty;

        public static int PageSize
            => 10;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCompatible<T>()
            => (typeof(T) == typeof(TDateTime) ||
                typeof(T) == typeof((TDateTime, TDateTime)) ||
                typeof(T) == typeof(QueryDateTime<TDateTime>) ||
                typeof(T) == typeof(QueryEmpty));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DateTimeQueryValue<TDateTime> Create<T>(T content)
        {
            if (typeof(T) == typeof(TDateTime))
            {
                return new DateTimeQueryValue<TDateTime>(QueryDateTime<TDateTime>.FromSingle(Unsafe.As<T, TDateTime>(ref content)));
            }
            else if (typeof(T) == typeof((TDateTime, TDateTime)))
            {
                return new DateTimeQueryValue<TDateTime>(QueryDateTime<TDateTime>.FromRange(Unsafe.As<T, (TDateTime, TDateTime)>(ref content)));
            }
            else if (typeof(T) == typeof(QueryDateTime<TDateTime>))
            {
                return new DateTimeQueryValue<TDateTime>(Unsafe.As<T, QueryDateTime<TDateTime>>(ref content));
            }
            else if (typeof(T) == typeof(QueryEmpty))
            {
                return new DateTimeQueryValue<TDateTime>(QueryDateTime<TDateTime>.FromQueryEmpty());
            }
            else
            {
                Value.ThrowIncompatible<T>(nameof(DateTimeQueryValue<TDateTime>));
                return default;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Get<T>()
        {
            if (typeof(T) == typeof(TDateTime) && _queryDT.IsSingle)
            {
                TDateTime single = _queryDT.Single;
                return Unsafe.As<TDateTime, T>(ref single);
            }
            if (typeof(T) == typeof((TDateTime, TDateTime)) & _queryDT.IsRange)
            {
                (TDateTime Lo, TDateTime Hi) range = _queryDT.Range;
                return Unsafe.As<(TDateTime, TDateTime), T>(ref range);
            }
            else if (typeof(T) == typeof(QueryDateTime<TDateTime>))
            {
                return Unsafe.As<QueryDateTime<TDateTime>, T>(ref Unsafe.AsRef(in _queryDT));
            }
            else
            {
                Value.ThrowIncompatible<T>(nameof(DateTimeQueryValue<TDateTime>));
                return default;
            }
        }

        public override string? ToString()
            => _queryDT.ToString();
    }
}
