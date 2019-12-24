﻿using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Bedrock.Framework;
using DiCor.Buffers;
using DiCor.Net.UpperLayer;
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

            ULClient ulClient = new ULClient(client);

            ULConnection ulConnection = await ulClient
                .AssociateAsync(new DnsEndPoint("dicomserver.co.uk", 11112), AssociationType.Find, new CancellationTokenSource(600).Token)
                .ConfigureAwait(false);

            Console.WriteLine(ulConnection.State);
        }

    }
}
