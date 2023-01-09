using System;

namespace DiCor.Generator
{
    public enum UidType
    {
        TransferSyntax,
        SOPClass,
        MetaSOPClass,
        ServiceClass,
        SOPInstance,
        ApplicationContextName,
        ApplicationHostingModel,
        CodingScheme,
        FrameOfReference,
        Synchronization,
        LDAP,
        MappingResource,
        ContextGroupName,
        Other,
    }

    public enum StorageCategory
    {
        None,
        Image,
        PresentationState,
        StructuredReport,
        Waveform,
        Document,
        Raw,
        Other,
        Private,
        Volume
    }

    public readonly struct UidValues
    {
        public readonly string Value;
        public readonly string Name;
        public readonly string Keyword;
        public readonly UidType Type;
        public readonly StorageCategory StorageCategory;
        public readonly bool IsRetired;

        public UidValues(string value, string name, string keyword, string type)
        {
            Value = value;
            Name = name;
            Keyword = keyword;
            Type = GetUidType(type);
            StorageCategory = GetStorageCategory(Value, Name, Type);
            IsRetired = Name.IndexOf("(Retired)", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public string Symbol(bool useValue = false)
        {
            if (!string.IsNullOrEmpty(Keyword))
            {
                return Keyword;
            }

            ReadOnlySpan<char> retired = "(Retired)".AsSpan();
            ReadOnlySpan<char> process = "(Process ".AsSpan();

            // Additional 9 chars for appending "_RETIRED" and prepending "_" if needed
            Span<char> buffer = stackalloc char[(useValue ? Value : Name).Length + 8 + 1];

            Span<char> symbol = buffer.Slice(1);

            (useValue ? Value : Name).AsSpan().CopyTo(symbol);

            ReadOnlySpan<char> read = symbol;
            int writeAt = 0;
            bool upper = true;

            while (read.Length > 0)
            {
                char ch = read[0];
                if (ch == ':')
                {
                    break;
                }

                if (ch == '(' && (read.StartsWith(retired) || read.StartsWith(process)))
                {
                    read = read.Slice(9);
                }
                else
                {
                    if (char.IsLetterOrDigit(ch) || ch == '_')
                    {
                        symbol[writeAt++] = upper ? char.ToUpperInvariant(ch) : ch;
                        upper = false;
                    }
                    else if (ch == ' ' || ch == '-')
                    {
                        upper = true;
                    }
                    else if (ch == '&' || ch == '.')
                    {
                        symbol[writeAt++] = '_';
                        upper = true;
                    }
                    read = read.Slice(1);
                }
            }
            if (writeAt == 0)
            {
                return Symbol(useValue: true);
            }
            if (char.IsDigit(symbol[0]))
            {
                symbol = buffer;
                symbol[0] = '_';
                writeAt++;
            }
            if (IsRetired)
            {
                "_RETIRED".AsSpan().CopyTo(symbol.Slice(writeAt));
                writeAt += 8;
            }

            return symbol.Slice(0, writeAt).ToString();
        }

        private static UidType GetUidType(string type)
        {
            if (type.Equals("synchronization frame of reference", StringComparison.OrdinalIgnoreCase))
                return UidType.Synchronization;
            if (type.Equals("ldap oid", StringComparison.OrdinalIgnoreCase))
                return UidType.LDAP;

            return Enum.TryParse<UidType>(type.Replace(" ", null), out UidType result)
                ? result
                : UidType.Other;
        }

        private static StorageCategory GetStorageCategory(string value, string name, UidType type)
        {
            if (!value.StartsWith("1.2.840.10008.") && type == UidType.SOPClass)
                return StorageCategory.Private;

            if (type != UidType.SOPClass || !name.Contains("Storage"))
                return StorageCategory.None;

            if (name.Contains("Image Storage"))
                return StorageCategory.Image;

            if (name.Contains("Volume Storage"))
                return StorageCategory.Volume;

            if (value == "1.2.840.10008.5.1.4.1.1.11.4" // BlendingSoftcopyPresentationStateStorage
                || value == "1.2.840.10008.5.1.4.1.1.11.2" // ColorSoftcopyPresentationStateStorage
                || value == "1.2.840.10008.5.1.4.1.1.11.1" // GrayscaleSoftcopyPresentationStateStorage
                || value == "1.2.840.10008.5.1.4.1.1.11.3") // PseudoColorSoftcopyPresentationStateStorage
                return StorageCategory.PresentationState;

            else if (value == "1.2.840.10008.5.1.4.1.1.88.2" // AudioSRStorageTrial_RETIRED
                || value == "1.2.840.10008.5.1.4.1.1.88.11" // BasicTextSRStorage
                || value == "1.2.840.10008.5.1.4.1.1.88.65" // ChestCADSRStorage
                || value == "1.2.840.10008.5.1.4.1.1.88.59" // ComprehensiveSRStorage
                || value == "1.2.840.10008.5.1.4.1.1.88.4" // ComprehensiveSRStorageTrial_RETIRED
                || value == "1.2.840.10008.5.1.4.1.1.88.3" // DetailSRStorageTrial_RETIRED
                || value == "1.2.840.10008.5.1.4.1.1.88.22" // EnhancedSRStorage
                || value == "1.2.840.10008.5.1.4.1.1.88.50" // MammographyCADSRStorage
                || value == "1.2.840.10008.5.1.4.1.1.88.1" // TextSRStorageTrial_RETIRED
                || value == "1.2.840.10008.5.1.4.1.1.88.67") // XRayRadiationDoseSRStorage)
                return StorageCategory.StructuredReport;

            else if (value == "1.2.840.10008.5.1.4.1.1.9.1.3" // AmbulatoryECGWaveformStorage
                || value == "1.2.840.10008.5.1.4.1.1.9.4.1" // BasicVoiceAudioWaveformStorage
                || value == "1.2.840.10008.5.1.4.1.1.9.3.1" // CardiacElectrophysiologyWaveformStorage
                || value == "1.2.840.10008.5.1.4.1.1.9.1.2" // GeneralECGWaveformStorage
                || value == "1.2.840.10008.5.1.4.1.1.9.2.1" // HemodynamicWaveformStorage
                || value == "1.2.840.10008.5.1.4.1.1.9.1.1" // _12LeadECGWaveformStorage
                || value == "1.2.840.10008.5.1.4.1.1.9.1") // WaveformStorageTrial_RETIRED
                return StorageCategory.Waveform;

            else if (value == "1.2.840.10008.5.1.4.1.1.104.2" // EncapsulatedCDAStorage
                || value == "1.2.840.10008.5.1.4.1.1.104.1") // EncapsulatedPDFStorage
                return StorageCategory.Document;

            else if (value == "1.2.840.10008.5.1.4.1.1.66") // RawDataStorage
                return StorageCategory.Raw;

            return StorageCategory.Other;
        }
    }
}
