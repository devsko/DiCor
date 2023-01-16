using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;

namespace DiCor.Generator
{
    internal class Part07 : DocBook
    {
        public Part07(HttpClient httpClient, SourceProductionContext context, Settings settings)
            : base(httpClient, context, settings)
        { }

        public async Task GetTablesAsync(DocBookData data)
        {
            await Initialization.ConfigureAwait(false);

            Debug.Assert(Reader != null);

            int tablesFound = 0;
            while (await Reader!.ReadAsync().ConfigureAwait(false))
            {
                _context.CancellationToken.ThrowIfCancellationRequested();

                if (Reader.NodeType == XmlNodeType.Element && Reader.LocalName == "table")
                {
                    string? id = Reader.GetAttribute("id", XNamespace.Xml.NamespaceName);
                    if (id == "table_E.1-1")
                    {
                        data.TableE11 = ReadTable(TableToTag);
                    }
                    else if (id == "table_E.2-1")
                    {
                        data.TableE21 = ReadTable(TableToTag);
                    }
                    else
                        continue;

                    if (++tablesFound >= 2)
                        break;
                }
            }
        }

    }
}
