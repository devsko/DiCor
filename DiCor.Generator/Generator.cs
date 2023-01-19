﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Threading;

namespace DiCor.Generator
{
    [Generator(LanguageNames.CSharp)]
    internal sealed class Generator : IIncrementalGenerator
    {
        private sealed class IdentifierComparer : IEqualityComparer<(string, HashSet<string>)>
        {
            public static readonly IdentifierComparer Instance = new();

            private IdentifierComparer()
            { }

            public bool Equals((string, HashSet<string>) x, (string, HashSet<string>) y)
                => x.Item2.SetEquals(y.Item2);

            public int GetHashCode((string, HashSet<string>) obj)
                => throw new NotImplementedException();
        }

        internal static JoinableTaskFactory JoinableTaskFactory { get; } = new(new JoinableTaskContext());
        private static readonly HttpClient s_httpClient = new();

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            try
            {
                System.Net.ServicePointManager.DefaultConnectionLimit = 5;

                context.RegisterSourceOutput(
                    context.AnalyzerConfigOptionsProvider.Select((context, _) =>
                    {
                        context.GlobalOptions.TryGetValue("build_property.dicorgenerator_logfilepath", out string? logFilePath);
                        return logFilePath;
                    }),
                    (_, logFilePath) => { Logger.Initialize(logFilePath); });

                IncrementalValueProvider<Settings> settings = context.AnalyzerConfigOptionsProvider
                    .Select((context, _) => new Settings(context));

                IncrementalValueProvider<ImmutableArray<Settings>> settingsWithIdentifiers = context.AdditionalTextsProvider
                    .Where(text => Path.GetDirectoryName(text.Path).EndsWith("\\docbook", StringComparison.OrdinalIgnoreCase))
                    .Select(ToHashSet)
                    .WithComparer(IdentifierComparer.Instance)
                    .Combine(settings)
                    .Select(AddIdentifiers)
                    .Collect();

                context.RegisterSourceOutput(settingsWithIdentifiers, static (context, settings) =>
                {
                    JoinableTaskFactory.Run(() => ExecuteAsync(context, settings.First()), JoinableTaskCreationOptions.LongRunning);
                });
            }
            catch (Exception ex)
            {
                Logger.Log("Initialize", ex);
                throw;
            }
        }

        private static async Task ExecuteAsync(SourceProductionContext context, Settings settings)
        {
            Stopwatch watch = Stopwatch.StartNew();
            try
            {
                Logger.StartSession();
                Logger.Log(">>>");
                Logger.Log($"ShouldCheckUpdates: {settings.ShouldCheckUpdates}");
                Logger.Log($"GenerateUids: {settings.GenerateUids}");
                Logger.Log($"GenerateTags: {settings.GenerateTags}");

                using (var part16 = new Part16(s_httpClient, context, settings))
                using (var part06 = new Part06(s_httpClient, context, settings))
                using (var part07 = new Part07(s_httpClient, context, settings))
                {
                    DocBookData data = new(part06, part07, part16);
                    await data.ExtractAsync().ConfigureAwait(false);

                    CreateCode(context, settings, data);

                    settings.SaveState();
                }
            }
            catch (Exception ex)
            {
                Logger.Log("ExecuteAsync", ex);
                context.ReportDiagnostic(Diag.UnexpectedException(ex));
            }
            finally
            {
                Logger.Log($"<<< ({watch.ElapsedMilliseconds}ms)");
            }
        }

