using System;
using System.Net;
using System.Threading.Tasks;
using Bedrock.Framework;
using Bedrock.Framework.Transports.Memory;
using DiCor.Net.UpperLayer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DiCor.UL.Test
{
    public class TestServer : IAsyncDisposable
    {
        public static async Task<TestServer> CreateAsysnc(bool useMemoryTransport = true)
        {
            ServiceProvider serviceProvider = new ServiceCollection()
                .AddLogging(builder => builder
                    .SetMinimumLevel(LogLevel.Trace)
                    .AddConsole()
                    .AddDebug()
                )
                .BuildServiceProvider();

            MemoryTransport? memoryTransport = useMemoryTransport ? new MemoryTransport() : null;

            var clientBuilder = new ClientBuilder(serviceProvider);
            Client client =
                (useMemoryTransport
                    ? clientBuilder.UseConnectionFactory(memoryTransport)
                    : clientBuilder.UseSockets()
                )
                .Build();

            var serverBuilder = new ServerBuilder(serviceProvider);
            Server server =
                (useMemoryTransport
                    ? serverBuilder.Listen(MemoryEndPoint.Default, memoryTransport, connection => connection
                        .RunULService()
                    )
                    : serverBuilder.UseSockets(builder => builder
                        .ListenLocalhost(11112, connection => connection
                            .RunULService()
                        )
                    )
                )
                .Build();

            await server.StartAsync();

            EndPoint endPoint = useMemoryTransport
                ? MemoryEndPoint.Default
                : new IPEndPoint(IPAddress.Loopback, 11112);

            return new TestServer(server, client, serviceProvider, endPoint);
        }

        private readonly Server _server;
        private readonly Client _client;
        private readonly ServiceProvider _serviceProvider;
        private readonly EndPoint _endPoint;

        private TestServer(Server server, Client client, ServiceProvider serviceProvider, EndPoint endPoint)
        {
            _server = server;
            _client = client;
            _serviceProvider = serviceProvider;
            _endPoint = endPoint;
        }

        public async Task<ULConnection> AssociateAsync(AssociationType type)
        {
            var connection = new ULConnection(_serviceProvider.GetService<ILoggerFactory>());
            await connection.AssociateAsync(_client, _endPoint, AssociationType.Find);

            return connection;
        }

        public async ValueTask DisposeAsync()
        {
            await _server.StopAsync();
        }
    }
}
