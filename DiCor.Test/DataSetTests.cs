using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiCor.Values;
using Xunit;

namespace DiCor.Test
{
    public class DataSetTests
    {
        [Fact]
        public void Variante1()
        {
            DataSet set = new(true);
            set.AddVRValue(new Tag(1, 1), new DAValue<IsNotQueryContext>(new DateOnly(2022, 1, 1)));
            if (set.TryGet(new Tag(1, 1), out DataItem item))
            {
                item.ValueRef<DAValue<IsNotQueryContext>>() = new DAValue<IsNotQueryContext>(new DateOnly(2022, 2, 2));
            }
            set.TryGetVRValue(new Tag(1, 1), out DAValue<IsNotQueryContext> value);

            Assert.False(value.IsDateRange);
            Assert.Equal(new DateOnly(2022, 2, 2), value.Date);
        }
        [Fact]
        public void Variante2()
        {
            DataSet set = new(true);
            set.Add(Tag.InstanceCreationDate, new DateOnly(2022, 1, 1));

            set.TryGet(Tag.InstanceCreationDate, out DateOnly date);


            if (set.TryGet(Tag.InstanceCreationDate, out DataItem item))
            {
                item.ValueRef<DAValue<IsNotQueryContext>>() = new DAValue<IsNotQueryContext>(new DateOnly(2022, 2, 2));
            }
            set.TryGetVRValue(Tag.InstanceCreationDate, out DAValue<IsNotQueryContext> value);

            Assert.True(value.IsSingleDate);
            Assert.Equal(new DateOnly(2022, 2, 2), value.Date);
        }
    }
}
