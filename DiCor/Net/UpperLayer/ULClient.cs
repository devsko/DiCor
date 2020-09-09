using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Bedrock.Framework;

namespace DiCor.Net.UpperLayer
{
    public class ULClient
    {
        public Client Client { get; }

        public ULClient(Client client)
        {
            Client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public async Task<ULConnection> AssociateAsync(EndPoint endpoint, AssociationType type, CancellationToken cancellationToken = default)
        {
            ULConnection ulConnection = new ULConnection(this, endpoint);
            await ulConnection.AssociateAsync(type, cancellationToken).ConfigureAwait(false);
            return ulConnection;
        }
    }
}
