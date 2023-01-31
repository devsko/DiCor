using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiCor.Values;
using Xunit;

namespace DiCor.Test.Values
{
    public class DATests
    {
        [Fact]
        public void NotInQuery()
        {
            var d1 = new DateOnly(2021, 1, 1);
            var d2 = new DateOnly(2022, 2, 2);

            var single = new DateTimeValue<DateOnly>(d1);
            Assert.Equal(d1, single.Value);
        }

        [Fact]
        public void InQuery()
        {
            var d1 = new DateOnly(2021, 1, 1);
            var d2 = new DateOnly(2022, 2, 2);

            var single = new DateTimeQueryValue<DateOnly>(QueryDateTime<DateOnly>.FromSingle(d1));
            Assert.True(single.QueryRange.IsSingle);
            Assert.False(single.QueryRange.IsRange);
            Assert.False(single.IsEmptyValue);
            Assert.Equal(d1, single.QueryRange.Single);
            Assert.Throws<InvalidOperationException>(() => single.QueryRange.Range);

            var range = new DateTimeQueryValue<DateOnly>(QueryDateTime<DateOnly>.FromRange(d1, d2));
            Assert.False(range.QueryRange.IsSingle);
            Assert.True(range.QueryRange.IsRange);
            Assert.False(range.IsEmptyValue);
            Assert.Equal(d1, range.QueryRange.Range.Lo);
            Assert.Equal(d2, range.QueryRange.Range.Hi);
            Assert.Throws<InvalidOperationException>(() => range.QueryRange.Single);

            var empty = new DateTimeQueryValue<DateOnly>(default(QueryEmpty));
            Assert.False(empty.QueryRange.IsSingle);
            Assert.False(empty.QueryRange.IsRange);
            Assert.True(empty.IsEmptyValue);
            Assert.Throws<InvalidOperationException>(() => empty.QueryRange.Single);
            Assert.Throws<InvalidOperationException>(() => empty.QueryRange.Range);
        }
    }
}
