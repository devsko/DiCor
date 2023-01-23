using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace DiCor.Test
{
    public class TagTests
    {
        [Fact]
        public void Tag()
        {
            Tag tag = new Tag(0x0014, 0x3050);
            var details = tag.GetDetails()!;
            VM vm = details.VM;
        }

        [Fact]
        public void TestVR()
        {
            var vr = new VR("AE"u8);
            var details = vr.GetDetails();
            var s = vr.ToString();
        }
    }
}
