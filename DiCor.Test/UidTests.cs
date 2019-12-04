using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace DiCor.Test
{
    public class UidTests
    {
        [Fact]
        public void Generate()
        {
            var uid = Uid.NewUid("test", UidType.SOPInstance);
            uid.ToString();
        }
    }
}
