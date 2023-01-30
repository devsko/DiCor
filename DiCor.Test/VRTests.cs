using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace DiCor.Test
{
    public class VRTests
    {
        [Fact]
        public void EndinessTest()
        {
            Assert.Equal(VR.CS, new VR("CS"u8));
        }
    }
}
