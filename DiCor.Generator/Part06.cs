using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

using Microsoft.CodeAnalysis;

namespace DiCor.Generator
{
    internal sealed class Part06 : DocBook
    {
        public Part06(HttpClient httpClient, SourceProductionContext context, Settings settings)
            : base(httpClient, context, settings)
        { }

        public async Task GetTablesAsync(DocBookData data, Dictionary<int, CidValues> cidTable)
        {
            await Initialization.ConfigureAwait(false);

            Debug.Assert(Reader != null);

            const int uidTables = 2;
            const int tagTables = 4;

            int tables = (Settings.GenerateUids ? uidTables : 0) + (Settings.GenerateTags ? tagTables : 0);
            int found = 0;
            while (found < tables && await Reader!.ReadAsync().ConfigureAwait(false))
            {
                _context.CancellationToken.ThrowIfCancellationRequested();

                if (Reader.NodeType == XmlNodeType.Element && Reader.LocalName == "table")
                {
                    string? id = Reader.GetAttribute("id", XNamespace.Xml.NamespaceName);
                    if (id == "table_A-1" && Settings.GenerateUids)
                    {
                        data.TableA1 = ReadTable(A1ToUid);
                    }
                    else if (id == "table_A-3" && Settings.GenerateUids)
                    {
                        data.TableA3 = ReadTable(row => A3ToUid(row, cidTable));
                    }
                    else if (id == "table_6-1" && Settings.GenerateTags)
                    {
                        data.Table61 = ReadTable(TableToTag);
                    }
                    else if (id == "table_7-1" && Settings.GenerateTags)
                    {
                        data.Table71 = ReadTable(TableToTag);
                    }
                    else if (id == "table_8-1" && Settings.GenerateTags)
                    {
                        data.Table81 = ReadTable(TableToTag);
                    }
                    else if (id == "table_9-1" && Settings.GenerateTags)
                    {
                        data.Table91 = ReadTable(TableToTag);
                    }
                    else
                    {
                        continue;
                    }

                    found++;
                }
            }
        }

        private static UidValues A1ToUid(IEnumerable<XElement> row)
            => new(
                GetValue(row.ElementAt(0)),
                GetValue(row.ElementAt(1)),
                GetValue(row.ElementAt(2)),
                GetValue(row.ElementAt(3)));

        private static UidValues A3ToUid(IEnumerable<XElement> row, Dictionary<int, CidValues> cidTable)
        {
            string? cid = row
                .ElementAt(1)
                .Descendants(Ns + "olink")?
                .FirstOrDefault()?
                .Attribute("targetptr")?
                .Value;

            if (cid is null)
                return default;

            cid = cid.Substring(9);
            CidValues cidValues = cidTable[int.Parse(cid, CultureInfo.InvariantCulture)];

            return new(
                GetValue(row.ElementAt(0)),
                $"{cidValues.Title} ({cid})",
                string.IsNullOrEmpty(cidValues.Keyword) ? string.Empty : $"{cidValues.Keyword}_{cid}",
                "ContextGroupName");
        }
    }
}
