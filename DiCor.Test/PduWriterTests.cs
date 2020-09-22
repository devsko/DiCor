using System.Net;
using System.Threading.Tasks;

using Bedrock.Framework;

using DiCor.Net.UpperLayer;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Xunit;

namespace DiCor.Test
{
    public class PduWriterTests
    {
        [Fact]
        public async Task AAssociateReq()
        {
            var services = new ServiceCollection();
            services.AddLogging(builder => builder
                .SetMinimumLevel(LogLevel.Debug)
                .AddDebug());

            ServiceProvider serviceProvider = services.BuildServiceProvider();

            Client client = new ClientBuilder(serviceProvider)
                .UseSockets()
                .UseConnectionLogging()
                .Build();

            ULConnection ulConnection = await ULConnection.AssociateAsync(
                client,
                new DnsEndPoint("dicomserver.co.uk", 11112),
                AssociationType.Find).ConfigureAwait(false);

            Assert.Equal(ULConnection.State.Sta6_Ready, ulConnection.CurrentState);
        }
    }
}
