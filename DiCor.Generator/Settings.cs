using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DiCor.Generator
{
    public class Settings : IEquatable<Settings>
    {
        private readonly object _lock = new();
        private readonly string _docBookDirectory;
        private readonly bool _shouldCheckUpdates;

        private bool? _didCheckUpdates;
        private HashSet<string>? _uidIdentifiers;
        private HashSet<string>? _tagIdentifiers;

        public string DocBookDirectory => _docBookDirectory;

        public bool ShouldCheckUpdates => _shouldCheckUpdates;

        public bool GenerateUids => _uidIdentifiers is not null;

        public HashSet<string>? UidIdentifiers
        {
            get => _uidIdentifiers;
            set => _uidIdentifiers = value;
        }

        public bool GenerateTags => _tagIdentifiers is not null;

        public HashSet<string>? TagIdentifiers
        {
            get => _tagIdentifiers;
            set => _tagIdentifiers = value;
        }

        private string StateFilePath
            => Path.Combine(DocBookDirectory, "state");

        public Settings(AnalyzerConfigOptionsProvider options)
        {
            options.GlobalOptions.TryGetValue("build_property.projectdir", out string? projectDir);
            options.GlobalOptions.TryGetValue("build_property.dicorgenerator_docbookdirectory", out string? docBookDirectory);
            options.GlobalOptions.TryGetValue("build_property.dicorgenerator_checkforupdates", out string? checkForUpdates);

            Logger.Log($"build_property.projectdir: {projectDir ?? "null"}");
            Logger.Log($"build_property.dicorgenerator_docbookdirectory: {docBookDirectory ?? "null"}");
            Logger.Log($"build_property.dicorgenerator_checkforupdates: {checkForUpdates ?? "null"}");

            _docBookDirectory = Path.Combine(
                projectDir ?? throw new InvalidOperationException("Property 'ProjectDir' not found."),
                docBookDirectory ?? throw new InvalidOperationException("Property 'DocBookDirectory' not found."));

            if (_shouldCheckUpdates = "true".Equals(checkForUpdates, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    string state = File.ReadAllText(StateFilePath);
                    Logger.Log($"state : {state}");
                    if (DateTime.TryParse(state, null, DateTimeStyles.RoundtripKind, out DateTime lastUpdateCheck))
                    {
                        _shouldCheckUpdates = lastUpdateCheck.AddHours(1) < DateTime.UtcNow;
                    }
                }
                catch (IOException ex)
                {
                    Logger.Log("Ignore: ", ex);
                }
            }

            Logger.Log($"DocBookDirectory: {DocBookDirectory}");
            Logger.Log($"CheckForUpdates: {ShouldCheckUpdates}");
        }

        internal bool ShouldGenerateIdentifier(UidValues values)
        {
            return _uidIdentifiers is not null && (
                _uidIdentifiers.Contains(values.Value, StringComparer.Ordinal) ||
                _uidIdentifiers.Contains(values.Symbol, StringComparer.OrdinalIgnoreCase) ||
                _uidIdentifiers.Contains($"Type:{values.Type}", StringComparer.OrdinalIgnoreCase));
        }

        public void DidCheckUpdates(bool didCheck)
        {
            lock (_lock)
            {
                _didCheckUpdates = (_didCheckUpdates ?? true) && didCheck;
            }
        }

        public void SaveState()
        {
            _uidIdentifiers = null;
            _tagIdentifiers = null;

            if (_didCheckUpdates ?? false)
            {
                string state = DateTime.UtcNow.ToString("O");
                Logger.Log($"Save state {state}");
                File.WriteAllText(StateFilePath, state);
            }
            else
            {
                Logger.Log("No need to save state");
            }
        }

        public bool Equals(Settings other)
            => false;

        public override bool Equals(object obj)
            => throw new NotImplementedException();

        public override int GetHashCode()
            => throw new NotImplementedException();
    }
}
