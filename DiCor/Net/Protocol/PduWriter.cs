using System.Buffers;

using DiCor.Buffers;

namespace DiCor.Net.Protocol
{
    public class PduWriter
    {
        private const byte PduTypeAAssociateRq = 0x01;

        private const byte ItemTypeApplicationContext = 0x10;
        private const byte ItemTypePresentationContext = 0x20;
        private const byte ItemTypeAbstractSyntax = 0x30;
        private const byte ItemTypeTransferSyntax = 0x40;
        private const byte ItemTypeUserInformation = 0x50;
        private const byte ItemTypeMaximumLength = 0x51;
        private const byte ItemTypeImplementationClassUid = 0x52;
        private const byte ItemTypeAsynchronousOperations = 0x53;
        private const byte ItemTypeScpScuRoleSelection = 0x54;
        private const byte ItemTypeImplementationVersionName = 0x55;


        private readonly IBufferWriter<byte> _buffer;

        public PduWriter(IBufferWriter<byte> buffer)
        {
            _buffer = buffer;
        }

        public void WriteAAssociateReq(Association association)
        {
            var buffer = new BufferWriter(_buffer);

            buffer.Write(PduTypeAAssociateRq); // PDU-type
            buffer.Reserved(1);
            using (buffer.BeginLengthPrefix(sizeof(uint)))
            {
                buffer.Write((ushort)0x0001); // Protocol-version
                buffer.Reserved(2);
                buffer.WriteAscii(association.CalledAE, 16); // Called-AE-title
                buffer.WriteAscii(association.CallingAE, 16); // Calling-AE-title
                buffer.Reserved(32);

                // Application Context Item

                buffer.Write(ItemTypeApplicationContext); // Item-type
                buffer.Reserved(1);
                buffer.WriteAsciiWithLength(Uid.DICOMApplicationContextName.Value); // Application-context-name

                bool needsScpScuRoleNegotiation = false;
                foreach (PresentationContext presentationContext in association.PresentationContexts)
                {
                    needsScpScuRoleNegotiation |= (presentationContext.SupportsScuRole != null || presentationContext.SupportsScpRole != null);

                    // Presentation Context Item

                    buffer.Write(ItemTypePresentationContext); // Item-type
                    buffer.Reserved(1);
                    using (buffer.BeginLengthPrefix())
                    {
                        buffer.Write(presentationContext.Id); // Presentation-context-ID
                        buffer.Reserved(3);

                        // Abstract Syntax Sub-Item

                        buffer.Write(ItemTypeAbstractSyntax); // Item-Type
                        buffer.Reserved(1);
                        buffer.WriteAsciiWithLength(presentationContext.AbstractSyntax.Value); // Abstract-syntax-name

                        foreach (Uid transferSyntax in presentationContext.TransferSyntaxes)
                        {
                            // Transfer Syntax Sub-Item

                            buffer.Write(ItemTypeTransferSyntax); // Item-Type
                            buffer.Reserved(1);
                            buffer.WriteAsciiWithLength(transferSyntax.Value); // Abstract-syntax-name
                        }
                    }

                    // User Information Item

                    buffer.Write(ItemTypeUserInformation); // Item-Type
                    buffer.Reserved(1);
                    using (buffer.BeginLengthPrefix())
                    {
                        // Maximum Length

                        buffer.Write(ItemTypeMaximumLength); // Item-type
                        buffer.Reserved(1);
                        buffer.Write((ushort)0x0004); // Item-length
                        buffer.Write(association.MaxReceiveDataLength); // Maximum-length-received

                        // Implementation Class UID

                        buffer.Write(ItemTypeImplementationClassUid); // Item-type
                        buffer.Reserved(1);
                        buffer.WriteAsciiWithLength(Implementation.ClassUid.Value); // Implementation-class-uid

                        // Asynchronous Operations Window

                        buffer.Write(ItemTypeAsynchronousOperations); // Item-type
                        buffer.Reserved(1);
                        buffer.Write((ushort)0x0004); // Item-length
                        buffer.Write((ushort)0); // Maximum-number-operations-invoked
                        buffer.Write((ushort)0); // Maximum-number-operations-performed

                        if (needsScpScuRoleNegotiation)
                        {
                            foreach (PresentationContext presentationContext1 in association.PresentationContexts)
                            {
                                buffer.Write(ItemTypeScpScuRoleSelection); // Item-type
                                buffer.Reserved(1);
                                using (buffer.BeginLengthPrefix())
                                {
                                    buffer.WriteAsciiWithLength(presentationContext1.AbstractSyntax.Value); // SOP-class-uid
                                    buffer.Write((byte)((presentationContext1.SupportsScuRole ?? false) ? 0 : 1)); // SCU-role
                                    buffer.Write((byte)((presentationContext1.SupportsScpRole ?? false) ? 0 : 1)); // SCP-role
                                }
                            }
                        }

                        // Implementation Version Name

                        buffer.Write(ItemTypeImplementationVersionName); // Item-type
                        buffer.Reserved(1);
                        buffer.WriteAsciiWithLength(Implementation.VersionName); // Implementation-version-name
                    }
                }
            }
            buffer.Commit();
        }

    }
}
