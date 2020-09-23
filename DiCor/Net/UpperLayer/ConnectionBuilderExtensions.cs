using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DiCor.Net.UpperLayer;

using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DiCor.Net.UpperLayer
{
    public static class ConnectionBuilderExtensions
    {
        public static IConnectionBuilder RunULService(this IConnectionBuilder connectionBuilder)
        {
            ILoggerFactory? loggerFactory = (ILoggerFactory?)connectionBuilder.ApplicationServices?.GetService(typeof(ILoggerFactory));

            connectionBuilder.Run(async connection =>
            {
                await using (var ulConnection = new ULConnection(loggerFactory))
                {
                    await ulConnection.StartServiceAsync(connection).Unwrap();
                }
            });

            return connectionBuilder;
        }
    }
}