        private static void CreateCode(SourceProductionContext context, Settings settings, DocBookData data)
        {
            string header = $"""
                // <auto-generated />
                // {data.Part06.Title} ({data.Part06.Uri})
                // {data.Part07.Title} ({data.Part07.Uri})
                // {data.Part16.Title} ({data.Part16.Uri})

                """ + "\r\n";

            if (settings.GenerateUids)
            {
                var details = new StringBuilder(header);
                var uids = new StringBuilder(header);

                details.AppendLine($$"""
                    using System;
                    using System.Collections.Frozen;
                    using System.Collections.Generic;
                
                    namespace DiCor
                    {
                        partial struct Uid
                        {
                            public partial Details? GetDetails()
                            {
                                return s_dictionary.TryGetValue(this, out Details details) ? details : null;
                            }

                            private static readonly FrozenDictionary<Uid, Details> s_dictionary = InitializeDictionary();

                            private static FrozenDictionary<Uid, Details> InitializeDictionary()
                            {
                                return EnumerateDetails().ToFrozenDictionary();

                                IEnumerable<KeyValuePair<Uid, Details>> EnumerateDetails()
                                {
                    """);

                uids.AppendLine("""
                    namespace DiCor
                    {
                        partial struct Uid
                        {
                    """);

                foreach (UidValues values in data.TableA1.Concat(data.TableA3))
                {
                    context.CancellationToken.ThrowIfCancellationRequested();

                    StorageCategory storageCategory = values.StorageCategory;
                    string category = storageCategory != StorageCategory.None ? $", StorageCategory.{storageCategory}" : "";
                    string isRetired = values.IsRetired ? ", isRetired: true" : "";
                    string uidConstant = $"Uid.{values.Symbol}";
                    string newUid = $"new Uid(\"{values.Value}\"u8, false)";

                    if (settings.ShouldGenerateIdentifier(values))
                    {
                        AppendNewUidDetails(uidConstant);
                        uids.Append(' ', 8)
                            .Append("///<summary>").Append(values.Type).Append(": ").Append(values.Name).AppendLine("</summary>")
                            .Append(' ', 8)
                            .Append("public static readonly Uid ").Append(values.Symbol).Append(" = ")
                            .Append(newUid)
                            .AppendLine(";");
                    }
                    else
                    {
                        AppendNewUidDetails(newUid);
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

                string uidsString = uids.ToString();
                string detailsString = details.ToString();

                context.AddSource("Uid.Uids.g.cs", SourceText.From(uidsString, Encoding.UTF8));
                context.AddSource("Uid.Details.g.cs", SourceText.From(detailsString, Encoding.UTF8));
            }
        }

        private static (string File, HashSet<string> Identifiers) ToHashSet(AdditionalText text, CancellationToken cancellationToken)
        {
            HashSet<string> identifiers = new(StringComparer.OrdinalIgnoreCase);
            ReadOnlySpan<char> content = (text.GetText(cancellationToken)?.ToString()).AsSpan();

            int index;
            while ((index = content.IndexOfAny('\r', '\n')) >= 0)
                content = AddIdentifier(identifiers, content, index);

            AddIdentifier(identifiers, content, content.Length);

            return (Path.GetFileNameWithoutExtension(text.Path), identifiers);

            static ReadOnlySpan<char> AddIdentifier(HashSet<string> identifiers, ReadOnlySpan<char> content, int index)
            {
                ReadOnlySpan<char> identifier = content.Slice(0, index).Trim();
                if (identifier.Length > 0)
                    identifiers.Add(identifier.ToString());

                index++;
                while (true)
                {
                    if (index >= content.Length)
                        return default;

                    if (content[index] is not '\n' and not '\r')
                        return content.Slice(index);

                    index++;
                }
            }
        }

        private static Settings AddIdentifiers(((string File, HashSet<string> Identifiers) Identifiers, Settings Settings) combined, CancellationToken cancellationToken)
        {
            string file = combined.Identifiers.File;
            HashSet<string> identifiers = combined.Identifiers.Identifiers;

            if (file == "GenerateUids")
            {
                combined.Settings.UidIdentifiers = identifiers;
            }
            else if (file == "GenerateTags")
            {
                combined.Settings.TagIdentifiers = identifiers;
            }

            Logger.Log($"AddIdentifiers file: {file} {identifiers.Count} identifiers");

            return combined.Settings;
        }
    }
}
