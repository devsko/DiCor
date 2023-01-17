using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Threading;

namespace DiCor.Generator
{
    [Generator]
    internal class Generator : IIncrementalGenerator
    {
        internal static JoinableTaskFactory JoinableTaskFactory { get; } = new(new JoinableTaskContext());
        private static readonly HttpClient s_httpClient = new();

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            try
            {
                System.Net.ServicePointManager.DefaultConnectionLimit = 5;

                Logger.StartSession();
                Logger.Log("=======================================");
                Logger.Log($"DefaultConnectionLimit: {System.Net.ServicePointManager.DefaultConnectionLimit}");

                //IncrementalValuesProvider<ClassDeclarationSyntax> syntax = context.SyntaxProvider
                //    .CreateSyntaxProvider(
                //        (node, ct) =>
                //        {
                //            if (node is ClassDeclarationSyntax classDeclaration)
                //            {
                //                Logger.Log($"CreateSyntaxProvider({classDeclaration.Keyword} {classDeclaration.Identifier})");
                //                return true;
                //            }
                //            return false;
                //        },
                //        GetSemanticTargetForGeneration)
                //    //.ForAttributeWithMetadataName(
                //    //    "DiCor.CodeGenerationSourcesAttribute",
                //    //    (node, _) => node is ClassDeclarationSyntax,
                //    //    GetSemanticTargetForGeneration)
                //    .Where(classDeclaration =>
                //    {
                //        Logger.Log($"SyntaxProvider.ForAttributeWithMetadataName().Where(classDeclaration={classDeclaration?.FullSpan}");
                //        return classDeclaration is not null;
                //    });

                IncrementalValueProvider<Settings> settings = context.AnalyzerConfigOptionsProvider
                    .Select((context, _) => new Settings(context));

                context.RegisterSourceOutput(settings, static (context, settings) =>
                {
                    JoinableTaskFactory.Run(() => ExecuteAsync(context, settings), JoinableTaskCreationOptions.LongRunning);
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

                using (var part16 = new Part16(s_httpClient, context, settings))
                using (var part06 = new Part06(s_httpClient, context, settings))
                using (var part07 = new Part07(s_httpClient, context, settings))
                {
                    DocBookData data = new(part06, part07, part16);
                    await data.ExtractAsync().ConfigureAwait(false);

                    CreateCode(context, data);

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

        private static void CreateCode(SourceProductionContext context, DocBookData data)
        {
            string header = $"""
                            // Generated code
                            // {data.Part06.Title} ({data.Part06.Uri})
                            // {data.Part07.Title} ({data.Part07.Uri})
                            // {data.Part16.Title} ({data.Part16.Uri})

                            """ + Environment.NewLine;

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

            string uidsString = uids.ToString();
            string detailsString = details.ToString();

            context.AddSource("Uid.Uids.g.cs", SourceText.From(uidsString, Encoding.UTF8));
            context.AddSource("Uid.Details.g.cs", SourceText.From(detailsString, Encoding.UTF8));
        }
    }
}
