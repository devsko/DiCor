namespace DiCor
{
    partial struct Uid
    {
        public bool IsKnown => GetDetails() is not null;

        public partial Details? GetDetails();

        public readonly struct Details
        {
            public string? Name { get; }
            public UidType Type { get; }
            public StorageCategory StorageCategory { get; }
            public bool IsRetired { get; }

            internal Details(string? name = null, UidType type = UidType.Other, StorageCategory storageCategory = StorageCategory.None, bool isRetired = false)
            {
                Name = name;
                Type = type;
                StorageCategory = storageCategory;
                IsRetired = isRetired;
            }
        }
    }
}
