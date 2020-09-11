namespace DiCor.Net.UpperLayer
{
    public static class Pdu
    {
        public enum Type : byte
        {
            AAssociateRq = 0x01,
            AAssociateAc = 0x02,
            AAssociateRj = 0x03,
            AAbort = 0x07,
        }

        public enum RejectResult : byte
        {
            Permanent = 0x01,
            Transient = 0x02,
        }
        public enum RejectSource : byte
        {
            ServiceUser = 0x01,
            ServiceProviderAcse = 0x02,
            ServiceProviderPresentation = 0x03,
        }

        public enum RejectReason : byte
        {
            NoReasonGiven = 0x01,
            ApplicationContextNameNotSupported = 0x02,
            CallingAETitleNotRecognized = 0x03,
            CalledAETitleNotRecognized = 0x07,

            AcseDiff = 0x20,
            AcseNoReasonGiven = 0x21,
            AcseProtocolVersionNotSupported = 0x22,

            PresentationDiff = 0x40,
            PresentationTemporaryCongestio = 0x41,
            PresentationLocalLimitExceeded = 0x42,
        }

        public enum AbortSource : byte
        {
            ServiceUser = 0x00,
            ServiceProvider = 0x02,
        }

        public enum AbortReason : byte
        {
            ReasonNotSpecified = 0x00,
            UnrecognizedPdu = 0x01,
            UnexpectedPdu = 0x02,
            UnrecognizedPduParameter = 0x04,
            UnexpectedPduParameter = 0x05,
            InvalidPduParameterValue = 0x06,
        }

        public enum PresentationContextItemResult : byte
        {
            Acceptance = 0x00,
            UserRejection = 0x01,
            ProviderRejectionNoReason = 0x02,
            ProviderRejectionAbstractSyntaxNotSupported = 0x03,
            ProviderRejectionTransferSyntaxNotSupported = 0x04,
        }

        public const byte ItemTypeApplicationContext = 0x10;
        public const byte ItemTypePresentationContextRq = 0x20;
        public const byte ItemTypePresentationContextAc = 0x21;
        public const byte ItemTypeAbstractSyntax = 0x30;
        public const byte ItemTypeTransferSyntax = 0x40;
        public const byte ItemTypeUserInformation = 0x50;
        public const byte ItemTypeMaximumLength = 0x51;
        public const byte ItemTypeImplementationClassUid = 0x52;
        public const byte ItemTypeAsynchronousOperations = 0x53;
        public const byte ItemTypeScpScuRoleSelection = 0x54;
        public const byte ItemTypeImplementationVersionName = 0x55;
    }
}
