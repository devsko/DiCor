using System;
using System.Collections.Generic;

namespace DiCor
{
    public class Association
    {
        public Association(string calledAE, string callingAE)
        {
            CalledAE = calledAE ?? throw new ArgumentNullException(nameof(calledAE));
            CallingAE = callingAE ?? throw new ArgumentNullException(nameof(callingAE));
        }

        public string CalledAE { get; }
        public string CallingAE { get; }
        public IEnumerable<PresentationContext> PresentationContexts { get; internal set; }
        public uint MaxReceiveDataLength { get; internal set; }
    }
}
