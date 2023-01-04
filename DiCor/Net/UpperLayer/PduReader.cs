using System.Buffers;

namespace DiCor.Net.UpperLayer
{
    public ref struct PduReader
    {
        private SequenceReader<byte> _input;

        public PduReader(in SequenceReader<byte> input)
        {
            _input = input;
        }

        public void ReadAAssociateRq(ref Association association)
        {
            association = new Association();

            _input.TryReadBigEndian(out ushort _); // Protocol-version
            _input.Reserved(2);
            _input.TryReadAscii(16, out string? calledAE);
            association.CalledAE = calledAE;
            _input.TryReadAscii(16, out string? callingAE);
            association.CallingAE = callingAE;
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
                        if (_input.TryReadAscii(length, out string? applicationContext)) // Item-length, Application-context-name
                            association.ApplicationContext = Uid.Get(applicationContext);
                        break;

                    case Pdu.ItemTypePresentationContextRq:
                        {
                            _input.Reserved(1);
                            _input.TryReadLength(out _); // itemLength

                            _input.TryRead(out _); // presentationContextid
                            _input.Reserved(3);



                            //if (result == Pdu.PresentationContextItemResult.Acceptance)
                            //{
                            //    // Transfer-Syntax Sub-Item

                            //    _input.TryRead(out byte itemType); // Item-type
                            //    if (itemType != Pdu.ItemTypeTransferSyntax)
                            //        // TODO InvalidPduException
                            //        throw new InvalidOperationException();
                            //    _input.Reserved(1);
                            //    _input.TryReadAscii(out string? transferSyntax); // Transfer-syntax-name

                            //    presentationContext.AcceptedTransferSyntax = Uid.Get(transferSyntax!);
                            //}
                            //else
                            _input.Reserved((int)(_input.Remaining - end));
                        }
                        break;

                    case Pdu.ItemTypeUserInformation:
                        {
                            var userInformation = new SequenceReader<byte>(_input.Sequence.Slice(_input.Position, length));
                            while (userInformation.Remaining > 0)
                            {
                                userInformation.TryRead(out byte itemType); // Item-type
                                userInformation.Reserved(1);
                                userInformation.TryReadLength(out ushort itemLength); // Item-length
                                long itemEnd = userInformation.Remaining - itemLength;

                                switch (itemType)
                                {
                                    case Pdu.SubItemTypeMaximumLength:
                                        userInformation.TryReadBigEndian(out uint maxLength); // Maximum-length-received
                                        association.MaxRequestDataLength = maxLength;
                                        break;

                                    case Pdu.SubItemTypeImplementationClassUid:
                                        userInformation.TryReadAscii(itemLength, out string? implementationClass); // Implementation-class-uid
                                        association.RemoteImplementationClass = Uid.Get(implementationClass!);
                                        break;

                                    case Pdu.SubItemTypeAsynchronousOperations:
                                        userInformation.TryReadBigEndian(out ushort maxOperationsInvoked); // Maximum-number-operations-invoked
                                        userInformation.TryReadBigEndian(out ushort maxOperationsPerformed); // Maximum-number-operations-performed
                                        association.MaxOperationsInvoked = maxOperationsInvoked;
                                        association.MaxOperationsPerformed = maxOperationsPerformed;
                                        break;

                                    case Pdu.SubItemTypeScpScuRoleSelection:
                                        userInformation.TryReadAscii(out string? syntax); // UID-length / SOP-class-uid
                                        userInformation.TryRead(out byte scuRole); // SCU-role
                                        userInformation.TryRead(out byte scpRole); // SCP-role
                                        // TODO InvalidPduException
                                        PresentationContext? presentationContext1 = association.GetPresentationContext(Uid.Get(syntax!)) ?? throw new InvalidOperationException();
                                        presentationContext1.SupportsScuRole = scuRole == 0x01;
                                        presentationContext1.SupportsScpRole = scpRole == 0x01;
                                        break;

                                    case Pdu.SubItemTypeImplementationVersionName:
                                        userInformation.TryReadAscii(itemLength, out string? implementationVersion); // Implementation-version-name
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
                        }
                        break;
                }
                if (_input.Remaining != end)
                    // TODO InvalidPduException
                    throw new InvalidOperationException();
            }
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
                        _input.TryReadAscii(length, out string _); // Item-length, Application-context-name
                        break;

                    case Pdu.ItemTypePresentationContextAc:
                        _input.TryRead(out byte id);  // Presentation-context-ID
                        _input.Reserved(1);
                        _input.TryReadEnumFromByte(out Pdu.PresentationContextItemResult result); // Result/Reason
                        _input.Reserved(1);

                        // TODO InvalidPduException
                        PresentationContext? presentationContext = association.GetPresentationContext(id) ?? throw new InvalidOperationException();
                        presentationContext.Result = result;

                        if (result == Pdu.PresentationContextItemResult.Acceptance)
                        {
                            // Transfer-Syntax Sub-Item

                            _input.TryRead(out byte itemType); // Item-type
                            if (itemType != Pdu.ItemTypeTransferSyntax)
                                // TODO InvalidPduException
                                throw new InvalidOperationException();
                            _input.Reserved(1);
                            _input.TryReadAscii(out string? transferSyntax); // Transfer-syntax-name

                            presentationContext.AcceptedTransferSyntax = Uid.Get(transferSyntax!);
                        }
                        else
                        {
                            _input.Reserved((int)(_input.Remaining - end));
                        }
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
                                case Pdu.SubItemTypeMaximumLength:
                                    userInformation.TryReadBigEndian(out uint maxLength); // Maximum-length-received
                                    association.MaxRequestDataLength = maxLength;
                                    break;

                                case Pdu.SubItemTypeImplementationClassUid:
                                    userInformation.TryReadAscii(itemLength, out string? implementationClass); // Implementation-class-uid
                                    association.RemoteImplementationClass = Uid.Get(implementationClass!);
                                    break;

                                case Pdu.SubItemTypeAsynchronousOperations:
                                    userInformation.TryReadBigEndian(out ushort maxOperationsInvoked); // Maximum-number-operations-invoked
                                    userInformation.TryReadBigEndian(out ushort maxOperationsPerformed); // Maximum-number-operations-performed
                                    association.MaxOperationsInvoked = maxOperationsInvoked;
                                    association.MaxOperationsPerformed = maxOperationsPerformed;
                                    break;

                                case Pdu.SubItemTypeScpScuRoleSelection:
                                    userInformation.TryReadAscii(out string? syntax); // UID-length / SOP-class-uid
                                    userInformation.TryRead(out byte scuRole); // SCU-role
                                    userInformation.TryRead(out byte scpRole); // SCP-role
                                    // TODO InvalidPduException
                                    PresentationContext? presentationContext1 = association.GetPresentationContext(Uid.Get(syntax!)) ?? throw new InvalidOperationException();
                                    presentationContext1.SupportsScuRole = scuRole == 0x01;
                                    presentationContext1.SupportsScpRole = scpRole == 0x01;
                                    break;

                                case Pdu.SubItemTypeImplementationVersionName:
                                    userInformation.TryReadAscii(itemLength, out string? implementationVersion); // Implementation-version-name
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

        public void ReadAAssociateRj(AAssociateRjData data)
        {
            _input.Reserved(1);
            _input.TryReadEnumFromByte(out data.Result);
            _input.TryReadEnumFromByte(out data.Source);
            _input.TryReadEnumFromByte(out data.Reason);
        }

        public void ReadPDataTf(PDataTfData data)
        {
            List<Pdv>? pdvList = null;
            Pdv singlePdv = default;
            bool first = true;

            while (_input.Remaining > 0)
            {
                if (first)
                {
                    singlePdv = ReadPdv();
                }
                else
                {
                    if (pdvList is null)
                    {
                        pdvList = new List<Pdv>() { singlePdv };
                        singlePdv = default;
                    }
                    pdvList.Add(ReadPdv());
                }
                first = false;
            }

            data.SinglePdv = singlePdv;
            if (pdvList is not null)
            {
                data.Pdvs = pdvList.ToArray();
            }
        }

        private Pdv ReadPdv()
        {
            Pdv pdv;
            _input.TryReadBigEndian(out int length);
            _input.TryRead(out pdv.PresentationContextId);
            _input.TryRead(out pdv.MessageControlHeader);
            length -= 2;
            pdv.Data = _input.UnreadSequence.Slice(0, length);
            _input.Advance(length);

            return pdv;
        }

        public void ReadAReleaseRq()
        {
            _input.Reserved(4);
        }

        public void ReadAReleaseRp()
        {
            _input.Reserved(4);
        }

        public void ReadAAbort(AAbortData data)
        {
            _input.Reserved(2);
            _input.TryReadEnumFromByte(out data.Source);
            _input.TryReadEnumFromByte(out data.Reason);
        }

    }
}
