using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Threading;

namespace DiCor.Generator
{
    [Generator]
    internal class Generator : IIncrementalGenerator
    {
        private static readonly HttpClient s_httpClient = new();
        internal static JoinableTaskFactory JoinableTaskFactory { get; } = new(new JoinableTaskContext());

        public Dictionary<int, CidValues>? CidTable { get; set; }
        public UidValues[]? TableA1 { get; set; }
        public UidValues[]? TableA3 { get; set; }
        public TagValues[]? Table61 { get; set; }
        public TagValues[]? Table71 { get; set; }
        public TagValues[]? Table81 { get; set; }
        public TagValues[]? Table91 { get; set; }
        public TagValues[]? TableE11 { get; set; }
        public TagValues[]? TableE21 { get; set; }

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            //if (!System.Diagnostics.Debugger.IsAttached)
            //    System.Diagnostics.Debugger.Launch();

            IncrementalValueProvider<ImmutableArray<AdditionalText>> docbooksProvider = context
                .AdditionalTextsProvider
                .Where(static text => Path.GetDirectoryName(text.Path).EndsWith("\\docbook", StringComparison.OrdinalIgnoreCase))
                .Collect();

            context.RegisterSourceOutput(docbooksProvider, (context, docbookTexts) =>
            {
                Settings settings;
                AdditionalText? settingsText = docbookTexts
                    .FirstOrDefault(text => Path.GetFileName(text.Path).Equals("settings.json", StringComparison.OrdinalIgnoreCase));
                if (settingsText is null)
                {
                    settings = new Settings();
                }
                else
                {
                    string? settingsJson = settingsText.GetText(context.CancellationToken)?.ToString();
                    settings = (settingsJson is null ? null : JsonSerializer.Deserialize<Settings>(settingsJson)) ?? new Settings();
                    settings.CheckForUpdate = settings.LastUpdateCheck.AddHours(1) < DateTime.UtcNow;
                }

                JoinableTaskFactory.Run(() => ExecuteAsync(context, docbookTexts, settings), JoinableTaskCreationOptions.LongRunning);

                if (settingsText is not null)
                    File.WriteAllText(settingsText.Path, JsonSerializer.Serialize(settings));
            });
        }

        private async Task ExecuteAsync(SourceProductionContext context, ImmutableArray<AdditionalText> docbookTexts, Settings settings)
        {
            try
            {
                using (var part16 = new Part16(s_httpClient, context, docbookTexts, settings))
                using (var part06 = new Part06(s_httpClient, context, docbookTexts, settings))
                using (var part07 = new Part07(s_httpClient, context, docbookTexts, settings))
                {
                    await part16.GetSectionsByIdAsync(this).ConfigureAwait(false);
                    await part06.GetTablesAsync(this).ConfigureAwait(false);
                    await part07.GetTablesAsync(this).ConfigureAwait(false);

                    string header = $"""
                            // Generated code
                            // {part06.Title} ({Part06.Uri})
                            // {part07.Title} ({Part07.Uri})
                            // {part16.Title} ({Part16.Uri})

                            """ + Environment.NewLine;

                    CreateCode(context, header);
                }
                if (settings.CheckForUpdate)
                    settings.LastUpdateCheck = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                context.ReportDiagnostic(Diag.UnexpectedException(ex));
            }
        }

        private void CreateCode(SourceProductionContext context, string header)
        {
            var details = new StringBuilder(header);
            var uids = new StringBuilder(header);

            details.AppendLine("""
                using System;
                using System.Collections.Frozen;
                using System.Collections.Generic;
                
                namespace DiCor
                {
                    partial struct Uid
                    {
                        public partial Details? GetDetails()
                            => s_dictionary.TryGetValue(this, out Details details) ? details : null;

                        private static readonly FrozenDictionary<Uid, Details> s_dictionary = InitializeDictionary();

                        private static FrozenDictionary<Uid, Details> InitializeDictionary()
                        {
                            return Enumerate().ToFrozenDictionary();

                            IEnumerable<KeyValuePair<Uid, Details>> Enumerate()
                            {
                """);

            uids.AppendLine("""
                namespace DiCor
                {
                    partial struct Uid
                    {
                """);

            foreach (UidValues values in TableA1.Concat(TableA3))
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                StorageCategory storageCategory = values.StorageCategory;
                string category = storageCategory != StorageCategory.None ? $", StorageCategory.{storageCategory}" : "";
                string isRetired = values.IsRetired ? ", isRetired: true" : "";
                string symbol = values.Symbol();
                string uidConstant = $"Uid.{symbol}";
                string newUid = $"new Uid(\"{values.Value}\"u8, false)";

                if (values.IsRetired)
                {
                    AppendNewUidDetails(newUid);
                }
                else
                {
                    AppendNewUidDetails(uidConstant);
                    uids.Append(' ', 8)
                        .Append("///<summary>").Append(values.Type).Append(": ").Append(values.Name).AppendLine("</summary>")
                        .Append(' ', 8)
                        .Append("public static readonly Uid ").Append(symbol).Append(" = ")
                        .Append(newUid)
                        .AppendLine(";");
                }

                void AppendNewUidDetails(string uid)
                    => details
                        .Append(' ', 16)
                        .Append("yield return new(").Append(uid).Append(", ").Append("new(\"")
                        .Append(values.Name).Append("\", UidType.").Append(values.Type)
                        .Append(category).Append(isRetired).AppendLine("));");
            }

            details.AppendLine("""
                        }
                    }
                }
            }
            """);
            uids.AppendLine("""
                }
            }
            """);

            context.AddSource("Uid.Uids.g.cs", SourceText.From(uids.ToString(), Encoding.UTF8));
            context.AddSource("Uid.Details.g.cs", SourceText.From(details.ToString(), Encoding.UTF8));
        }
    }
}
