using System;
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
    internal class Part16 : DocBook
    {
        public Part16(HttpClient httpClient, SourceProductionContext context, Settings settings)
            : base(httpClient, context, settings)
        { }

        public async Task GetSectionsByIdAsync(DocBookData data)
        {
            await Initialization.ConfigureAwait(false);

            Debug.Assert(Reader != null);

            var sections = new Dictionary<int, CidValues>();
            while (await Reader!.ReadAsync().ConfigureAwait(false))
            {
                _context.CancellationToken.ThrowIfCancellationRequested();

                if (Reader.NodeType == XmlNodeType.Element && Reader.LocalName == "section")
                {
                    CidValues values = default;

                    string? id = Reader.GetAttribute("id", XNamespace.Xml.NamespaceName);
                    if (id != null
                        && id.StartsWith("sect_CID_", StringComparison.Ordinal)
                        && int.TryParse(id.Substring(9), out values.Cid))
                    {
                        XElement element = XElement.Load(Reader.ReadSubtree());
                        values.Title = GetValue(element.Element(Ns + "title"));
                        values.Keyword = GetValue(element
                            .Element(Ns + "variablelist")?
                            .Elements(Ns + "varlistentry")
                            .Where(el => GetValue(el.Element(Ns + "term")) == "Keyword:")
                            .SingleOrDefault()?
                            .Element(Ns + "listitem")
                            .Element(Ns + "para"));
                        sections.Add(values.Cid, values);
                    }
                }
            }

            data.CidTable = sections;
        }
    }
}
