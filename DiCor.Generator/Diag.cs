using System;
using Microsoft.CodeAnalysis;

namespace DiCor.Generator
{
    public static class Diag
    {
        private const string DiagnosticCategory = "Usage";

        private static readonly DiagnosticDescriptor s_resourceChecked = new(
            "GEN001",
            "Resource checked",
            "Resource '{0}' checked for updates ({1}ms)",
            DiagnosticCategory,
            DiagnosticSeverity.Warning,
            true);

        private static readonly DiagnosticDescriptor s_resourceOutdated = new(
            "GEN002",
            "Resource outdated",
            "Downloading resource '{0}' from {1}",
            DiagnosticCategory,
            DiagnosticSeverity.Warning,
            true);

        private static readonly DiagnosticDescriptor s_invalidResourceXml = new(
            "GEN003",
            "InvalidResourceXml",
            "Invalid XML in resource '{0}': {1}",
            DiagnosticCategory,
            DiagnosticSeverity.Warning,
            true);

        private static readonly DiagnosticDescriptor s_exception = new(
            "GEN004",
            "UnexpectedException",
            "Unexpected error '{0}'",
            DiagnosticCategory,
            DiagnosticSeverity.Error,
            true);

        public static Diagnostic ResourceChecked(Location location, string resourceKey, long milliseconds)
            => Diagnostic.Create(s_resourceChecked, location, resourceKey, milliseconds);

        public static Diagnostic ResourceOutdated(Location location, string resourceKey, string downloadUri)
            => Diagnostic.Create(s_resourceOutdated, location, resourceKey, downloadUri);

        public static Diagnostic InvalidXml(string resourceKey, object error)
            => Diagnostic.Create(s_invalidResourceXml, default, resourceKey, error);

        public static Diagnostic UnexpectedException(Exception ex)
        {
            if (ex is AggregateException aex)
            {
                aex = aex.Flatten();
                ex = aex.InnerExceptions.Count == 1 ? aex.InnerException : aex;
            }

            return Diagnostic.Create(s_exception, default, ex.Message);
        }
    }
}
