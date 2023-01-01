using System;
using System.Net;
using System.Threading;
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
        private class MyULScu : ULScu
        {
            private readonly Func<ULConnection, Task> _onAAssociateAccepted;
            private readonly Func<ULConnection, Pdu.RejectResult, Pdu.RejectSource, Pdu.RejectReason, Task> _onAAssociateRejected;
            private readonly Func<ULConnection, bool, Task> _onAReleaseRequested;
            private readonly Func<ULConnection, Task> _onAReleaseConfirmed;
            private readonly Func<ULConnection, Pdu.AbortSource, Pdu.AbortReason, Task> _onAPAbort;

            public MyULScu(
                Func<ULConnection, Task>? onAAssociateAccepted,
                Func<ULConnection, Pdu.RejectResult, Pdu.RejectSource, Pdu.RejectReason, Task>? onAAssociateRejected,
                Func<ULConnection, bool, Task>? onAReleaseRequested,
                Func<ULConnection, Task>? onAReleaseConfirmed,
                Func<ULConnection, Pdu.AbortSource, Pdu.AbortReason, Task>? onAPAbort)
            {
                _onAAssociateAccepted = onAAssociateAccepted ?? (connection => Task.CompletedTask);
                _onAAssociateRejected = onAAssociateRejected ?? ((connection, result, source, reason) => Task.CompletedTask);
                _onAReleaseRequested = onAReleaseRequested ?? ((connection, isCollision) => Task.CompletedTask);
                _onAReleaseConfirmed = onAReleaseConfirmed ?? (connection => Task.CompletedTask);
                _onAPAbort = onAPAbort ?? ((connection, source, reason) => Task.CompletedTask);
            }

            public override Task AAssociateAccepted(ULConnection connection, CancellationToken cancellationToken = default)
                => _onAAssociateAccepted(connection);
            public override Task AAssociateRejected(ULConnection connection, Pdu.RejectResult result, Pdu.RejectSource source, Pdu.RejectReason reason, CancellationToken cancellationToken = default)
                => _onAAssociateRejected(connection, result, source, reason);
            public override Task AReleaseRequested(ULConnection connection, bool isCollision, CancellationToken cancellationToken = default)
                => _onAReleaseRequested(connection, isCollision);
            public override Task AReleaseConfirmed(ULConnection connection, CancellationToken cancellationToken = default)
                => _onAReleaseConfirmed(connection);
            public override Task APAbort(ULConnection connection, Pdu.AbortSource source, Pdu.AbortReason reason)
                => _onAPAbort(connection, source, reason);
        }

        [Fact]
        public async Task AAssociateReq()
        {
            var services = new ServiceCollection();
            services.AddLogging(builder => builder
                .SetMinimumLevel(LogLevel.Trace)
                .AddDebug());

            ServiceProvider serviceProvider = services.BuildServiceProvider();

            Client client = new ClientBuilder(serviceProvider)
                .UseSockets()
                .UseConnectionLogging()
                .Build();

            await ULConnection.RunScuAsync(
                client,
                new DnsEndPoint("dicomserver.co.uk", 11112),
                AssociationType.Find,
                new MyULScu(OnAAssociateAccepted, null, null, null, null),
                serviceProvider.GetService<ILoggerFactory>()).ConfigureAwait(false);

            static async Task OnAAssociateAccepted(ULConnection connection)
            {
                Assert.Equal(ULConnection.ConnectionState.Sta6_Ready, await connection.GetCurrentStateAsync().ConfigureAwait(false));

                await connection.RequestReleaseAsync().ConfigureAwait(false);
            }
        }
    }
}
