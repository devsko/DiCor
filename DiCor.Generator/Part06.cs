using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

using Microsoft.CodeAnalysis;

namespace DiCor.Generator
{
    internal class Part06 : DocBook
    {
        public const string Uri = "http://medical.nema.org/medical/dicom/current/source/docbook/part06/part06.xml";

        public Part06(HttpClient httpClient, GeneratorExecutionContext context, Settings settings)
            : base(httpClient, Uri, context, settings)
        { }

        public async Task GetTablesAsync(Generator generator)
        {
            await InitializeAsync().ConfigureAwait(false);

            Debug.Assert(Reader != null);

            int tablesFound = 0;
            while (await Reader!.ReadAsync().ConfigureAwait(false))
            {
                _context.CancellationToken.ThrowIfCancellationRequested();

                if (Reader.NodeType == XmlNodeType.Element && Reader.LocalName == "table")
                {
                    string? id = Reader.GetAttribute("id", XNamespace.Xml.NamespaceName);
                    if (id == "table_A-1")
                    {
                        generator.TableA1 = ReadTable(A1ToUid);
                    }
                    else if (id == "table_A-3")
                    {
                        generator.TableA3 = ReadTable(row => A3ToUid(row, generator.CidTable!));
                    }
                    else if (id == "table_6-1")
                    {
                        generator.Table61 = ReadTable(TableToTag);
                    }
                    else if (id == "table_7-1")
                    {
                        generator.Table71 = ReadTable(TableToTag);
                    }
                    else if (id == "table_8-1")
                    {
                        generator.Table81 = ReadTable(TableToTag);
                    }
                    else if (id == "table_9-1")
                    {
                        generator.Table91 = ReadTable(TableToTag);
                    }
                    else
                        continue;

                    if (++tablesFound >= 6)
                        break;
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
            CidValues cidValues = cidTable[int.Parse(cid)];

            return new(
                GetValue(row.ElementAt(0)),
                $"{cidValues.Title} ({cid})",
                string.IsNullOrEmpty(cidValues.Keyword) ? string.Empty : $"{cidValues.Keyword}_{cid}",
                "ContextGroupName");
        }
    }
}
