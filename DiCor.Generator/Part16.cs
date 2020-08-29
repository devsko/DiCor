#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

using Microsoft.CodeAnalysis;

namespace DiCor.Generator
{
    public class Part16 : DicomXmlDoc
    {
        public const string ResourceKey = "xml.part16.xml";
        public const string Uri = "http://medical.nema.org/medical/dicom/current/source/docbook/part16/part16.xml";

        public Part16(HttpClient httpClient, string projectPath, CancellationToken cancellationToken)
            : base(httpClient, projectPath, Uri, ResourceKey, cancellationToken)
        { }

        public async Task<Dictionary<int, string>> GetSectionsByIdAsync(SourceGeneratorContext context)
        {
            await InitializeAsync(context).ConfigureAwait(false);

            Debug.Assert(Reader != null);

            var sections = new Dictionary<int, string>();
            while (await Reader!.ReadAsync().ConfigureAwait(false))
            {
                _cancellationToken.ThrowIfCancellationRequested();

                if (Reader.NodeType == XmlNodeType.Element && Reader.LocalName == "section")
                {
                    string? id = Reader.GetAttribute("id", XNamespace.Xml.NamespaceName);
                    if (id != null
                        && id.StartsWith("sect_CID_", StringComparison.Ordinal)
                        && int.TryParse(id.Substring(9), out int i)
                        && Reader.ReadToDescendant("title", DocbookNS.NamespaceName))
                    {
                        sections.Add(i, await Reader.ReadInnerXmlAsync());
                    }
                }
            }

            return sections;
        }
    }
}
