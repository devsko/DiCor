using System.Buffers;

using DiCor.Buffers;

namespace DiCor.Net.Protocol
{
    public ref struct PduWriter
    {
        private BufferWriter _buffer;

        public PduWriter(BufferWriter buffer)
        {
            _buffer = buffer;
        }

        public void WriteAAssociateRq(Association association)
        {
            _buffer.Write((ushort)0x0001); // Protocol-version
            _buffer.Reserved(2);
            _buffer.WriteAsciiFixed(association.CalledAE, 16); // Called-AE-title
            _buffer.WriteAsciiFixed(association.CallingAE, 16); // Calling-AE-title
            _buffer.Reserved(32);

            // Application Context Item

            _buffer.Write(Pdu.ItemTypeApplicationContext); // Item-type
            _buffer.Reserved(1);
            _buffer.WriteAscii(Uid.DICOMApplicationContextName.Value); // Application-context-name

            bool needsScpScuRoleNegotiation = false;
            byte presentationContextId = 1;
            foreach (PresentationContext presentationContext in association.PresentationContexts)
            {
                // Presentation Context Item

                _buffer.Write(Pdu.ItemTypePresentationContext); // Item-type
                _buffer.Reserved(1);
                using (_buffer.BeginLengthPrefix())
                {
                    _buffer.Write(presentationContext.Id = presentationContextId); // Presentation-context-ID
                    _buffer.Reserved(3);

                    // Abstract Syntax Sub-Item

                    _buffer.Write(Pdu.ItemTypeAbstractSyntax); // Item-Type
                    _buffer.Reserved(1);
                    _buffer.WriteAscii(presentationContext.AbstractSyntax.Value); // Abstract-syntax-name

                    foreach (Uid transferSyntax in presentationContext.TransferSyntaxes)
                    {
                        // Transfer Syntax Sub-Item

                        _buffer.Write(Pdu.ItemTypeTransferSyntax); // Item-Type
                        _buffer.Reserved(1);
                        _buffer.WriteAscii(transferSyntax.Value); // Abstract-syntax-name
                    }
                }

                needsScpScuRoleNegotiation |= (presentationContext.SupportsScuRole != null || presentationContext.SupportsScpRole != null);
                presentationContextId += 2;
            }

            // User Information Item

            _buffer.Write(Pdu.ItemTypeUserInformation); // Item-Type
            _buffer.Reserved(1);
            using (_buffer.BeginLengthPrefix())
            {
                // Maximum Length

                _buffer.Write(Pdu.ItemTypeMaximumLength); // Item-type
                _buffer.Reserved(1);
                _buffer.Write((ushort)0x0004); // Item-length
                _buffer.Write(association.MaxResponseDataLength); // Maximum-length-received

                // Implementation Class UID

                _buffer.Write(Pdu.ItemTypeImplementationClassUid); // Item-type
                _buffer.Reserved(1);
                _buffer.WriteAscii(Implementation.ClassUid.Value); // Implementation-class-uid

                // Asynchronous Operations Window

                _buffer.Write(Pdu.ItemTypeAsynchronousOperations); // Item-type
                _buffer.Reserved(1);
                _buffer.Write((ushort)0x0004); // Item-length
                _buffer.Write((ushort)0); // Maximum-number-operations-invoked
                _buffer.Write((ushort)0); // Maximum-number-operations-performed

                if (needsScpScuRoleNegotiation)
                {
                    foreach (PresentationContext presentationContext1 in association.PresentationContexts)
                    {
                        _buffer.Write(Pdu.ItemTypeScpScuRoleSelection); // Item-type
                        _buffer.Reserved(1);
                        using (_buffer.BeginLengthPrefix())
                        {
                            _buffer.WriteAscii(presentationContext1.AbstractSyntax.Value); // SOP-class-uid
                            _buffer.Write((byte)((presentationContext1.SupportsScuRole ?? false) ? 0 : 1)); // SCU-role
                            _buffer.Write((byte)((presentationContext1.SupportsScpRole ?? false) ? 0 : 1)); // SCP-role
                        }
                    }
                }

                // Implementation Version Name

                _buffer.Write(Pdu.ItemTypeImplementationVersionName); // Item-type
                _buffer.Reserved(1);
                _buffer.WriteAscii(Implementation.VersionName); // Implementation-version-name
            }
        }

        public void WriteAAbort(Pdu.AbortSource source, Pdu.AbortReason reason)
        {
            _buffer.Reserved(2);
            // Source
            _buffer.Write((byte)source);
            // Reason/Diag
            _buffer.Write((byte)reason);
        }

    }
}
