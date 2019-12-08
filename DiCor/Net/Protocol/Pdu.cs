namespace DiCor.Net.Protocol
{
    public static class Pdu
    {
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

        public const byte ItemTypeApplicationContext = 0x10;
        public const byte ItemTypePresentationContext = 0x20;
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
