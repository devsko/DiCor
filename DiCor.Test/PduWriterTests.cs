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
            services.AddLogging(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Debug);
                builder.AddConsole();
            });

            ServiceProvider serviceProvider = services.BuildServiceProvider();

            ILogger logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<PduWriterTests>();

            Client client = new ClientBuilder(serviceProvider)
                .UseSockets()
                .UseConnectionLogging()
                .Build();

            ULClient ulClient = new ULClient(client);
            ULConnection ulConnection = await ulClient
                .AssociateAsync(new DnsEndPoint("dicomserver.co.uk", 11112), AssociationType.Find)
                .ConfigureAwait(false);
        }
    }
}
