using System;
using System.Net;
using System.Threading.Tasks;

using Bedrock.Framework;

using DiCor.Net.Protocol;

using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DiCor.ConsoleApp
{
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

            Client client = new ClientBuilder(serviceProvider)
                .UseSockets()
                .UseConnectionLogging()
                .Build();

            await using (ConnectionContext connection = await client.ConnectAsync(new DnsEndPoint("dicomserver.co.uk", 11112)))
            {
                Console.WriteLine($"Connected {connection.LocalEndPoint} to {connection.RemoteEndPoint}");

                X(connection);
                await connection.Transport.Output.FlushAsync();
            }

            static void X(ConnectionContext connection)
            {
                var pduWriter = new PduWriter(connection.Transport.Output);
                pduWriter.WriteAAssociateReq(null!);

            }
        }

    }
}
