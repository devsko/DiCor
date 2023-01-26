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

            var single = new DAValue<NotInQuery>(d1);
            Assert.True(single.IsSingleDate);
            Assert.False(single.IsDateRange);
            Assert.False(single.IsEmptyValue);
            Assert.Equal(d1, single.Date);
            Assert.Throws<InvalidOperationException>(() => single.DateRange);

            Assert.Throws<InvalidOperationException>(() => new DAValue<NotInQuery>(d1, d2));
            Assert.Throws<InvalidOperationException>(() => new DAValue<NotInQuery>(default(EmptyValue)));
        }

        [Fact]
        public void InQuery()
        {
            var d1 = new DateOnly(2021, 1, 1);
            var d2 = new DateOnly(2022, 2, 2);

            var single = new DAValue<InQuery>(d1);
            Assert.True(single.IsSingleDate);
            Assert.False(single.IsDateRange);
            Assert.False(single.IsEmptyValue);
            Assert.Equal(d1, single.Date);
            Assert.Throws<InvalidOperationException>(() => single.DateRange);

            var range = new DAValue<InQuery>(d1, d2);
            Assert.False(range.IsSingleDate);
            Assert.True(range.IsDateRange);
            Assert.False(range.IsEmptyValue);
            Assert.Equal((d1, d2), range.DateRange);
            Assert.Throws<InvalidOperationException>(() => range.Date);

            var empty = new DAValue<InQuery>(default(EmptyValue));
            Assert.False(empty.IsSingleDate);
            Assert.False(empty.IsDateRange);
            Assert.True(empty.IsEmptyValue);
            Assert.Throws<InvalidOperationException>(() => empty.Date);
            Assert.Throws<InvalidOperationException>(() => empty.DateRange);
        }
    }
}
