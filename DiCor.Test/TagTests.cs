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
        public void Test()
        {
            var details = new Tag(0x7f33, 0x0010).GetDetails();
            details!.VM.ToString();
        }
    }
}
