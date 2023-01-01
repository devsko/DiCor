using System.Data.Common;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;

namespace DiCor.Net.UpperLayer
{
    public static class ConnectionBuilderExtensions
    {
        public static IConnectionBuilder RunULService(this IConnectionBuilder connectionBuilder)
        {
            ILoggerFactory? loggerFactory = (ILoggerFactory?)connectionBuilder.ApplicationServices?.GetService(typeof(ILoggerFactory));

            connectionBuilder.Run(async context =>
            {
                await ULConnection.RunScpAsync(context, ULScp.Default, loggerFactory).ConfigureAwait(false);
            });

            return connectionBuilder;
        }
    }
}
