using System;
using System.Globalization;
using System.IO;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DiCor.Generator
{
    public record Settings
    {
        private readonly object _lock = new();
        private readonly string _docBookDirectory;
        private readonly bool _shouldCheckUpdates;

        private bool? _didCheckUpdates;

        public string DocBookDirectory => _docBookDirectory;

        public bool ShouldCheckUpdates => _shouldCheckUpdates;

        private string StateFilePath => Path.Combine(DocBookDirectory, "state");

        public Settings(AnalyzerConfigOptionsProvider options)
        {
            options.GlobalOptions.TryGetValue("build_property.projectdir", out string? projectDir);
            options.GlobalOptions.TryGetValue("build_property.dicorgenerator_logfilepath", out string? logFilePath);
            options.GlobalOptions.TryGetValue("build_property.dicorgenerator_docbookdirectory", out string? docBookDirectory);
            options.GlobalOptions.TryGetValue("build_property.dicorgenerator_checkforupdates", out string? checkForUpdates);

            Logger.Initialize(logFilePath);

            Logger.Log($"build_property.projectdir: {projectDir ?? "null"}");
            Logger.Log($"build_property.dicorgenerator_logfilepath: {logFilePath ?? "null"}");
            Logger.Log($"build_property.dicorgenerator_docbookdirectory: {docBookDirectory ?? "null"}");
            Logger.Log($"build_property.dicorgenerator_checkforupdates: {checkForUpdates ?? "null"}");

            _docBookDirectory = Path.Combine(
                projectDir ?? throw new InvalidOperationException("Property 'ProjectDir' not found."),
                docBookDirectory ?? throw new InvalidOperationException("Property 'DocBookDirectory' not found."));
            _shouldCheckUpdates = "true".Equals(checkForUpdates, StringComparison.OrdinalIgnoreCase);

            if (_shouldCheckUpdates)
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

        public void DidCheckUpdates(bool didCheck)
        {
            lock (_lock)
            {
                _didCheckUpdates = (_didCheckUpdates ?? true) && didCheck;
            }
        }

        public void SaveState()
        {
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
    }
}
