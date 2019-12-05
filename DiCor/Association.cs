using System;
using System.Collections.Generic;

namespace DiCor
{
    public class Association
    {
        public const uint DefaultMaxDataLength = 256 * 1024;

        public uint MaxReceiveDataLength { get; set; } = DefaultMaxDataLength;
        public string? CalledAE { get; set; }
        public string? CallingAE { get; set; }
        public IList<PresentationContext> PresentationContexts { get; } = new List<PresentationContext>();
        public IDictionary<byte, PresentationContext> PresentationContextsById { get; } = new Dictionary<byte, PresentationContext>();
    }
}
