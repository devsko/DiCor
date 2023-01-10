using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Threading;

namespace DiCor.Generator
{
    [Generator]
    internal class Generator : ISourceGenerator
    {
        private static readonly HttpClient s_httpClient = new();
        internal static JoinableTaskFactory JoinableTaskFactory { get; } = new(new JoinableTaskContext());

        public static Assembly Assembly => typeof(Generator).Assembly;
        public static string AssemblyName => Assembly.GetName().Name ?? string.Empty;

        public Dictionary<int, CidValues>? CidTable { get; set; }
        public UidValues[]? TableA1 { get; set; }
        public UidValues[]? TableA3 { get; set; }
        public TagValues[]? Table61 { get; set; }
        public TagValues[]? Table71 { get; set; }
        public TagValues[]? Table81 { get; set; }
        public TagValues[]? Table91 { get; set; }
        public TagValues[]? TableE11 { get; set; }
        public TagValues[]? TableE21 { get; set; }

        public void Initialize(GeneratorInitializationContext context)
        {
            //if (!System.Diagnostics.Debugger.IsAttached)
            //    System.Diagnostics.Debugger.Launch();
        }

        public void Execute(GeneratorExecutionContext context)
        {
            Settings? settings;
            AdditionalText? settingsText = context
                .AdditionalFiles
                .FirstOrDefault(text => Path.GetFileName(text.Path).Equals("settings.json", StringComparison.OrdinalIgnoreCase));
            if (settingsText is null)
                settings = new Settings();
            else
            {
                string? settingsJson = settingsText.GetText()?.ToString();
                settings = (settingsJson is null ? null : JsonSerializer.Deserialize<Settings>(settingsJson)) ?? new Settings();
                settings.CheckForUpdate = settings.LastUpdateCheck.AddHours(1) < DateTime.UtcNow;
            }

            JoinableTaskFactory.Run(() => ExecuteAsync(this, context, settings), JoinableTaskCreationOptions.LongRunning);

            if (settingsText is not null)
                File.WriteAllText(settingsText.Path, JsonSerializer.Serialize(settings));

            static async Task ExecuteAsync(Generator generator, GeneratorExecutionContext context, Settings settings)
            {
                try
                {
                    using (var part16 = new Part16(s_httpClient, context, settings))
                    using (var part06 = new Part06(s_httpClient, context, settings))
                    using (var part07 = new Part07(s_httpClient, context, settings))
                    {
                        await part16.GetSectionsByIdAsync(generator).ConfigureAwait(false);
                        await part06.GetTablesAsync(generator).ConfigureAwait(false);
                        await part07.GetTablesAsync(generator).ConfigureAwait(false);

                        string header = $"""
                            // Generated code
                            // {part06.Title} ({Part06.Uri})
                            // {part07.Title} ({Part07.Uri})
                            // {part16.Title} ({Part16.Uri})

                            """ + Environment.NewLine;

                        CreateCode(generator, context, header);
                    }
                    if (settings.CheckForUpdate)
                        settings.LastUpdateCheck = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    context.ReportDiagnostic(Diag.UnexpectedException(ex));
                }
            }
        }

        private static void CreateCode(Generator generator, GeneratorExecutionContext context, string header)
        {
            var hashSet = new StringBuilder(header);
            var uids = new StringBuilder(header);

            hashSet.AppendLine("""
                using System.Collections.Frozen;
                using System.Collections.Generic;
                
                namespace DiCor
                {
                    partial struct UidDetails
                    {
                        public static partial UidDetails Get(Uid uid)
                            => s_dictionary.TryGetValue(uid.Value, out UidDetails details) ? details : new UidDetails(uid);

                        private static readonly FrozenDictionary<byte[], UidDetails> s_dictionary = InitializeDictionary(new Dictionary<byte[], UidDetails>());

                        private static FrozenDictionary<byte[], UidDetails> InitializeDictionary(Dictionary<byte[], UidDetails> dictionary)
                        {
                            void Add(Uid uid, string name, UidType type, StorageCategory storageCategory = StorageCategory.None, bool isRetired = false)
                                => dictionary.Add(uid.Value, new UidDetails(uid, name, type, storageCategory, isRetired));

                """);

            uids.AppendLine("""
                namespace DiCor
                {
                    partial record struct Uid
                    {
                """);

            foreach (UidValues values in generator.TableA1.Concat(generator.TableA3))
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
                    => hashSet
                        .Append(' ', 12)
                        .Append("Add(").Append(uid).Append(", \"")
                        .Append(values.Name).Append("\", UidType.").Append(values.Type)
                        .Append(category).Append(isRetired).AppendLine(");");
            }

            hashSet.AppendLine("""
                        return dictionary.ToFrozenDictionary();
                    }
                }
            }
            """);
            uids.AppendLine("""
                }
            }
            """);

            context.AddSource("Uid.Uids.g.cs", SourceText.From(uids.ToString(), Encoding.UTF8));
            context.AddSource("UidDetails.Dictionary.g.cs", SourceText.From(hashSet.ToString(), Encoding.UTF8));
        }
    }
}
