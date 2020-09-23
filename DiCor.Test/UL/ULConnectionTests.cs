using System.Threading.Tasks;

using DiCor.Net.UpperLayer;

using Xunit;

namespace DiCor.UL.Test
{
    public static class ULConnectionTests
    {
        [Fact]
        public static async Task Test()
        {
            await using (TestServer server = await TestServer.CreateAsysnc())
            await using (ULConnection connection = await server.AssociateAsync(AssociationType.Find))
            {
                Assert.Equal(ULConnection.State.Sta6_Ready, connection.CurrentState);
            }
        }
    }
}
