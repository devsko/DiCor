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
            Assert.Equal(d1, single.Get<DateOnly>());
        }

        [Fact]
        public void InQuery()
        {
            var d1 = new DateOnly(2021, 1, 1);
            var d2 = new DateOnly(2022, 2, 2);

            var single = new DateTimeQueryValue<DateOnly>(QueryDateTime<DateOnly>.FromSingle(d1));
            Assert.True(single.Get<QueryDateTime<DateOnly>>().IsSingle);
            Assert.False(single.Get<QueryDateTime<DateOnly>>().IsRange);
            Assert.False(single.IsEmptyValue);
            Assert.Equal(d1, single.Get<QueryDateTime<DateOnly>>().Single);
            Assert.Throws<InvalidOperationException>(() => single.Get<QueryDateTime<DateOnly>>().Range);

            var range = new DateTimeQueryValue<DateOnly>(QueryDateTime<DateOnly>.FromRange(d1, d2));
            Assert.False(range.Get<QueryDateTime<DateOnly>>().IsSingle);
            Assert.True(range.Get<QueryDateTime<DateOnly>>().IsRange);
            Assert.False(range.IsEmptyValue);
            Assert.Equal(d1, range.Get<QueryDateTime<DateOnly>>().Range.Lo);
            Assert.Equal(d2, range.Get<QueryDateTime<DateOnly>>().Range.Hi);
            Assert.Throws<InvalidOperationException>(() => range.Get<QueryDateTime<DateOnly>>().Single);

            var empty = new DateTimeQueryValue<DateOnly>(default(QueryEmpty));
            Assert.False(empty.Get<QueryDateTime<DateOnly>>().IsSingle);
            Assert.False(empty.Get<QueryDateTime<DateOnly>>().IsRange);
            Assert.True(empty.IsEmptyValue);
            Assert.Throws<InvalidOperationException>(() => empty.Get<QueryDateTime<DateOnly>>().Single);
            Assert.Throws<InvalidOperationException>(() => empty.Get<QueryDateTime<DateOnly>>().Range);
        }
    }
}
