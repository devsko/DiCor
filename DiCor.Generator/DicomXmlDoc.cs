using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace DiCor.Generator
{
    public class DicomXmlDoc : IDisposable
    {
        private record XmlAndTitle(
            XmlReader Xml,
            string Title) : IDisposable
        {
            public void Dispose() => Xml.Dispose();
        }

        protected static readonly XNamespace DocbookNS = XNamespace.Get("http://docbook.org/ns/docbook");

        private readonly HttpClient _httpClient;
        private readonly string _uri;
        private readonly string _resourceKey;
        protected readonly CancellationToken _cancellationToken;

        private XmlAndTitle? _resource;
        private Stream? _resourceStream;

        public string? DownloadedTitle { get; private set; }

        public XmlReader? Xml => _resource?.Xml;

        public string? Title => _resource?.Title;

        public DicomXmlDoc(HttpClient httpClient, string uri, string resourceKey, CancellationToken cancellationToken)
        {
            _httpClient = httpClient;
            _uri = uri;
            _resourceKey = resourceKey;
            _cancellationToken = cancellationToken;
        }

        public async Task<bool> IsUpToDateAsync()
        {
            _resource = await LoadXmlAsync(LoadResource()).ConfigureAwait(false);

            using (var request = new HttpRequestMessage(HttpMethod.Get, _uri))
            using (HttpResponseMessage response = await _httpClient
                    .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, _cancellationToken)
                    .ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();
                using (Stream content = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                using (XmlAndTitle downloadXml = await LoadXmlAsync(content).ConfigureAwait(false))
                { 
                    return Title == (DownloadedTitle = downloadXml.Title);
                }
            }
        }

        private Stream LoadResource()
        {
            Assembly asm = Assembly.GetCallingAssembly();
            return _resourceStream = asm.GetManifestResourceStream($"{asm.GetName().Name}.{_resourceKey}");
        }

        private async Task<XmlAndTitle> LoadXmlAsync(Stream stream)
        {
            XmlReader xml = XmlReader.Create(stream, new XmlReaderSettings
            {
                Async = true,
                ValidationFlags = XmlSchemaValidationFlags.None,
                ValidationType = ValidationType.None
            });

            while (await xml.ReadAsync().ConfigureAwait(false))
            {
                _cancellationToken.ThrowIfCancellationRequested();

                if (xml.NodeType == XmlNodeType.Element && xml.LocalName == "subtitle")
                {
                    string title = await xml.ReadElementContentAsStringAsync().ConfigureAwait(false);
                    return new (xml, title);
                }
            }

            return new (xml, string.Empty);
        }

        public void Dispose()
        {
            _resource?.Dispose();
            _resourceStream?.Dispose();
            _resource = null;
            _resourceStream = null;
        }
    }
}
