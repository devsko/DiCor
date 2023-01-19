using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Threading;

namespace DiCor.Generator
{
    [SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1035:Do not use APIs banned for analyzers", Justification = "<Pending>")]
    public static class Logger
    {
        private static int s_sessionCounter;
        private static readonly AsyncLocal<int> s_sessionId = new();
        private static readonly object s_lock = new();

        private static string? s_path;
        private static int s_processId;
        private static string s_name = null!;

        public static void Initialize(string? path)
        {
            s_path = string.IsNullOrEmpty(path) ? null : path;
            Process process = Process.GetCurrentProcess();
            s_processId = process.Id;
            s_name = (Assembly.GetEntryAssembly()?.FullName ?? process.ProcessName).PadRight(8).Substring(0, 8);
        }

        public static void StartSession()
        {
            s_sessionId.Value = Interlocked.Increment(ref s_sessionCounter);
        }

        private static readonly char[] s_splitChars = new[] { '\r' };
        private static readonly string s_indent = new(' ', 36);
        public static void Log(string? message, Exception? ex = null)
        {
            if (s_path is null) return;

            lock (s_lock)
            {
                using FileStream stream = File.Open(s_path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                using StreamWriter writer = new(stream);

                writer.WriteLine($"{DateTime.Now:HH:mm:ss.fff} {s_name} {s_processId,7} {s_sessionId.Value,5} {message}");
                if (ex is not null)
                {
                    foreach (string line in ex.ToString().Split(s_splitChars))
                    {
                        writer.Write(s_indent);
                        writer.WriteLine(line.Trim());
                    }
                }
            }
        }
    }
}
