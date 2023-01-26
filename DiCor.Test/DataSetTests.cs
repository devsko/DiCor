using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiCor.Values;
using FellowOakDicom;
using Xunit;

namespace DiCor.Test
{
    public class DataSetTests
    {
        //[Fact]
        //public void Variante1()
        //{
        //    DataSet set = new(true);
        //    set.AddVRValue(new Tag(1, 1), new DAValue<NotInQuery>(new DateOnly(2022, 1, 1)));
        //    if (set.TryGet(new Tag(1, 1), out DataItem item))
        //    {
        //        item.ValueRef<DAValue<NotInQuery>>() = new DAValue<NotInQuery>(new DateOnly(2022, 2, 2));
        //    }
        //    set.TryGetVRValue(new Tag(1, 1), out DAValue<NotInQuery> value);

        //    Assert.False(value.IsDateRange);
        //    Assert.Equal(new DateOnly(2022, 2, 2), value.Date);
        //}

        [Fact]
        public void Variante2()
        {
            DataSet set = new(true);

            set.Set(Tag.InstanceCreationDate, new DateOnly(2022, 1, 1));
            set.TryGet(Tag.InstanceCreationDate, out DataItem item);
            set.Set(Tag.InstanceCreationDate, new DateOnly(2022, 2, 2));
            var date = item.GetValue<DateOnly>();

            Assert.Equal(new DateOnly(2022, 2, 2), date);
        }
    }
}
