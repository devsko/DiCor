using System;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DiCor.Generator
{
    public static class AnalyzerConfigOptionsProviderExtensions
    {
        public static string? GetStringProperty(this AnalyzerConfigOptionsProvider options, string propertyName)
        {
            options.GlobalOptions.TryGetValue($"build_property.{propertyName}", out string? propertyValue);

            return string.IsNullOrEmpty(propertyValue) ? null : propertyValue;
        }

        public static bool GetBoolProperty(this AnalyzerConfigOptionsProvider options, string propertyName)
            => options.GlobalOptions.TryGetValue($"build_property.{propertyName}", out string? propertyValue) &&
                string.Equals(propertyValue, bool.TrueString, StringComparison.OrdinalIgnoreCase);
    }
}
