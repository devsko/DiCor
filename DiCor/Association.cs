using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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
            Debug.Assert(type == AssociationType.Find);

            var presentationContext = new PresentationContext
            {
                AbstractSyntax = Uid.PatientRootQueryRetrieveInformationModelFIND
            };
            presentationContext.TransferSyntaxes.Add(Uid.ImplicitVRLittleEndian);

            CallingAE = "A";
            CalledAE = "B";
            PresentationContexts.Add(presentationContext);
        }

        public string? CalledAE { get; set; }
        public string? CallingAE { get; set; }
        public IList<PresentationContext> PresentationContexts { get; } = new List<PresentationContext>();
        public uint MaxResponseDataLength { get; set; } = DefaultMaxDataLength;
        public uint MaxRequestDataLength { get; set; }
        public ushort MaxOperationsInvoked { get; set; }
        public ushort MaxOperationsPerformed { get; set; }
        public Uid RemoteImplementationClass { get; set; }
        public string? RemoteImplementationVersion { get; set; }

        public PresentationContext? GetPresentationContext(byte id)
        {
            int index = (id - 1) / 2;
            if (index >= PresentationContexts.Count)
                return null;
            PresentationContext presentationContext = PresentationContexts[index];

            return presentationContext.Id == id ? presentationContext : null;
        }

        public PresentationContext? GetPresentationContext(in Uid uid)
        {
            foreach (PresentationContext pc in PresentationContexts)
                if (pc.AbstractSyntax == uid)
                    return pc;

            return null;
        }

    }
}
