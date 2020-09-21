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
        public override async Task OnConnectedAsync(ConnectionContext connection)
        {
            try
            {
                await ULConnection.StartServiceAsync(connection).Unwrap().ConfigureAwait(false);
            }
            catch
            {

            }
        }
    }

    public class Program
    {
        public static async Task Main(string[] args)
        {
            ServiceProvider serviceProvider = new ServiceCollection()
                .AddLogging(builder => builder
                    .SetMinimumLevel(LogLevel.Debug)
                    .AddConsole()
                )
                .BuildServiceProvider();

            Server server = new ServerBuilder(serviceProvider)
                .UseSockets(builder => builder
                    .ListenLocalhost(11112, connection => connection
                        .UseConnectionLogging("Server")
                        .UseConnectionHandler<DiCorConnectionHandler>()
                ))
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
