using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace DiCor.Performance
{
    public class TagTests
    {
        [Benchmark]
        public void Details()
        {
            var details = new Tag(0x7f33, 0x0010).GetDetails();
        }
    }
}
