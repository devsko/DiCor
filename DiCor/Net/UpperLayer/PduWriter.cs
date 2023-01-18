using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using DiCor.Buffers;

namespace DiCor.Net.UpperLayer
{
    public ref struct PduWriter
    {
        private BufferWriter _buffer;

        public PduWriter(IBufferWriter<byte> output)
        {
            _buffer = new BufferWriter(output);
        }

        [UnscopedRef]
        private BufferWriter.LengthPrefix BeginPdu(Pdu.Type type)
        {
            _buffer.Write((byte)type);
            _buffer.Reserved(1);

            return _buffer.BeginLengthPrefix(sizeof(uint));
        }

        public void WriteAAssociateRq(Association association)
        {
            // PS3.8 - 9.3.2 A-ASSOCIATE-RQ PDU

            using (BeginPdu(Pdu.Type.AAssociateRq))
            {
                _buffer.Write((ushort)0x0001); // Protocol-version
                _buffer.Reserved(2);
                _buffer.WriteAsciiFixed(association.CalledAE, 16); // Called-AE-title
                _buffer.WriteAsciiFixed(association.CallingAE, 16); // Calling-AE-title
                _buffer.Reserved(32);

                // PS3.8 - 9.3.2.1 Application Context Item

                _buffer.Write(Pdu.ItemTypeApplicationContext); // Item-type
                _buffer.Reserved(1);
                _buffer.Write(association.ApplicationContext); // Application-context-name

                bool needsRoleNegotiation = false;
                byte presentationContextId = 1;
                foreach (PresentationContext presentationContext in association.PresentationContexts)
                {
                    // PS3.8 - 9.3.2.2 Presentation Context Item

                    _buffer.Write(Pdu.ItemTypePresentationContextRq); // Item-type
                    _buffer.Reserved(1);
                    using (_buffer.BeginLengthPrefix())
                    {
                        _buffer.Write(presentationContext.Id = presentationContextId); // Presentation-context-ID
                        _buffer.Reserved(3);

                        // PS3.8 - 9.3.2.2.1 Abstract Syntax Sub-Item

                        _buffer.Write(Pdu.ItemTypeAbstractSyntax); // Item-Type
                        _buffer.Reserved(1);
                        _buffer.Write(presentationContext.AbstractSyntax); // Abstract-syntax-name

                        foreach (Uid transferSyntax in presentationContext.TransferSyntaxes)
                        {
                            // PS3.8 - 9.3.2.2.2 Transfer Syntax Sub-Item

                            _buffer.Write(Pdu.ItemTypeTransferSyntax); // Item-Type
                            _buffer.Reserved(1);
                            _buffer.Write(transferSyntax); // Abstract-syntax-name
                        }
                    }

                    needsRoleNegotiation |= (presentationContext.SupportsScuRole != null || presentationContext.SupportsScpRole != null);
                    presentationContextId += 2;
                }

                // PS3.8 - 9.3.2.3 User Information Item

                _buffer.Write(Pdu.ItemTypeUserInformation); // Item-Type
                _buffer.Reserved(1);
                using (_buffer.BeginLengthPrefix())
                {
                    // PS3.8 - D.1.1 Maximum Length Sub-Item

                    _buffer.Write(Pdu.SubItemTypeMaximumLength); // Item-type
                    _buffer.Reserved(1);
                    _buffer.Write((ushort)0x0004); // Item-length
                    _buffer.Write(association.MaxResponseDataLength); // Maximum-length-received

                    // PS3.7 - D.3.3.2.1 Implementation Class UID

                    _buffer.Write(Pdu.SubItemTypeImplementationClassUid); // Item-type
                    _buffer.Reserved(1);
                    _buffer.Write(Uid.Implementation.ClassUid); // Implementation-class-uid

                    // PS3.7 - D.3.3.3.1 Asynchronous Operations Window

                    if (association.MaxOperationsInvoked != 1 && association.MaxOperationsPerformed != 1)
                    {
                        _buffer.Write(Pdu.SubItemTypeAsynchronousOperations); // Item-type
                        _buffer.Reserved(1);
                        _buffer.Write((ushort)0x0004); // Item-length
                        _buffer.Write(association.MaxOperationsInvoked); // Maximum-number-operations-invoked
                        _buffer.Write(association.MaxOperationsPerformed); // Maximum-number-operations-performed
                    }

                    // PS3.7 - D.3.3.4.1 SCP/SCU Role Selection

                    if (needsRoleNegotiation)
                    {
                        foreach (PresentationContext presentationContext1 in association.PresentationContexts)
                        {
                            if (presentationContext1.SupportsScuRole != null || presentationContext1.SupportsScpRole != null)
                            {
                                _buffer.Write(Pdu.SubItemTypeScpScuRoleSelection); // Item-type
                                _buffer.Reserved(1);
                                using (_buffer.BeginLengthPrefix())
                                {
                                    _buffer.Write(presentationContext1.AbstractSyntax); // SOP-class-uid
                                    _buffer.Write((byte)((presentationContext1.SupportsScuRole ?? false) ? 0 : 1)); // SCU-role
                                    _buffer.Write((byte)((presentationContext1.SupportsScpRole ?? false) ? 0 : 1)); // SCP-role
                                }
                            }
                        }
                    }

                    // PS3.7 - D.3.3.2.3 Implementation Version Name

                    _buffer.Write(Pdu.SubItemTypeImplementationVersionName); // Item-type
                    _buffer.Reserved(1);
                    _buffer.WriteAscii(Uid.Implementation.VersionName); // Implementation-version-name

                    // TODO PS3.7 - D.3.3.5.1 SOP Class Extended Negotiation Sub-Item 0x56
                    // TODO PS3.7 - D.3.3.6.1 SOP Class Common Extended Negotiation Sub-Item 0x57
                    // TODO PS3.7 - D.3.3.7.1 User Identity Sub-Item 0x58
                }
            }
            _buffer.Commit();
        }

        public void WritePDataTf(PDataTfData data)
        {
            // PS3.8 - 9.3.5 P-DATA-TF PDU

            _buffer.Write((byte)Pdu.Type.PDataTf);
            _buffer.Reserved(1);

            _buffer.Write(data.Length);

            if (data.Pdvs is null)
            {
                WritePdv(data.SinglePdv);
            }
            else
            {
                foreach (Pdv pdv in data.Pdvs)
                {
                    WritePdv(pdv);
                }
            }
            _buffer.Commit();
        }

        private void WritePdv(Pdv pdv)
        {
            // PS3.8 - 9.3.5.1 Presentation Data Value Item

            _buffer.Write(pdv.Length);
            _buffer.Write(pdv.PresentationContextId);

            // PS 3.8 - E.2 Message Control Header Encoding
            _buffer.Write(pdv.MessageControlHeader);
            foreach (ReadOnlyMemory<byte> memory in pdv.Data)
            {
                _buffer.Write(memory.Span);
                _buffer.Commit();
            }
        }

        public void WriteAReleaseRq()
        {
            // PS3.8 - 9.3.6 A-RELEASE-RQ PDU

            using (BeginPdu(Pdu.Type.AReleaseRq))
            {
                _buffer.Reserved(4);
            }
            _buffer.Commit();
        }

        public void WriteAReleaseRp()
        {
            // PS3.8 - 9.3.7 A-RELEASE-RP PDU

            using (BeginPdu(Pdu.Type.AReleaseRp))
            {
                _buffer.Reserved(4);
            }
            _buffer.Commit();
        }

        public void WriteAAbort(AAbortData data)
        {
            // PS3.8 - 9.3.8 A-ABORT PDU

            using (BeginPdu(Pdu.Type.AAbort))
            {
                _buffer.Reserved(2);
                _buffer.Write((byte)data.Source);
                _buffer.Write((byte)data.Reason);
            }
            _buffer.Commit();
        }

    }
}
