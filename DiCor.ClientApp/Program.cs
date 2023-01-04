using System.Net;
using System.Threading.Tasks;

using Bedrock.Framework;
using DiCor.Net;
using DiCor.Net.UpperLayer;

using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DiCor.ConsoleApp
{
    public class DiCorConnectionHandler : ConnectionHandler
    {
        private readonly ILoggerFactory? _loggerFactory;

        public DiCorConnectionHandler(ILoggerFactory? loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }

        public override async Task OnConnectedAsync(ConnectionContext context)
        {
            try
            {
                await ULConnection.RunScpAsync(context, ULScp.Default, _loggerFactory).ConfigureAwait(false);
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
                    .SetMinimumLevel(LogLevel.Trace)
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

            EndPoint endpoint = new IPEndPoint(IPAddress.Loopback, 11112);
            //EndPoint endpoint = new IPEndPoint(new IPAddress(new byte[] { 51, 75, 171, 41 }), 11112);
            //EndPoint endpoint = new DnsEndPoint("dicomserver.co.uk", 11112);

            await ULConnection.RunScuAsync(
                client,
                endpoint,
                AssociationType.Find,
                ULScu.Default,
                serviceProvider.GetService<ILoggerFactory>()).ConfigureAwait(false);
        }

    }
}
