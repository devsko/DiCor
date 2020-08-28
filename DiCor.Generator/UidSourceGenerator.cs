using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using DiCor.Internal;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace DiCor.Generator
{
    [Generator]
    public class UidSourceGenerator : ISourceGenerator
    {
        private const string DiagnosticCategory = "DiCor.Generator";

        private static readonly DiagnosticDescriptor s_generatorStarted = new DiagnosticDescriptor(
                        "GEN001",
                        "Uid generator started",
                        "Uid generator started",
                        DiagnosticCategory,
                        DiagnosticSeverity.Info,
                        true);
        private static readonly DiagnosticDescriptor s_generatorFinished = new DiagnosticDescriptor(
                        "GEN901",
                        "Uid generator finished",
                        "Uid generator finished",
                        DiagnosticCategory,
                        DiagnosticSeverity.Info,
                        true);
        private static readonly DiagnosticDescriptor s_resourceOutdated = new DiagnosticDescriptor(
                        "GEN002",
                        "Resource outdated",
                        "The new version '{0}' must be downladed from {1]",
                        DiagnosticCategory,
                        DiagnosticSeverity.Error,
                        true);

        private readonly HttpClient _httpClient = new HttpClient();

        public void Initialize(InitializationContext context)
        { }

        public void Execute(SourceGeneratorContext context)
        {
            context.ReportDiagnostic(Diagnostic.Create(s_generatorStarted, default));
            ExecuteAsync().Wait(context.CancellationToken);
            context.ReportDiagnostic(Diagnostic.Create(s_generatorFinished, default));

            async Task ExecuteAsync()
            {
                using (var part16 = new Part16(_httpClient, context.CancellationToken))
                using (var part06 = new Part06(_httpClient, context.CancellationToken))
                {
                    bool generate = true;
                    if (!await part16.IsUpToDateAsync().ConfigureAwait(false))
                    {
                        generate = false;
                        context.ReportDiagnostic(Diagnostic.Create(s_resourceOutdated, default, Part16.ResourceKey, Part16.Uri));
                    }
                    if (!await part06.IsUpToDateAsync().ConfigureAwait(false))
                    {
                        generate = false;
                        context.ReportDiagnostic(Diagnostic.Create(s_resourceOutdated, default, Part06.ResourceKey, Part06.Uri));
                    }
                    if (generate)
                    {
                        Dictionary<int, string> cidTable = await part16.GetSectionsByIdAsync().ConfigureAwait(false);
                        if (cidTable.Count > 0)
                        {
                            (Uid[]? tableA1, Uid[]? tableA3) = await part06.GetTablesAsync(cidTable).ConfigureAwait(false);
                            if (tableA1 != null && tableA3 != null)
                            {
                                var hashSet = new StringBuilder();
                                hashSet.Append(
"using System.Collections.Generic;\r\n" +
"namespace DiCor\r\n" +
"{\r\n" +
"    public partial struct Uid\r\n" +
"    {\r\n" +
"        private const string _part16Title = \"").Append(part16.Title).Append("\";\r\n" +
"        private const string _part06Title = \"").Append(part06.Title).Append("\";\r\n" +
"        private static readonly HashSet<Uid> s_uids = new HashSet<Uid>()\r\n" +
"        {\r\n");
                                var uids = new StringBuilder();
                                uids.Append(
"namespace DiCor\r\n" +
"{\r\n" +
"    public partial struct Uid\r\n" +
"    {\r\n");

                                foreach (Uid uid in tableA1.Concat(tableA3))
                                {
                                    StorageCategory storageCategory = Uid.GetStorageCategory(uid);
                                    string category = storageCategory != StorageCategory.None ? $", storageCategory: StorageCategory.{storageCategory}" : "";
                                    string isRetired = uid.IsRetired ? ", isRetired: true" : "";
                                    string symbol = ToSymbol(uid);

                                    hashSet.Append(
"            Uid.").Append(symbol).Append(",\r\n");

                                    uids.Append(
"        public static readonly Uid ").Append(symbol).Append(" = new Uid(\"").Append(uid.Value).Append("\", \"").Append(uid.Name).Append("\", UidType.").Append(uid.Type).Append(category).Append(isRetired).Append(");\r\n");
                                }

                                hashSet.Append(
"        };\r\n" +
"    }\r\n" +
"}\r\n");
                                uids.Append(
"    }\r\n" +
"}\r\n");
                                context.AddSource("Uid.HashSet.g.cs", SourceText.From(hashSet.ToString(), Encoding.UTF8));
                                context.AddSource("Uid.Uids.g.cs", SourceText.From(uids.ToString(), Encoding.UTF8));
                            }
                        }
                    }
                }
            }
        }

        private static string ToSymbol(Uid uid, bool useValue = false)
        {
            ReadOnlySpan<char> retired = "(Retired)".AsSpan();
            ReadOnlySpan<char> process = "(Process ".AsSpan();

            // Additional 9 chars for appending "_RETIRED" and prepending "_" if needed
            Span<char> symbol = stackalloc char[(useValue ? uid.Value : uid.Name).Length + 8 + 1];
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
                symbol.Slice(0, writeAt++).CopyTo(symbol.Slice(1));
                symbol[0] = '_';
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
