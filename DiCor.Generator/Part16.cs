using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;

namespace DiCor.Generator
{
    public class Part16 : DocBook
    {
        public const string Uri = "http://medical.nema.org/medical/dicom/current/source/docbook/part16/part16.xml";

        public Part16(HttpClient httpClient, GeneratorExecutionContext context, Settings settings)
            : base(httpClient, Uri, context, settings)
        { }

        public async Task<Dictionary<int, string>> GetSectionsByIdAsync()
        {
            await InitializeAsync().ConfigureAwait(false);

            Debug.Assert(Reader != null);

            var sections = new Dictionary<int, string>();
            while (await Reader!.ReadAsync().ConfigureAwait(false))
            {
                _context.CancellationToken.ThrowIfCancellationRequested();

                if (Reader.NodeType == XmlNodeType.Element && Reader.LocalName == "section")
                {
                    string? id = Reader.GetAttribute("id", XNamespace.Xml.NamespaceName);
                    if (id != null
                        && id.StartsWith("sect_CID_", StringComparison.Ordinal)
                        && int.TryParse(id.Substring(9), out int i)
                        && Reader.ReadToDescendant("title", Ns.NamespaceName))
                    {
                        sections.Add(i, await Reader.ReadInnerXmlAsync());
                    }
                }
            }

            return sections;
        }
    }
}
