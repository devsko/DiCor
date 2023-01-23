namespace DiCor
{
    public partial struct Uid
    {
        public bool IsKnown
            => GetDetails() is not null;

        public partial Details? GetDetails();

        public sealed class Details
        {
            public string? Name { get; }
            public UidType Type { get; }
            public StorageCategory StorageCategory { get; }
            public bool IsRetired { get; }

            internal Details(string? name, UidType type, StorageCategory storageCategory = StorageCategory.None, bool isRetired = false)
            {
                Name = name;
                Type = type;
                StorageCategory = storageCategory;
                IsRetired = isRetired;
            }
        }
    }
}
