using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DiCor.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Threading;

namespace DiCor.Generator
{
    [Generator]
    public class UidSourceGenerator : ISourceGenerator
    {
        private static readonly HttpClient s_httpClient = new();
        internal static JoinableTaskFactory JoinableTaskFactory { get; } = new(new JoinableTaskContext());

        public static Assembly Assembly => typeof(UidSourceGenerator).Assembly;
        public static string AssemblyName => Assembly.GetName().Name ?? string.Empty;

        public void Initialize(GeneratorInitializationContext context)
        {
            //if (!Debugger.IsAttached)
            //    Debugger.Launch();
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

            JoinableTaskFactory.Run(() => ExecuteAsync(context, settings), JoinableTaskCreationOptions.LongRunning);

            if (settingsText is not null)
                File.WriteAllText(settingsText.Path, JsonSerializer.Serialize(settings));

            static async Task ExecuteAsync(GeneratorExecutionContext context, Settings settings)
            {
                try
                {
                    using (var part16 = new Part16(s_httpClient, context, settings))
                    using (var part06 = new Part06(s_httpClient, context, settings))
                    {
                        Dictionary<int, string> cidTable = await part16.GetSectionsByIdAsync().ConfigureAwait(false);
                        if (cidTable.Count > 0)
                        {
                            (Uid[]? tableA1, Uid[]? tableA3) = await part06.GetTablesAsync(cidTable).ConfigureAwait(false);
                            if (tableA1 != null && tableA3 != null)
                            {
                                string header =
$"// Generated code\r\n" +
$"// {part06.Title} ({Part06.Uri})\r\n" +
$"// {part16.Title} ({Part16.Uri})\r\n\r\n";

                                (string uids, string hashSet) = CreateCode(context, header, tableA1, tableA3);

                                context.AddSource("Uid.Uids.g.cs", SourceText.From(uids, Encoding.UTF8));
                                context.AddSource("Uid.HashSet.g.cs", SourceText.From(hashSet, Encoding.UTF8));
                            }
                        }
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

        private static (string, string) CreateCode(GeneratorExecutionContext context, string header, Uid[] tableA1, Uid[] tableA3)
        {
            var hashSet = new StringBuilder(header);
            var uids = new StringBuilder(header);

            hashSet.Append(
"using System.Collections.Generic;\r\n" +
"\r\n" +
"namespace DiCor\r\n" +
"{\r\n" +
"    public partial struct Uid\r\n" +
"    {\r\n" +
"        private static readonly HashSet<Uid> s_uids = new()\r\n" +
"        {\r\n");

            uids.Append(
"namespace DiCor\r\n" +
"{\r\n" +
"    public partial struct Uid\r\n" +
"    {\r\n");

            foreach (Uid uid in tableA1.Concat(tableA3))
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                StorageCategory storageCategory = Uid.GetStorageCategory(uid);
                string category = storageCategory != StorageCategory.None ? $", storageCategory: StorageCategory.{storageCategory}" : "";
                string isRetired = uid.IsRetired ? ", isRetired: true" : "";
                string symbol = ToSymbol(uid);

                hashSet.Append(
"            Uid.").Append(symbol).Append(",\r\n");

                uids.Append(
"        public static readonly Uid ").Append(symbol).Append(" = new(\"").Append(uid.Value).Append("\", \"").Append(uid.Name).Append("\", UidType.").Append(uid.Type).Append(category).Append(isRetired).Append(");\r\n");
            }

            hashSet.Append(
"        };\r\n" +
"    }\r\n" +
"}\r\n");
            uids.Append(
"    }\r\n" +
"}\r\n");

            return (uids.ToString(), hashSet.ToString());
        }

        private static string ToSymbol(Uid uid, bool useValue = false)
        {
            ReadOnlySpan<char> retired = "(Retired)".AsSpan();
            ReadOnlySpan<char> process = "(Process ".AsSpan();

            // Additional 9 chars for appending "_RETIRED" and prepending "_" if needed
            Span<char> buffer = stackalloc char[(useValue ? uid.Value : uid.Name).Length + 8 + 1];

            Span<char> symbol = buffer.Slice(1);

            (useValue ? uid.Value : uid.Name).AsSpan().CopyTo(symbol);

            ReadOnlySpan<char> read = symbol;
            int writeAt = 0;
            bool upper = true;

            while (read.Length > 0)
            {
                char ch = read[0];
                if (ch == ':')
                {
                    break;
                }

                if (ch == '(' && (read.StartsWith(retired) || read.StartsWith(process)))
                {
                    read = read.Slice(9);
                }
                else
                {
                    if (char.IsLetterOrDigit(ch) || ch == '_')
                    {
                        symbol[writeAt++] = upper ? char.ToUpperInvariant(ch) : ch;
                        upper = false;
                    }
                    else if (ch == ' ' || ch == '-')
                    {
                        upper = true;
                    }
                    else if (ch == '&' || ch == '.')
                    {
                        symbol[writeAt++] = '_';
                        upper = true;
                    }
                    read = read.Slice(1);
                }
            }
            if (writeAt == 0)
            {
                return ToSymbol(uid, true);
            }
            if (char.IsDigit(symbol[0]))
            {
                symbol = buffer;
                symbol[0] = '_';
                writeAt++;
            }
            if (uid.IsRetired)
            {
                "_RETIRED".AsSpan().CopyTo(symbol.Slice(writeAt));
                writeAt += 8;
            }

            return symbol.Slice(0, writeAt).ToString();
        }
    }
}
