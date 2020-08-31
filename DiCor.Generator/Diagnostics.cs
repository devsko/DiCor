using System;

using Microsoft.CodeAnalysis;

namespace DiCor.Generator
{
    public static class Diagnostics
    {
        private const string DiagnosticCategory = "DiCor.Generator";

        private static readonly DiagnosticDescriptor s_resourceOutdated = new DiagnosticDescriptor(
            "GEN002",
            "Resource outdated",
            "Downloading resource '{0}' from {1}",
            DiagnosticCategory,
            DiagnosticSeverity.Warning,
            true);

        private static readonly DiagnosticDescriptor s_invalidResourceXml = new DiagnosticDescriptor(
            "GEN003",
            "InvalidResourceXml",
            "Invalid XML in resource '{0}': {1}",
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

        public static Diagnostic ResourceOutdated(string resourceKey, string downloadUri)
            => Diagnostic.Create(s_resourceOutdated, default, resourceKey, downloadUri);

        public static Diagnostic InvalidXml(string resourceKey, object error)
            => Diagnostic.Create(s_invalidResourceXml, default, resourceKey, error);

        public static Diagnostic UnexpectedException(Exception ex)
        {
            if (ex is AggregateException aex)
            {
                aex = aex.Flatten();
                if (aex.InnerExceptions.Count == 1)
                    ex = aex.InnerException!;
                else
                    ex = aex;
            }

            return Diagnostic.Create(s_exception, default, ex);
        }
    }
}
