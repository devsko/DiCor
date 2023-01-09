using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
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
        protected readonly GeneratorExecutionContext _context;
        private readonly Settings _settings;
        private readonly string _path;
        private readonly string _uri;

        private XmlAndTitle? _xml;

        public string? Title => _xml?.Title;

        public XmlReader? Reader => _xml?.Xml;

        public DocBook(HttpClient httpClient, string uri, GeneratorExecutionContext context, Settings settings)
        {
            _httpClient = httpClient;
            _context = context;
            _settings = settings;
            AdditionalText text = context.AdditionalFiles.First(text => Path.GetFileName(text.Path).Equals($"{GetType().Name}.xml", StringComparison.OrdinalIgnoreCase));
            _path = text.Path;
            _uri = uri;
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

            if (download || _settings.CheckForUpdate)
            {
                FileSaveStream saveStream = await StartDownloadAsync().ConfigureAwait(false);
                XmlAndTitle downloadXml = await LoadXmlAsync(saveStream).ConfigureAwait(false);

                if (download || fileXml?.Title != downloadXml.Title)
                {
                    fileXml?.Dispose();
                    _context.ReportDiagnostic(Diag.ResourceOutdated(_path, _uri));
                    await saveStream.StopBufferingAsync().ConfigureAwait(false);
                    _xml = downloadXml;

                    return;
                }
                downloadXml.Dispose();
            }

            _xml = fileXml;
        }

        private async Task<FileSaveStream> StartDownloadAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, _uri);
            HttpResponseMessage response = await _httpClient
                    .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, _context.CancellationToken)
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
                _context.CancellationToken.ThrowIfCancellationRequested();

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

        protected T[]? ReadTable<T>(Func<IEnumerable<XElement>, T> selector) where T : struct
            => XElement.Load(Reader!.ReadSubtree())
                .Element(Ns + "tbody")?
                .Elements(Ns + "tr")
                .Select(tr => tr.Elements(Ns + "td").Select(td => td.Element(Ns + "para")))
                .Select(selector)
                .Where(t => !t.Equals(default(T)))
                .ToArray();

        protected static TagValues TableToTag(IEnumerable<XElement> row)
        {
            TagValues values = default;
            string tag = GetValue(row.ElementAt(0));
            if (!(tag.Length == 11 && tag[0] == '(' && tag[10] == ')' && tag[5] == ',') ||
                !ushort.TryParse(tag.Substring(1, 4), NumberStyles.AllowHexSpecifier, null, out values.Group) ||
                !ushort.TryParse(tag.Substring(6, 4), NumberStyles.AllowHexSpecifier, null, out values.Element))
            {
                return default;
            }
            values.MessageField = GetValue(row.ElementAt(1));
            values.Keyword = GetValue(row.ElementAt(2));
            values.VR = GetValue(row.ElementAt(3));
            values.VM = GetValue(row.ElementAt(4));
            values.IsRetired = (row.ElementAtOrDefault(5)?.Value.TrimStart() ?? string.Empty).StartsWith("RET", StringComparison.Ordinal);

            return values;
        }

        protected static string GetValue(XElement? element) => element?.Value.Replace("\u200b", "").Trim() ?? string.Empty;
    }
}
