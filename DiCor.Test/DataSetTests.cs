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
        public void Variante1()
        {
            DataSet set = new(true);
            set.AddVRValue(new Tag(1, 1), new DAValue<NotInQuery>(new DateOnly(2022, 1, 1)));
            if (set.TryGet(new Tag(1, 1), out DataItem item))
            {
                item.ValueRef<DAValue<NotInQuery>>() = new DAValue<NotInQuery>(new DateOnly(2022, 2, 2));
            }
            set.TryGetVRValue(new Tag(1, 1), out DAValue<NotInQuery> value);

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
                item.ValueRef<DAValue<NotInQuery>>() = new DAValue<NotInQuery>(new DateOnly(2022, 2, 2));
            }
            set.TryGetVRValue(Tag.InstanceCreationDate, out DAValue<NotInQuery> value);

            Assert.True(value.IsSingleDate);
            Assert.Equal(new DateOnly(2022, 2, 2), value.Date);
        }


        [Fact]
        public void FoDicom()
        {
            DicomDataset set = new();
            var tagDetails = DicomDictionary.Default[new DicomTag(0x0014, 0x3050)];

            set.Add(tagDetails.Tag, 100);
        }

    }
}
