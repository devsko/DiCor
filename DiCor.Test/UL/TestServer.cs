using System;
using System.Net;
using System.Threading.Tasks;
using Bedrock.Framework;
using Bedrock.Framework.Transports.Memory;
using DiCor.Net;
using DiCor.Net.UpperLayer;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DiCor.UL.Test
{
    public sealed class TestServer : IAsyncDisposable
    {
        public static async Task<TestServer> CreateAsync(bool useMemoryTransport = true)
        {
            ServiceProvider serviceProvider = new ServiceCollection()
                .AddLogging(builder => builder
                    .SetMinimumLevel(LogLevel.Trace)
                    .AddConsole()
                    .AddDebug()
                )
                .BuildServiceProvider();


            ClientBuilder clientBuilder = new(serviceProvider);
            ServerBuilder serverBuilder = new(serviceProvider);
            Client client;
            Server server;
            EndPoint endPoint;

            if (useMemoryTransport)
            {
                MemoryTransport memoryTransport = new();
                endPoint = MemoryEndPoint.Default;
                clientBuilder.UseConnectionFactory(memoryTransport);
                serverBuilder.Listen(endPoint, memoryTransport, RunULService);
            }
            else
            {
                endPoint = new IPEndPoint(IPAddress.Loopback, 11112);
                clientBuilder.UseSockets();
                serverBuilder.UseSockets(builder => builder.Listen(endPoint, RunULService));
            }

            client = clientBuilder.Build();
            server = serverBuilder.Build();

            await server.StartAsync();

            return new TestServer(server, client, serviceProvider, endPoint);

            static void RunULService(IConnectionBuilder builder) => builder.RunULService();
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

        public ValueTask CreateUserAsync(AssociationType type)
            => ULConnection.RunScuAsync(_client, _endPoint, type, ULScu.Default, _serviceProvider.GetService<ILoggerFactory>());

        public async ValueTask DisposeAsync()
        {
            await _server.StopAsync();
        }
    }
}
