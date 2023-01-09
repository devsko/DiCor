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
        StructuredReport,
        Waveform,
        Document,
        Raw,
        Other,
        Private,
        Volume
    }

    public readonly partial struct UidDetails : IEquatable<UidDetails>
    {
        public static partial UidDetails Get(Uid uid);

        public Uid Uid { get; }
        public string Name { get; }
        public UidType Type { get; }
        public StorageCategory StorageCategory { get; }
        public bool IsRetired { get; }

        private UidDetails(Uid uid, string name, UidType type, StorageCategory storageCategory = StorageCategory.None, bool isRetired = false)
        {
            Uid = uid;
            Name = name;
            Type = type;
            StorageCategory = storageCategory;
            IsRetired = isRetired;
        }

        public override string ToString()
            => $"{(IsRetired ? "*" : "")}{Type}: {Name} [{Uid}]";

        public override int GetHashCode()
            => Uid.GetHashCode();

        public override bool Equals(object? obj)
            => obj is UidDetails uid && Equals(uid);

        public bool Equals(UidDetails other)
            => this == other;

        public static bool operator ==(UidDetails left, UidDetails right)
            => left.Uid.Equals(right.Uid);

        public static bool operator !=(UidDetails left, UidDetails right)
            => !(left == right);
    }
}
