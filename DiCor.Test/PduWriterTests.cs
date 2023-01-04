using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Bedrock.Framework;
using DiCor.Net;
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
            private readonly Func<ULConnection, ValueTask> _onAssociationAccepted;
            private readonly Func<ULConnection, Pdu.RejectResult, Pdu.RejectSource, Pdu.RejectReason, ValueTask> _onAssociationRejected;
            private readonly Func<ULConnection, bool, ValueTask> _onReleaseRequested;
            private readonly Func<ULConnection, ValueTask> _onReleaseConfirmed;
            private readonly Func<ULConnection, Pdu.AbortSource, Pdu.AbortReason, ValueTask> _onAbortReceived;

            public MyULScu(
                Func<ULConnection, ValueTask>? onAssociationAccepted,
                Func<ULConnection, Pdu.RejectResult, Pdu.RejectSource, Pdu.RejectReason, ValueTask>? onAssociationRejected,
                Func<ULConnection, bool, ValueTask>? onReleaseRequested,
                Func<ULConnection, ValueTask>? onReleaseConfirmed,
                Func<ULConnection, Pdu.AbortSource, Pdu.AbortReason, ValueTask>? onAbortReceived)
            {
                _onAssociationAccepted = onAssociationAccepted ?? (connection => ValueTask.CompletedTask);
                _onAssociationRejected = onAssociationRejected ?? ((connection, result, source, reason) => ValueTask.CompletedTask);
                _onReleaseRequested = onReleaseRequested ?? ((connection, isCollision) => ValueTask.CompletedTask);
                _onReleaseConfirmed = onReleaseConfirmed ?? (connection => ValueTask.CompletedTask);
                _onAbortReceived = onAbortReceived ?? ((connection, source, reason) => ValueTask.CompletedTask);
            }

            public override ValueTask AssociationAccepted(ULConnection connection, CancellationToken cancellationToken = default)
                => _onAssociationAccepted(connection);
            public override ValueTask AssociationRejected(ULConnection connection, Pdu.RejectResult result, Pdu.RejectSource source, Pdu.RejectReason reason, CancellationToken cancellationToken = default)
                => _onAssociationRejected(connection, result, source, reason);
            public override ValueTask ReleaseRequested(ULConnection connection, bool isCollision, CancellationToken cancellationToken = default)
                => _onReleaseRequested(connection, isCollision);
            public override ValueTask ReleaseConfirmed(ULConnection connection, CancellationToken cancellationToken = default)
                => _onReleaseConfirmed(connection);
            public override ValueTask AbortReceived(ULConnection connection, Pdu.AbortSource source, Pdu.AbortReason reason)
                => _onAbortReceived(connection, source, reason);
        }

        [Fact]
        public async Task RunScu()
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

            bool associationAccepted = false;

            await ULConnection.RunScuAsync(
                client,
                new DnsEndPoint("dicomserver.co.uk", 11112),
                AssociationType.Find,
                new MyULScu(OnAssociationAccepted, null, null, null, null),
                serviceProvider.GetService<ILoggerFactory>()).ConfigureAwait(false);

            Assert.True(associationAccepted);

            async ValueTask OnAssociationAccepted(ULConnection connection)
            {
                Assert.Equal(ULConnection.ConnectionState.Sta6_Ready, await connection.GetCurrentStateAsync().ConfigureAwait(false));

                associationAccepted = true;
                await connection.RequestReleaseAsync().ConfigureAwait(false);
            }
        }
    }
}
