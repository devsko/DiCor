﻿using Xunit;

namespace DiCor.Test
{
    public class UidTests
    {
        [Fact]
        public void Generate()
        {
            var uid = Uid.NewUid("test", UidType.SOPInstance);
            var str = uid.ToString();
        }
    }
}
