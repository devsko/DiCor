using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

using Microsoft.CodeAnalysis;

namespace DiCor.Generator
{
    public class DocBook : IDisposable
    {
        private record XmlAndTitle(XmlReader Xml, string Title) : IDisposable
        {
            public void Dispose() => Xml.Dispose();
        }

        protected static readonly XNamespace Ns = XNamespace.Get("http://docbook.org/ns/docbook");

        private readonly HttpClient _httpClient;
        private readonly GeneratorExecutionContext _context;
        private readonly string _path;
        private readonly string _uri;
        protected readonly CancellationToken _cancellationToken;

        private XmlAndTitle? _xml;

        public string? Title => _xml?.Title;

        public XmlReader? Reader => _xml?.Xml;

        public DocBook(HttpClient httpClient, GeneratorExecutionContext context, string uri, CancellationToken cancellationToken)
        {
            _httpClient = httpClient;
            _context = context;
            _path = context.AdditionalFiles.Single(text => Path.GetFileName(text.Path).Equals($"{GetType().Name}.xml", StringComparison.OrdinalIgnoreCase)).Path;
            _uri = uri;
            _cancellationToken = cancellationToken;
        }

        protected async Task InitializeAsync()
        {
            bool download = false;
            XmlAndTitle? fileXml = null;

            if (!File.Exists(_path))
            {
                _context.ReportDiagnostic(Diag.InvalidXml(_path, "File not found."));
                download = true;
            }
            else
            {
                Stream stream = File.OpenRead(_path);
                try
                {
                    fileXml = await LoadXmlAsync(stream).ConfigureAwait(false);
                }
                catch (XmlException ex)
                {
                    stream.Dispose();
                    _context.ReportDiagnostic(Diag.InvalidXml(_path, ex));
                    download = true;
                }
            }

            FileSaveStream saveStream = await StartDownloadAsync().ConfigureAwait(false);
            XmlAndTitle downloadXml = await LoadXmlAsync(saveStream).ConfigureAwait(false);

            download |= fileXml?.Title != downloadXml.Title;

            if (download)
            {
                _context.ReportDiagnostic(Diag.ResourceOutdated(_path, _uri));
                await saveStream.StopBufferingAsync().ConfigureAwait(false);
                _xml = downloadXml;
                fileXml?.Dispose();
            }
            else
            {
                _xml = fileXml;
                downloadXml.Dispose();
            }
        }

        private async Task<FileSaveStream> StartDownloadAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, _uri);
            HttpResponseMessage response = await _httpClient
                    .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, _cancellationToken)
                    .ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            Stream content = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

            return new FileSaveStream(content, _path);
        }

        private async Task<XmlAndTitle> LoadXmlAsync(Stream stream)
        {
            XmlReader xml = XmlReader.Create(stream, new XmlReaderSettings
            {
                CloseInput = true,
                Async = true,
                ValidationFlags = XmlSchemaValidationFlags.None,
                ValidationType = ValidationType.None,
            });

            while (await xml.ReadAsync().ConfigureAwait(false))
            {
                _cancellationToken.ThrowIfCancellationRequested();

                if (xml.NodeType == XmlNodeType.Element && xml.LocalName == "subtitle")
                {
                    string title = await xml.ReadElementContentAsStringAsync().ConfigureAwait(false);
                    return new(xml, title);
                }
            }

            return new(xml, string.Empty);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _xml?.Dispose();
                _xml = null;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
