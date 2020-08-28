using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace DiCor.Generator
{
    public class Part16 : DicomXmlDoc
    {
        public const string ResourceKey = "xml.part16.xml";
        public const string Uri = "http://medical.nema.org/medical/dicom/current/source/docbook/part16/part16.xml";

        public Part16(HttpClient httpClient, CancellationToken cancellationToken)
            : base(httpClient, Uri, ResourceKey, cancellationToken)
        { }

        public async Task<Dictionary<int, string>> GetSectionsByIdAsync()
        {
            Debug.Assert(Xml != null);

            var sections = new Dictionary<int, string>();
            while (await Xml!.ReadAsync().ConfigureAwait(false))
            {
                _cancellationToken.ThrowIfCancellationRequested();

                if (Xml.NodeType == XmlNodeType.Element && Xml.LocalName == "section")
                {
                    string? id = Xml.GetAttribute("id", XNamespace.Xml.NamespaceName);
                    if (id != null
                        && id.StartsWith("sect_CID_", StringComparison.Ordinal)
                        && int.TryParse(id.Substring(9), out int i)
                        && Xml.ReadToDescendant("title", DocbookNS.NamespaceName))
                    {
                        sections.Add(i, Xml.ReadInnerXml());
                    }
                }
            }

            return sections;
        }
    }
}
