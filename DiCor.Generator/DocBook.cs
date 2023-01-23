using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
    [SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1035:Do not use APIs banned for analyzers", Justification = "<Pending>")]
    internal class DocBook : IDisposable
    {
        private sealed record XmlAndTitle(XmlReader Xml, string Title) : IDisposable
        {
            public void Dispose() => Xml.Dispose();
        }

        protected static readonly XNamespace Ns = XNamespace.Get("http://docbook.org/ns/docbook");

        private readonly HttpClient _httpClient;
        protected readonly SourceProductionContext _context;
        private readonly Settings _settings;
        private readonly Task _initialization;

        private XmlAndTitle? _xml;

        protected Settings Settings => _settings;

        public Task Initialization => _initialization;

        public string? Title => _xml?.Title;

        public XmlReader? Reader => _xml?.Xml;

        public string Name => GetType().Name;

        public string Uri => $"http://medical.nema.org/medical/dicom/current/source/docbook/{Name}/{Name}.xml";

        public DocBook(HttpClient httpClient, SourceProductionContext context, Settings settings)
        {
            _httpClient = httpClient;
            _context = context;
            _settings = settings;
            _initialization = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            Stopwatch watch = Stopwatch.StartNew();
            Logger.Log($"{Name} starting initialization");

            string path = Path.Combine(_settings.DocBookDirectory, $"{Name}.xml");
            int retries = 0;

        Start:
            try
            {
                _xml = await LoadXmlAsync(File.OpenRead(path)).ConfigureAwait(false);
                Logger.Log($"XML loaded from {path}");
            }
            catch (XmlException ex)
            {
                Logger.Log(path, ex);
                _context.ReportDiagnostic(Diag.InvalidXml(path, ex));
            }
            catch (FileNotFoundException ex)
            {
                Logger.Log(path, ex);
                _context.ReportDiagnostic(Diag.InvalidXml(path, ex));
            }
            catch (IOException ex)
            {
                Logger.Log(path, ex);
                if (++retries <= 5)
                {
                    Logger.Log("Retry in 5 seconds");
                    await Task.Delay(5_000).ConfigureAwait(false);
                    goto Start;
                }
            }

            bool checkForUpdates = _settings.ShouldCheckUpdates || _xml is null;
            if (checkForUpdates)
            {
                Logger.Log($"{Name} checking for updates");

                string uri = Uri;
                FileSaveStream saveStream = await DownloadAsync(uri, path).ConfigureAwait(false);
                XmlAndTitle downloadXml = await LoadXmlAsync(saveStream).ConfigureAwait(false);
                Logger.Log($"XML loaded from {uri}");

                if (_xml is null || _xml.Title != downloadXml.Title)
                {
                    Logger.Log($"{Name} update found");

                    _context.ReportDiagnostic(Diag.ResourceOutdated(path, uri));

                    _xml?.Dispose();
                    await saveStream.StopBufferingAsync().ConfigureAwait(false);
                    _xml = downloadXml;
                }
                else
                {
                    Logger.Log($"{Name} is up to date");

                    downloadXml.Dispose();
                }
            }
            Logger.Log($"{Name} initialization complete ({watch.ElapsedMilliseconds}ms)");
            _settings.DidCheckUpdates(checkForUpdates);
        }

        private async Task<FileSaveStream> DownloadAsync(string uri, string path)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            HttpResponseMessage response = await _httpClient
                    .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, _context.CancellationToken)
                    .ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            Stream content = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

            return new FileSaveStream(content, path);
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
                    return new XmlAndTitle(xml, title);
                }
            }

            return new XmlAndTitle(xml, string.Empty);
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
            if (!(tag.Length == 11 && tag[0] == '(' && tag[10] == ')' && tag[5] == ','))
            {
                return default;
            }
            values.Group = tag.Substring(1, 4);
            values.Element = tag.Substring(6, 4);
            values.MessageField = GetValue(row.ElementAt(1));
            values.Keyword = GetValue(row.ElementAt(2));
            values.VR = GetValue(row.ElementAt(3));
            values.VM = GetValue(row.ElementAt(4));
            values.IsRetired = (row.ElementAtOrDefault(5)?.Value.TrimStart() ?? string.Empty).StartsWith("RET", StringComparison.Ordinal);

            return values;
        }

        protected static string GetValue(XElement? element)
            => element?.Value.Replace("\u200b", "").Trim() ?? string.Empty;
    }
}
