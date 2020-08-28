﻿using System;
using System.Buffers;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace DiCor.Net.UpperLayer
{
    public ref struct PduReader
    {
        private SequenceReader<byte> _input;

        public PduReader(in SequenceReader<byte> input)
        {
            _input = input;
        }

        public void ReadAAssociateRq()
        {
            // TODO
        }

        public void ReadAAssociateAc(Association association)
        {
            association.MaxOperationsInvoked = 1;
            association.MaxOperationsPerformed = 1;

            _input.TryReadBigEndian(out ushort _); // Protocol-version
            _input.Reserved(2);
            _input.Reserved(16);
            _input.Reserved(16);
            _input.Reserved(32);

            while (_input.Remaining > 0)
            {
                _input.TryRead(out byte type); // Item-type
                _input.Reserved(1);
                _input.TryReadLength(out ushort length); // Item-length
                long end = _input.Remaining - length;

                switch (type)
                {
                    case Pdu.ItemTypeApplicationContext:
                        _input.TryRead(length, out string _); // Item-length, Application-context-name
                        break;

                    case Pdu.ItemTypePresentationContextAc:
                        _input.TryRead(out byte id);  // Presentation-context-ID
                        _input.Reserved(1);
                        _input.TryRead(out Pdu.PresentationContextItemResult result); // Result/Reason
                        _input.Reserved(1);

                        PresentationContext? presentationContext = association.GetPresentationContext(id);
                        if (presentationContext == null)
                            // TODO InvalidPduException
                            throw new InvalidOperationException();

                        presentationContext.Result = result;

                        if (result == Pdu.PresentationContextItemResult.Acceptance)
                        {
                            // Transfer-Syntax Sub-Item

                            _input.TryRead(out byte itemType); // Item-type
                            if (itemType != Pdu.ItemTypeTransferSyntax)
                                // TODO InvalidPduException
                                throw new InvalidOperationException();
                            _input.Reserved(1);
                            _input.TryRead(out string? transferSyntax); // Transfer-syntax-name

                            presentationContext.AcceptedTransferSyntax = Uid.Get(transferSyntax!);
                        }
                        else
                            _input.Reserved((int)(_input.Remaining - end));
                        break;

                    case Pdu.ItemTypeUserInformation:
                        var userInformation = new SequenceReader<byte>(_input.Sequence.Slice(_input.Position, length));
                        while (userInformation.Remaining > 0)
                        {
                            userInformation.TryRead(out byte itemType); // Item-type
                            userInformation.Reserved(1);
                            userInformation.TryReadLength(out ushort itemLength); // Item-length
                            long itemEnd = userInformation.Remaining - itemLength;

                            switch (itemType)
                            {
                                case Pdu.ItemTypeMaximumLength:
                                    userInformation.TryReadBigEndian(out uint maxLength); // Maximum-length-received
                                    association.MaxRequestDataLength = maxLength;
                                    break;

                                case Pdu.ItemTypeImplementationClassUid:
                                    userInformation.TryRead(itemLength, out string? implementationClass); // Implementation-class-uid
                                    association.RemoteImplementationClass = Uid.Get(implementationClass!);
                                    break;

                                case Pdu.ItemTypeAsynchronousOperations:
                                    userInformation.TryReadBigEndian(out ushort maxOperationsInvoked); // Maximum-number-operations-invoked
                                    userInformation.TryReadBigEndian(out ushort maxOperationsPerformed); // Maximum-number-operations-performed
                                    association.MaxOperationsInvoked = maxOperationsInvoked;
                                    association.MaxOperationsPerformed = maxOperationsPerformed;
                                    break;

                                case Pdu.ItemTypeScpScuRoleSelection:
                                    userInformation.TryRead(out string? syntax); // UID-length / SOP-class-uid
                                    userInformation.TryRead(out byte scuRole); // SCU-role
                                    userInformation.TryRead(out byte scpRole); // SCP-role
                                    PresentationContext? presentationContext1 = association.GetPresentationContext(Uid.Get(syntax!));
                                    if (presentationContext1 == null)
                                        // TODO InvalidPduException
                                        throw new InvalidOperationException();
                                    presentationContext1.SupportsScuRole = scuRole == 0x01;
                                    presentationContext1.SupportsScpRole = scpRole == 0x01;
                                    break;

                                case Pdu.ItemTypeImplementationVersionName:
                                    userInformation.TryRead(itemLength, out string? implementationVersion); // Implementation-version-name
                                    association.RemoteImplementationVersion = implementationVersion!;
                                    break;

                                    // TODO SOP Class Extended Negotiation Sub-Item 0x56
                                    // TODO User Identity Negotiation Sub-Item 0x59

                            }
                            if (userInformation.Remaining != itemEnd)
                                // TODO InvalidPduException
                                throw new InvalidOperationException();
                        }
                        _input.Advance(length);
                        break;
                }
                if (_input.Remaining != end)
                    // TODO InvalidPduException
                    throw new InvalidOperationException();
            }

        }

        public void ReadAAbort(ref ULMessage message)
        {
            _input.Reserved(2);
            _input.TryRead(out byte source); // Source
            _input.TryRead(out byte reason); // Reason/Diag

            message.B1 = source;
            message.B2 = reason;
        }

    }
}