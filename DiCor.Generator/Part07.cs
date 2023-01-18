using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;

namespace DiCor.Generator
{
    internal sealed class Part07 : DocBook
    {
        public Part07(HttpClient httpClient, SourceProductionContext context, Settings settings)
            : base(httpClient, context, settings)
        { }

        public async Task GetTablesAsync(DocBookData data)
        {
            await Initialization.ConfigureAwait(false);

            Debug.Assert(Reader != null);

            const int uidTables = 0;
            const int tagTables = 2;

            int tables = (Settings.GenerateUids ? uidTables : 0) + (Settings.GenerateTags ? tagTables : 0);
            int found = 0;
            while (found < tables && await Reader!.ReadAsync().ConfigureAwait(false))
            {
                _context.CancellationToken.ThrowIfCancellationRequested();

                if (Reader.NodeType == XmlNodeType.Element && Reader.LocalName == "table")
                {
                    string? id = Reader.GetAttribute("id", XNamespace.Xml.NamespaceName);
                    if (id == "table_E.1-1" && Settings.GenerateTags)
                    {
                        data.TableE11 = ReadTable(TableToTag);
                    }
                    else if (id == "table_E.2-1" && Settings.GenerateTags)
                    {
                        data.TableE21 = ReadTable(TableToTag);
                    }
                    else
                    {
                        continue;
                    }

                    found++;
                }
            }
        }

    }
}
