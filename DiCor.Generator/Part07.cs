using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;

namespace DiCor.Generator
{
    public class Part07 : DocBook
    {
        public const string Uri = "http://medical.nema.org/medical/dicom/current/source/docbook/part07/part07.xml";

        public Part07(HttpClient httpClient, GeneratorExecutionContext context, Settings settings)
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
                    if (id == "table_E.1-1")
                    {
                        generator.TableE11 = ReadTable(TableToTag);
                    }
                    else if (id == "table_E.2-1")
                    {
                        generator.TableE21 = ReadTable(TableToTag);
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
