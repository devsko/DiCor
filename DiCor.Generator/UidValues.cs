using System;

namespace DiCor.Generator
{
    internal enum UidType
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

    internal enum StorageCategory
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

    internal readonly struct UidValues
    {
        public readonly string Value;
        public readonly string Name;
        public readonly string Keyword;
        public readonly UidType Type;
        public readonly bool IsRetired;
        public readonly string Symbol;

        public StorageCategory StorageCategory
            => GetStorageCategory();

        public UidValues(string value, string name, string keyword, string type)
        {
            Value = value;
            Name = name;
            Keyword = keyword;
            Type = GetUidType(type);
            IsRetired = Name.IndexOf("(Retired)", StringComparison.OrdinalIgnoreCase) >= 0;
            Symbol = CreateSymbol();
        }

        private string CreateSymbol(bool useValue = false)
        {
            if (!string.IsNullOrEmpty(Keyword))
            {
                return Keyword.Contains("(Retired)") ? Keyword + "_RETIRED" : Keyword;
            }

            ReadOnlySpan<char> retired = "(Retired)".AsSpan();
            ReadOnlySpan<char> process = "(Process ".AsSpan();

            string text = useValue ? Value : Name;
            Span<char> buffer = stackalloc char[text.Length + 8 + 1]; // Additional 9 chars for appending "_RETIRED" and prepending "_" if needed
            Span<char> symbol = buffer.Slice(1);
            text.AsSpan().CopyTo(symbol);

            int pos = 0;
            bool toUpper = true;
            ReadOnlySpan<char> remainder = symbol;
            while (remainder.Length > 0)
            {
                char ch = remainder[0];
                if (ch == ':')
                {
                    break;
                }

                if (ch == '(' && (remainder.StartsWith(retired) || remainder.StartsWith(process)))
                {
                    remainder = remainder.Slice(9);
                }
                else
                {
                    if (char.IsLetterOrDigit(ch) || ch == '_')
                    {
                        symbol[pos++] = toUpper ? char.ToUpperInvariant(ch) : ch;
                        toUpper = false;
                    }
                    else if (ch is ' ' or '-')
                    {
                        toUpper = true;
                    }
                    else if (ch is '&' or '.')
                    {
                        symbol[pos++] = '_';
                        toUpper = true;
                    }
                    remainder = remainder.Slice(1);
                }
            }
            if (pos == 0)
            {
                return CreateSymbol(useValue: true);
            }
            if (char.IsDigit(symbol[0]))
            {
                symbol = buffer;
                symbol[0] = '_';
                pos++;
            }
            if (IsRetired)
            {
                "_RETIRED".AsSpan().CopyTo(symbol.Slice(pos));
                pos += 8;
            }

            return symbol.Slice(0, pos).ToString();
        }

        private static UidType GetUidType(string type)
        {
            if (type.Equals("synchronization frame of reference", StringComparison.OrdinalIgnoreCase))
                return UidType.Synchronization;

            if (type.Equals("ldap oid", StringComparison.OrdinalIgnoreCase))
                return UidType.LDAP;

            return Enum.TryParse(type.Replace(" ", null), out UidType result)
                ? result
                : UidType.Other;
        }

        private StorageCategory GetStorageCategory()
        {
            if (Type != UidType.SOPClass)
                return StorageCategory.None;

            if (!Value.StartsWith("1.2.840.10008.", StringComparison.Ordinal))
                return StorageCategory.Private;

            if (!Name.Contains("Storage") || Name.StartsWith("Storage Commitment", StringComparison.Ordinal))
                return StorageCategory.None;

            if (Name.Contains("Image Storage"))
                return StorageCategory.Image;

            if (Name.Contains("Presentation State Storage"))
                return StorageCategory.PresentationState;

            if (Name.Contains("SR Storage") ||
                Value == "1.2.840.10008.5.1.4.1.1.88.40" || // Procedure Log Storage
                Value == "1.2.840.10008.5.1.4.1.1.88.59")   // Key Object Selection Document Storage
                return StorageCategory.SRDocument;

            if (Name.Contains("Volume Storage"))
                return StorageCategory.Volume;

            if (Name.Contains("Waveform Storage"))
                return StorageCategory.Waveform;

            if (Name.StartsWith("Encapsulated ", StringComparison.Ordinal))
                return StorageCategory.EncapsulatedDocument;

            if (Name.Contains("Spectroscopy Storage"))
                return StorageCategory.Spectroscopy;

            if (Name.StartsWith("Raw Data ", StringComparison.Ordinal))
                return StorageCategory.Raw;

            return StorageCategory.Other;
        }
    }
}
