using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

using DiCor.Internal;

using Microsoft.CodeAnalysis;

namespace DiCor.Generator
{
    public class Part06 : DicomXmlDoc
    {
        public const string ResourceKey = "xml.part06.xml";
        public const string Uri = "http://medical.nema.org/medical/dicom/current/source/docbook/part06/part06.xml";

        public Part06(HttpClient httpClient, string projectPath, CancellationToken cancellationToken)
            : base(httpClient, projectPath, Uri, ResourceKey, cancellationToken)
        { }

        public async Task<(Uid[]? TableA1, Uid[]? TableA3)> GetTablesAsync(SourceGeneratorContext context, Dictionary<int, string> cidTable)
        {
            await InitializeAsync(context).ConfigureAwait(false);

            Debug.Assert(Reader != null);

            int tablesFound = 0;
            Uid[]? tableA1 = null;
            Uid[]? tableA3 = null;
            while (await Reader!.ReadAsync().ConfigureAwait(false))
            {
                _cancellationToken.ThrowIfCancellationRequested();

                if (Reader.NodeType == XmlNodeType.Element && Reader.LocalName == "table")
                {
                    string? id = Reader.GetAttribute("id", XNamespace.Xml.NamespaceName);
                    if (id == "table_A-1")
                    {
                        tableA1 = ReadTableA1();
                    }
                    else if (id == "table_A-3")
                    {
                        tableA3 = ReadTableA3(cidTable);
                    }
                    else
                        continue;

                    if (++tablesFound >= 2)
                        break;
                }
            }

            return (tableA1, tableA3);
        }

        private Uid[]? ReadTableA1()
            => XElement.Load(Reader!.ReadSubtree())
                .Element(DocbookNS + "tbody")?
                .Elements(DocbookNS + "tr")
                .Select(tr => tr.Elements(DocbookNS + "td"))
                .Select(row => A1ToUid(row))
                .ToArray();

        private Uid[]? ReadTableA3(Dictionary<int, string> cidTable)
            => XElement.Load(Reader!.ReadSubtree())
                .Element(DocbookNS + "tbody")?
                .Elements(DocbookNS + "tr")
                .Select(tr => tr.Elements(DocbookNS + "td"))
                .Select(row => A3ToUid(row, cidTable))
                .ToArray();

        private static Uid A1ToUid(IEnumerable<XElement> row)
        {
            string uid = row.ElementAt(0).Value.Trim().Replace("\u200b", "");
            string name = row.ElementAt(1).Value.Trim();
            string type = row.ElementAt(2).Value.Trim();

            return new Uid(
                    uid,
                    name,
                    ToUidType(type),
                    isRetired: name.IndexOf("(Retired)", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static UidType ToUidType(string value)
        {
            if (value.Equals("synchronization frame of reference", StringComparison.OrdinalIgnoreCase))
                return UidType.Synchronization;
            if (value.IndexOf("frame of reference", StringComparison.OrdinalIgnoreCase) >= 0)
                return UidType.FrameOfReference;
            if (value.IndexOf("sop instance", StringComparison.OrdinalIgnoreCase) >= 0)
                return UidType.SOPInstance;
            if (value.IndexOf("coding scheme", StringComparison.OrdinalIgnoreCase) >= 0)
                return UidType.CodingScheme;
            if (value.Equals("ldap oid", StringComparison.OrdinalIgnoreCase))
                return UidType.LDAP;

            return Enum.TryParse<UidType>(value.Replace(" ", null), out UidType result)
                ? result
                : UidType.Other;
        }

        private static Uid A3ToUid(IEnumerable<XElement> row, Dictionary<int, string> cidTable)
        {
            string uid = row.ElementAt(0).Value.Trim().Replace("\u200b", "");
            string? cid = row
                .ElementAt(1)
                .Descendants(DocbookNS + "olink")?
                .FirstOrDefault()?
                .Attribute("targetptr")?
                .Value;

            if (cid is null)
                return new Uid(uid, string.Empty, UidType.ContextGroupName);

            cid = cid.Substring(9);
            string name = cidTable[int.Parse(cid)];

            return new Uid(
                uid,
                $"{name} ({cid})",
                UidType.ContextGroupName,
                isRetired: name.IndexOf("(Retired)", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static string ToSymbol(Uid uid, bool useValue = false)
        {
            // Additional 9 chars for appending "_RETIRED" and prepending "_" if needed
            Span<char> symbol = stackalloc char[(useValue ? uid.Value : uid.Name).Length + 8 + 1];
            (useValue ? uid.Value : uid.Name).AsSpan().CopyTo(symbol);

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

                if (ch == '(' && (read.StartsWith("(Retired)".AsSpan()) || read.StartsWith("(Process ".AsSpan())))
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
                return ToSymbol(uid, true);
            }
            if (char.IsDigit(symbol[0]))
            {
                symbol.Slice(0, writeAt++).CopyTo(symbol.Slice(1));
                symbol[0] = '_';
            }
            if (uid.IsRetired)
            {
                "_RETIRED".AsSpan().CopyTo(symbol.Slice(writeAt));
                writeAt += 8;
            }

            return symbol.Slice(0, writeAt).ToString();
        }
    }
}
