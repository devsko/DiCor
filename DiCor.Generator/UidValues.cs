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
        public readonly StorageCategory StorageCategory;
        public readonly bool IsRetired;
        public readonly string Symbol;

        public UidValues(string value, string name, string keyword, string type)
        {
            Value = value;
            Name = name;
            Keyword = keyword;
            Type = GetUidType(type);
            StorageCategory = GetStorageCategory(Value, Name, Type);
            IsRetired = Name.IndexOf("(Retired)", StringComparison.OrdinalIgnoreCase) >= 0;
            Symbol = CreateSymbol();
        }

        private string CreateSymbol(bool useValue = false)
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
                    else if (ch is ' ' or '-')
                    {
                        upper = true;
                    }
                    else if (ch is '&' or '.')
                    {
                        symbol[writeAt++] = '_';
                        upper = true;
                    }
                    read = read.Slice(1);
                }
            }
            if (writeAt == 0)
            {
                return CreateSymbol(useValue: true);
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

            return Enum.TryParse(type.Replace(" ", null), out UidType result)
                ? result
                : UidType.Other;
        }

        private static StorageCategory GetStorageCategory(string value, string name, UidType type)
        {
            if (type != UidType.SOPClass)
                return StorageCategory.None;

            if (!value.StartsWith("1.2.840.10008.", StringComparison.Ordinal))
                return StorageCategory.Private;

            if (!name.Contains("Storage") || name.StartsWith("Storage Commitment", StringComparison.Ordinal))
                return StorageCategory.None;

            if (name.Contains("Image Storage"))
                return StorageCategory.Image;

            if (name.Contains("Presentation State Storage"))
                return StorageCategory.PresentationState;

            if (name.Contains("SR Storage") ||
                value == "1.2.840.10008.5.1.4.1.1.88.40" || // Procedure Log Storage
                value == "1.2.840.10008.5.1.4.1.1.88.59")   // Key Object Selection Document Storage
                return StorageCategory.SRDocument;

            if (name.Contains("Volume Storage"))
                return StorageCategory.Volume;

            if (name.Contains("Waveform Storage"))
                return StorageCategory.Waveform;

            if (name.StartsWith("Encapsulated ", StringComparison.Ordinal))
                return StorageCategory.EncapsulatedDocument;

            if (name.Contains("Spectroscopy Storage"))
                return StorageCategory.Spectroscopy;

            if (name.StartsWith("Raw Data ", StringComparison.Ordinal))
                return StorageCategory.Raw;

            return StorageCategory.Other;
        }
    }
}
