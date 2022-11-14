using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;

namespace DiCor.Test.Buffers
{
    public class Assert : Xunit.Assert
    {
        public static void Produces(IEnumerable<byte> expected, Action<PipeWriter> producer)
        {
            ArgumentNullException.ThrowIfNull(expected);
            ArgumentNullException.ThrowIfNull(producer);

            var pipe = new Pipe();

            producer(pipe.Writer);

            pipe.Writer.Complete();
            pipe.Reader.TryRead(out ReadResult result);

            byte[] buffer = result.Buffer.ToArray();
            Equal(expected, buffer);
        }
    }
}
