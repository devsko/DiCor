using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace DiCor
#if GENERATOR
.Internal
#endif
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

    public readonly partial struct Uid : IEquatable<Uid>
    {
        public const string DicomOrgRoot = "1.2.840.10008";
        public const string ThisOrgRoot = "1.2.826.0.1.3680043.10.386";

        public string Value { get; }
        public string Name { get; }
        public UidType Type { get; }
        public bool IsRetired { get; }

        public StorageCategory StorageCategory { get; }

        public Uid(string value, string name, UidType type, StorageCategory storageCategory = StorageCategory.None, bool isRetired = false)
        {
            if (!IsValid(value))
                throw new ArgumentException($"{value} is not a valid uid.");

            Value = value;
            Name = name;
            Type = type;
            StorageCategory = storageCategory;
            IsRetired = isRetired;
        }

#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable IDE0051 // Remove unused private members
        private Uid(string value)
#pragma warning restore IDE0051 // Remove unused private members
#pragma warning restore IDE0079 // Remove unnecessary suppression
        {
            Value = value;
            Name = string.Empty;
            Type = default;
            StorageCategory = default;
            IsRetired = default;
        }

        public static bool IsValid(string value)
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));

            if (value.Length == 0 || value.Length > 64)
                return false;

            foreach (char c in value)
            {
                if (c != '.' && (c < '0' || c > '9'))
                    return false;
            }

            return true;
        }

        public static StorageCategory GetStorageCategory(in Uid uid)
        {
            if (!uid.IsDicomDefined && uid.Type == UidType.SOPClass)
                return StorageCategory.Private;

            if (uid.Type != UidType.SOPClass || !uid.Name.Contains("Storage"))
                return StorageCategory.None;

            if (uid.Name.Contains("Image Storage"))
                return StorageCategory.Image;

            if (uid.Name.Contains("Volume Storage"))
                return StorageCategory.Volume;

            if (uid.Value == "1.2.840.10008.5.1.4.1.1.11.4" // BlendingSoftcopyPresentationStateStorage
                || uid.Value == "1.2.840.10008.5.1.4.1.1.11.2" // ColorSoftcopyPresentationStateStorage
                || uid.Value == "1.2.840.10008.5.1.4.1.1.11.1" // GrayscaleSoftcopyPresentationStateStorage
                || uid.Value == "1.2.840.10008.5.1.4.1.1.11.3") // PseudoColorSoftcopyPresentationStateStorage
                return StorageCategory.PresentationState;

            else if (uid.Value == "1.2.840.10008.5.1.4.1.1.88.2" // AudioSRStorageTrial_RETIRED
                || uid.Value == "1.2.840.10008.5.1.4.1.1.88.11" // BasicTextSRStorage
                || uid.Value == "1.2.840.10008.5.1.4.1.1.88.65" // ChestCADSRStorage
                || uid.Value == "1.2.840.10008.5.1.4.1.1.88.59" // ComprehensiveSRStorage
                || uid.Value == "1.2.840.10008.5.1.4.1.1.88.4" // ComprehensiveSRStorageTrial_RETIRED
                || uid.Value == "1.2.840.10008.5.1.4.1.1.88.3" // DetailSRStorageTrial_RETIRED
                || uid.Value == "1.2.840.10008.5.1.4.1.1.88.22" // EnhancedSRStorage
                || uid.Value == "1.2.840.10008.5.1.4.1.1.88.50" // MammographyCADSRStorage
                || uid.Value == "1.2.840.10008.5.1.4.1.1.88.1" // TextSRStorageTrial_RETIRED
                || uid.Value == "1.2.840.10008.5.1.4.1.1.88.67") // XRayRadiationDoseSRStorage)
                return StorageCategory.StructuredReport;

            else if (uid.Value == "1.2.840.10008.5.1.4.1.1.9.1.3" // AmbulatoryECGWaveformStorage
                || uid.Value == "1.2.840.10008.5.1.4.1.1.9.4.1" // BasicVoiceAudioWaveformStorage
                || uid.Value == "1.2.840.10008.5.1.4.1.1.9.3.1" // CardiacElectrophysiologyWaveformStorage
                || uid.Value == "1.2.840.10008.5.1.4.1.1.9.1.2" // GeneralECGWaveformStorage
                || uid.Value == "1.2.840.10008.5.1.4.1.1.9.2.1" // HemodynamicWaveformStorage
                || uid.Value == "1.2.840.10008.5.1.4.1.1.9.1.1" // _12LeadECGWaveformStorage
                || uid.Value == "1.2.840.10008.5.1.4.1.1.9.1") // WaveformStorageTrial_RETIRED
                return StorageCategory.Waveform;

            else if (uid.Value == "1.2.840.10008.5.1.4.1.1.104.2" // EncapsulatedCDAStorage
                || uid.Value == "1.2.840.10008.5.1.4.1.1.104.1") // EncapsulatedPDFStorage
                return StorageCategory.Document;

            else if (uid.Value == "1.2.840.10008.5.1.4.1.1.66") // RawDataStorage
                return StorageCategory.Raw;

            return StorageCategory.Other;
        }

        public bool IsDicomDefined
            => Value.StartsWith(DicomOrgRoot);

        public override string ToString()
            => $"{(IsRetired ? "*" : "")}{Type}: {Name} [{Value}]";

        public override int GetHashCode()
            => Value.GetHashCode();

        public override bool Equals(object? obj)
            => obj is Uid uid && Equals(uid);

        public bool Equals(Uid other)
            => Value == other.Value;

        public static bool operator ==(Uid left, Uid right)
            => left.Equals(right);

        public static bool operator !=(Uid left, Uid right)
            => !(left == right);
    }
}
