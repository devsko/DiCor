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
            if (expected is null)
            {
                throw new ArgumentNullException(nameof(expected));
            }

            if (producer is null)
            {
                throw new ArgumentNullException(nameof(producer));
            }

            var pipe = new Pipe();

            producer(pipe.Writer);

            pipe.Writer.Complete();
            var x = pipe.Reader.TryRead(out ReadResult result);

            var buffer = result.Buffer.ToArray();
            Equal(expected, buffer);
        }
    }
}
