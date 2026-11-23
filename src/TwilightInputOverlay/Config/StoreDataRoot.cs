using System;
using System.IO;
using System.Collections.Generic;

namespace TwilightInputOverlay
{
    internal static class StoreDataRoot
    {
        private const string DirectoryName = "TwilightInputOverlay-MicrosoftStore";
        private const string MigrationMarker = ".localappdata-imported";

        internal static string Select(string documents, string localAppData, Action<string> warn)
        {
            string legacy = Path.IsPathFullyQualified(localAppData ?? "")
                ? Path.Combine(localAppData, DirectoryName) : null;
            try
            {
                if (!Path.IsPathFullyQualified(documents ?? "") || !Directory.Exists(documents))
                    throw new DirectoryNotFoundException("Windows Documents could not be found.");

                string preferred = Path.Combine(documents, DirectoryName);
                VerifyWritable(preferred);
                if (legacy != null && !string.Equals(preferred, legacy, StringComparison.OrdinalIgnoreCase)
                    && !File.Exists(Path.Combine(preferred, MigrationMarker)))
                {
                    // Keep track of our additions so a failed import can be retried
                    // without shadowing newer writes made in the fallback directory.
                    var added = new List<string>();
                    try
                    {
                        if (Directory.Exists(legacy))
                            Import(legacy, preferred, added);
                        using var marker = new FileStream(Path.Combine(preferred, MigrationMarker), FileMode.OpenOrCreate);
                    }
                    catch (Exception error) when (IsAccessFailure(error))
                    {
                        foreach (var path in added)
                        {
                            try { File.Delete(path); }
                            catch (Exception cleanup) when (IsAccessFailure(cleanup))
                            {
                                warn($"Could not roll back copied settings at '{path}': {cleanup.Message}");
                            }
                        }
                        throw;
                    }
                }
                return preferred;
            }
            catch (Exception error) when (IsAccessFailure(error))
            {
                warn($"Documents settings unavailable: {error.Message}. Trying LocalAppData.");
                if (legacy == null)
                    throw new IOException("LocalAppData could not be found.", error);
                VerifyWritable(legacy);
                return legacy;
            }
        }

        private static bool IsAccessFailure(Exception error)
            => error is IOException or UnauthorizedAccessException or System.Security.SecurityException;

        private static void VerifyWritable(string directory)
        {
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, ".write-test-" + Guid.NewGuid().ToString("N"));
            using var probe = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                4096, FileOptions.DeleteOnClose);
            probe.WriteByte(0);
            probe.Flush(flushToDisk: true);
        }

        private static void Import(string source, string destination, List<string> added)
        {
            string settings = Path.Combine(source, "settings.ini");
            if (File.Exists(settings))
                CopyMissing(settings, Path.Combine(destination, "settings.ini"), added);

            foreach (string name in new[] { "presets", "lang" })
            {
                string folder = Path.Combine(source, name);
                if (!Directory.Exists(folder)) continue;
                var options = new EnumerationOptions
                {
                    RecurseSubdirectories = true,
                    AttributesToSkip = FileAttributes.ReparsePoint,
                };
                foreach (string file in Directory.EnumerateFiles(folder, "*", options))
                    CopyMissing(file, Path.Combine(destination, name, Path.GetRelativePath(folder, file)), added);
            }
        }

        private static void CopyMissing(string source, string destination, List<string> added)
        {
            if (File.Exists(destination)) return;
            string parent = Path.GetDirectoryName(destination);
            Directory.CreateDirectory(parent);
            string temporary = Path.Combine(parent, ".import-" + Guid.NewGuid().ToString("N"));
            try
            {
                File.Copy(source, temporary);
                try
                {
                    File.Move(temporary, destination);
                    added.Add(destination);
                }
                catch (IOException) when (File.Exists(destination))
                {
                    // A destination created during migration takes precedence.
                }
            }
            finally
            {
                File.Delete(temporary);
            }
        }
    }
}
