using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.FindSymbols;

namespace DiCor
{
    public enum AssociationType
    {
        Find,
    }

    public class Association
    {
        public const uint DefaultMaxDataLength = 256 * 1024;

        public Association(AssociationType type)
        {
            // TODO

            var presentationContext = new PresentationContext
            {
                AbstractSyntax = Uid.PatientRootQueryRetrieveInformationModelFIND
            };
            presentationContext.TransferSyntaxes.Add(Uid.ImplicitVRLittleEndian);

            CallingAE = "A";
            CalledAE = "B";
            PresentationContexts.Add(presentationContext);
        }

        public uint MaxResponseDataLength { get; set; } = DefaultMaxDataLength;
        public uint MaxRequestDataLength { get; set; }
        public string? CalledAE { get; set; }
        public string? CallingAE { get; set; }
        public IList<PresentationContext> PresentationContexts { get; } = new List<PresentationContext>();
    }
}
