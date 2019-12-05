using System;
using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net;
using System.Threading.Tasks;
using DiCor.Net.Protocol;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Connections;
using Xunit;

namespace DiCor.Test
{
    public class PduWriterTests
    {
        [Fact]
        public async Task AAssociateReq()
        {
            await using (var connection = new Bedrock.Framework.SocketConnection(new DnsEndPoint("dicomserver.co.uk", 11112)))
            {
                await connection.StartAsync();

                Console.WriteLine(connection.LocalEndPoint);
                Console.WriteLine(connection.RemoteEndPoint);

                PipeWriter output = connection.Transport.Output;
                PipeReader input = connection.Transport.Input;

                var association = new Association();
                var presentationContext = new PresentationContext();
                association.PresentationContexts.Add(presentationContext);
                presentationContext.AbstractSyntax = Uid.PatientRootQueryRetrieveInformationModelFIND;
                presentationContext.TransferSyntaxes.Add(Uid.ImplicitVRLittleEndian);

                new PduWriter(output).WriteAAssociateReq(association);
                FlushResult flush = await output.FlushAsync();


                while (true)
                {
                    ReadResult result = await input.ReadAsync();
                    Process(result.Buffer);

                    if (result.IsCompleted)
                        break;

                    void Process(ReadOnlySequence<byte> buffer)
                    {
                        var reader = new PduReader(buffer);
                        SequencePosition consumed = reader.Position;

                        while (reader.TryRead())
                            input.AdvanceTo(consumed = reader.Position);

                        if (!consumed.Equals(buffer.End))
                            input.AdvanceTo(consumed, buffer.End);
                    }
                }
            }
        }
    }
}
