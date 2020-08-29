#nullable enable

using System;
using System.Xml;

using Microsoft.CodeAnalysis;

namespace DiCor.Generator
{
    public static class Diagnostics
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
            "Downloading resource '{0}' from {1}",
            DiagnosticCategory,
            DiagnosticSeverity.Warning,
            true);

        private static readonly DiagnosticDescriptor s_xmlException = new DiagnosticDescriptor(
            "GEN003",
            "XmlException",
            "Invalid XML found in resource '{0}' {1}",
            DiagnosticCategory,
            DiagnosticSeverity.Warning,
            true);

        private static readonly DiagnosticDescriptor s_exception = new DiagnosticDescriptor(
            "GEN004",
            "UnexpectedException",
            "Unexpected error: '{0}'",
            DiagnosticCategory,
            DiagnosticSeverity.Error,
            true);

        public static Diagnostic GeneratorStart()
            => Diagnostic.Create(s_generatorStarted, default);
        public static Diagnostic GeneratorFinish()
            => Diagnostic.Create(s_generatorFinished, default);
        public static Diagnostic ResourceOutdated(string resourceKey, string downloadUri)
            => Diagnostic.Create(s_resourceOutdated, default, resourceKey, downloadUri);
        public static Diagnostic XmlException(string resourceKey, XmlException ex)
            => Diagnostic.Create(s_xmlException, default, resourceKey, ex.Message);
        public static Diagnostic UnexpectedException(Exception ex)
        {
            if (ex is AggregateException aex)
            {
                aex = aex.Flatten();
                if (aex.InnerExceptions.Count == 1)
                    ex = aex.InnerException;
                else
                    ex = aex;
            }

            return Diagnostic.Create(s_exception, default, ex);
        }
    }
}
