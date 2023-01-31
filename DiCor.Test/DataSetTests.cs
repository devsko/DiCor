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
        [Fact]
        public void Variante2()
        {
            DataSet set = new(true);

            set.Set(Tag.InstanceCreationDate, new DateOnly(2022, 1, 1));
            set.TryGet(Tag.InstanceCreationDate, out DateOnly d);

            Assert.Equal(new DateOnly(2022, 1, 1), d);

            set.TryGet(Tag.InstanceCreationDate, out DataItem item);
            set.Set(Tag.InstanceCreationDate, new DateOnly(2022, 2, 2));

            Assert.Equal(new DateOnly(2022, 2, 2), item.GetValue<DateOnly>());
        }
    }
}
