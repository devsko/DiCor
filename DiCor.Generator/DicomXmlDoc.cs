﻿#nullable enable

using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

using Microsoft.CodeAnalysis;

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
        private readonly string _resourcePath;
        private readonly string _uri;
        private readonly string _resourceKey;
        protected readonly CancellationToken _cancellationToken;

        private XmlAndTitle? _xml;

        public string? Title => _xml?.Title;

        public XmlReader? Reader => _xml?.Xml;


        public DicomXmlDoc(HttpClient httpClient, string projectPath, string uri, string resourceKey, CancellationToken cancellationToken)
        {
            _httpClient = httpClient;
            _resourcePath = CreatePath(projectPath, resourceKey);
            _uri = uri;
            _resourceKey = resourceKey;
            _cancellationToken = cancellationToken;
        }

        private static string CreatePath(string basePath, string key)
        {
            string path = basePath;
            string[] parts = key.Split('.');
            for (int i = 0; i < parts.Length - 2; i++)
            {
                path = Path.Combine(path, parts[i]);
            }
            path = Path.Combine(path, Path.ChangeExtension(parts[parts.Length - 2], parts[parts.Length - 1]));

            return path;
        }

        protected async Task InitializeAsync(SourceGeneratorContext context)
        {
            bool download = false;

            XmlAndTitle? resourceXml = null;
            try
            {
                resourceXml = await LoadXmlAsync(LoadResource()).ConfigureAwait(false);
            }
            catch (XmlException ex)
            {
                context.ReportDiagnostic(Diagnostics.XmlException(_resourceKey, ex));
                download = true;
            }

            FileSaveStream saveStream = await StartDownloadAsync().ConfigureAwait(false);
            XmlAndTitle downloadXml = await LoadXmlAsync(saveStream).ConfigureAwait(false);

            download |= resourceXml?.Title != downloadXml.Title;

            if (download)
            {
                context.ReportDiagnostic(Diagnostics.ResourceOutdated(_resourceKey, _uri));
                await saveStream.StopBufferingAsync().ConfigureAwait(false);
                _xml = downloadXml;
                resourceXml?.Dispose();
            }
            else
            {
                _xml = resourceXml;
                downloadXml.Dispose();
            }
        }

        private Stream LoadResource()
            => UidSourceGenerator.Assembly.GetManifestResourceStream($"{UidSourceGenerator.AssemblyName}.{_resourceKey}");

        private async Task<FileSaveStream> StartDownloadAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, _uri);
            HttpResponseMessage response = await _httpClient
                    .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, _cancellationToken)
                    .ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            Stream content = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

            return new FileSaveStream(content, _resourcePath);
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
                    return new (xml, title);
                }
            }

            return new (xml, string.Empty);
        }

        public void Dispose()
        {
            _xml?.Dispose();
            _xml= null;
        }
    }
}
