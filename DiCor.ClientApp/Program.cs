using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Bedrock.Framework;

using DiCor.Net.UpperLayer;

using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DiCor.ConsoleApp
{
    public class DiCorConnectionHandler : ConnectionHandler
    {
        public override Task OnConnectedAsync(ConnectionContext connection)
        {
            return ULConnection.StartServiceAsync(connection);
        }
    }

    public class Program
    {
        public static async Task Main(string[] args)
        {
            var services = new ServiceCollection();
            services.AddLogging(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Debug);
                builder.AddConsole();
            });

            ServiceProvider serviceProvider = services.BuildServiceProvider();

            ILogger logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<Program>();

            Server server = new ServerBuilder(serviceProvider)
                .UseSockets(builder
                    => builder.ListenLocalhost(11112, b
                        => b.UseConnectionLogging()
                            .UseConnectionHandler<DiCorConnectionHandler>()))
                .Build();

            await server.StartAsync().ConfigureAwait(false);

            Client client = new ClientBuilder(serviceProvider)
                .UseSockets()
                .UseConnectionLogging()
                .Build();

            await using (var connection = await ULConnection.AssociateAsync(
                client,
                new IPEndPoint(IPAddress.Loopback, 11112),
                //new DnsEndPoint("dicomserver.co.uk", 11112),
                AssociationType.Find,
                new CancellationTokenSource(60000).Token).ConfigureAwait(false))
            {
                Console.WriteLine(connection.CurrentState);
            }

        }

    }
}
