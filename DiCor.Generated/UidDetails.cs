using System;

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
        SRDocument,
        Waveform,
        EncapsulatedDocument,
        Spectroscopy,
        Raw,
        Other,
        Private,
        Volume
    }

    public readonly partial struct UidDetails : IEquatable<UidDetails>
    {
        public Uid Uid { get; }
        public string Name { get; }
        public UidType Type { get; }
        public StorageCategory StorageCategory { get; }
        public bool IsRetired { get; }

        private UidDetails(Uid uid, string? name = null, UidType type = UidType.Other, StorageCategory storageCategory = StorageCategory.None, bool isRetired = false)
        {
            Uid = uid;
            Name = name ?? string.Empty;
            Type = type;
            StorageCategory = storageCategory;
            IsRetired = isRetired;
        }

        public static partial UidDetails Get(Uid uid);

        public override string ToString()
        {
            return $"{(IsRetired ? "*" : "")}{Type}: {Name} [{Uid}]";
        }

        public override int GetHashCode()
        {
            return Uid.GetHashCode();
        }

        public override bool Equals(object? obj)
        {
            return obj is UidDetails uid && Equals(uid);
        }

        public bool Equals(UidDetails other)
        {
            return this == other;
        }

        public static bool operator ==(UidDetails left, UidDetails right)
        {
            return left.Uid.Equals(right.Uid);
        }

        public static bool operator !=(UidDetails left, UidDetails right)
        {
            return !(left == right);
        }
    }
}
