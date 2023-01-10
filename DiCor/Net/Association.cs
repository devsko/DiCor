using System.Diagnostics;

namespace DiCor.Net
{
    public enum AssociationType
    {
        Find,
    }

    public sealed record Association
    {
        public const uint DefaultMaxDataLength = 256 * 1024;

        public Association()
        { }

        public Association(AssociationType type)
        {
            // TODO
            Debug.Assert(type == AssociationType.Find);

            var presentationContext = new PresentationContext
            {
                AbstractSyntax = Uid.PatientRootQueryRetrieveInformationModelFind,
            };
            presentationContext.TransferSyntaxes.Add(Uid.ImplicitVRLittleEndian);

            CallingAE = "A"u8.ToArray();
            CalledAE = "B"u8.ToArray();
            PresentationContexts.Add(presentationContext);
        }

        public byte[]? CalledAE { get; set; }
        public byte[]? CallingAE { get; set; }
        public Uid ApplicationContext { get; set; } = Uid.DICOMApplicationContext;
        public IList<PresentationContext> PresentationContexts { get; } = new List<PresentationContext>();
        public uint MaxResponseDataLength { get; set; } = DefaultMaxDataLength;
        public uint MaxRequestDataLength { get; set; }
        public ushort MaxOperationsInvoked { get; set; }
        public ushort MaxOperationsPerformed { get; set; }
        public Uid RemoteImplementationClass { get; set; }
        public byte[]? RemoteImplementationVersion { get; set; }

        public PresentationContext? GetPresentationContext(byte id)
        {
            int index = (id - 1) / 2;
            if (index >= PresentationContexts.Count)
                return null;
            PresentationContext presentationContext = PresentationContexts[index];

            return presentationContext.Id == id ? presentationContext : null;
        }

        public PresentationContext? GetPresentationContext(Uid uid)
        {
            foreach (PresentationContext pc in PresentationContexts)
                if (pc.AbstractSyntax == uid)
                    return pc;

            return null;
        }
    }
}
