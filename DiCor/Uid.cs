using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace DiCor
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

        private Uid(string value)
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
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (value.Length == 0 || value.Length > 64)
            {
                return false;
            }

            foreach (char c in value)
            {
                if (c != '.' && (c < '0' || c > '9'))
                {
                    return false;
                }
            }

            return true;
        }

        public static StorageCategory GetStorageCategory(Uid uid)
        {
            if (!uid.IsDicomDefined && uid.Type == UidType.SOPClass)
                return StorageCategory.Private;

            if (uid.Type != UidType.SOPClass || !uid.Name.Contains("Storage"))
                return StorageCategory.None;

            if (uid.Name.Contains("Image Storage"))
                return StorageCategory.Image;

            if (uid.Name.Contains("Volume Storage"))
                return StorageCategory.Volume;

            if (uid == BlendingSoftcopyPresentationStateStorage
                || uid == ColorSoftcopyPresentationStateStorage
                || uid == GrayscaleSoftcopyPresentationStateStorage
                || uid == PseudoColorSoftcopyPresentationStateStorage)
                return StorageCategory.PresentationState;

            else if (uid == AudioSRStorageTrial_RETIRED
                || uid == BasicTextSRStorage
                || uid == ChestCADSRStorage
                || uid == ComprehensiveSRStorage
                || uid == ComprehensiveSRStorageTrial_RETIRED
                || uid == DetailSRStorageTrial_RETIRED
                || uid == EnhancedSRStorage
                || uid == MammographyCADSRStorage
                || uid == TextSRStorageTrial_RETIRED
                || uid == XRayRadiationDoseSRStorage)
                return StorageCategory.StructuredReport;

            else if (uid == AmbulatoryECGWaveformStorage
                || uid == BasicVoiceAudioWaveformStorage
                || uid == CardiacElectrophysiologyWaveformStorage
                || uid == GeneralECGWaveformStorage
                || uid == HemodynamicWaveformStorage
                || uid == _12LeadECGWaveformStorage
                || uid == WaveformStorageTrial_RETIRED)
                return StorageCategory.Waveform;

            else if (uid == EncapsulatedCDAStorage
                || uid == EncapsulatedPDFStorage)
                return StorageCategory.Document;

            else if (uid == RawDataStorage)
                return StorageCategory.Raw;

            return StorageCategory.Other;
        }

        public static Uid Get(string value)
            => s_uids.TryGetValue(new Uid(value), out Uid uid) ? uid : new Uid(value, string.Empty, UidType.Other);

        public static Uid NewUid(string name = "", UidType type = UidType.SOPInstance)
        {
            unsafe
            {
                Span<byte> span = stackalloc byte[sizeof(Guid)];
                Guid.NewGuid().TryWriteBytes(span);
                Swap(ref span[7], ref span[6]);
                Swap(ref span[5], ref span[4]);
                Swap(ref span[3], ref span[0]);
                Swap(ref span[1], ref span[2]);

                return new Uid("2.25." + new BigInteger(span, isUnsigned: true, isBigEndian: true).ToString(), name, type);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Swap(ref byte b1, ref byte b2)
        {
            byte temp = b2;
            b2 = b1;
            b1 = temp;
        }

        public bool IsDicomDefined
            => Value.StartsWith(DicomOrgRoot);

        public override string ToString()
            => $"{(IsRetired ? "*" : "")}{Type}: {Name} [{Value}]";

        public override int GetHashCode()
            => Value?.GetHashCode() ?? 0;

        public override bool Equals(object? obj)
            => obj is Uid uid && Equals(uid);

        public bool Equals([AllowNull] Uid other)
            => Value == other.Value;

        public static bool operator ==(Uid left, Uid right)
            => left.Equals(right);

        public static bool operator !=(Uid left, Uid right)
            => !(left == right);

    }
}
