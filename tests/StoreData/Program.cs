using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TwilightInputOverlay;

const string Plugin = "TwilightInputOverlay-MicrosoftStore";
string temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
string documents = Path.Combine(temp, "Documents");
string local = Path.Combine(temp, "Local");
string legacy = Path.Combine(local, Plugin);
string preferred = Path.Combine(documents, Plugin);
var warnings = new List<string>();

static void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

static void Put(string root, string file, string text)
{
    string path = Path.Combine(root, file);
    Directory.CreateDirectory(Path.GetDirectoryName(path));
    File.WriteAllText(path, text);
}

string projectPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
    "../../../../../src/TwilightInputOverlay/TwilightInputOverlay.csproj"));
var projectVersion = System.Xml.Linq.XDocument.Load(projectPath).Root.Element("PropertyGroup").Element("Version");
Check((string)projectVersion == PluginInfo.PLUGIN_VERSION, "Runtime and project versions agree");
Check(System.Text.RegularExpressions.Regex.IsMatch(PluginInfo.PLUGIN_VERSION,
    @"^0\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)-pre$"), "Store version format");

try
{
    Put(legacy, "settings.ini", "scale = 1.1234567");
    Put(legacy, "presets/Custom/settings.ini", "preset");
    Put(legacy, "lang/custom.txt", "translation");
    Put(preferred, "settings.ini", "existing");
    Check(StoreDataRoot.Select(documents, local, warnings.Add) == preferred, "Documents selected");
    Check(File.ReadAllText(Path.Combine(preferred, "settings.ini")) == "existing", "Existing settings win");
    Check(File.ReadAllText(Path.Combine(preferred, "lang/custom.txt")) == "translation", "Language copied");
    string preset = Path.Combine(preferred, "presets/Custom/settings.ini");
    Check(File.ReadAllText(preset) == "preset", "Preset copied");
    Check(File.ReadAllText(Path.Combine(legacy, "settings.ini")) == "scale = 1.1234567", "Source retained");
    File.Delete(preset);
    Check(StoreDataRoot.Select(documents, local, warnings.Add) == preferred && !File.Exists(preset),
        "Restart does not restore a deleted preset");

    Check(StoreDataRoot.Select(Path.Combine(temp, "missing"), local, warnings.Add) == legacy,
        "Missing Documents uses fallback");
    string blockedDocuments = Path.Combine(temp, "blocked");
    Put(blockedDocuments, Plugin, "file blocks directory creation");
    Check(StoreDataRoot.Select(blockedDocuments, local, warnings.Add) == legacy, "Unwritable root uses fallback");

    string retryDocuments = Path.Combine(temp, "retry");
    string retryRoot = Path.Combine(retryDocuments, Plugin);
    Put(retryRoot, "presets", "file blocks migration");
    Check(StoreDataRoot.Select(retryDocuments, local, warnings.Add) == legacy, "Migration failure uses fallback");
    Check(!File.Exists(Path.Combine(retryRoot, "settings.ini")), "Partial copy rolled back");
    File.Delete(Path.Combine(retryRoot, "presets"));
    Put(legacy, "settings.ini", "scale = 1.2345678");
    Check(StoreDataRoot.Select(retryDocuments, local, warnings.Add) == retryRoot
        && File.ReadAllText(Path.Combine(retryRoot, "settings.ini")) == "scale = 1.2345678",
        "Retry imports latest fallback settings");
    Check(warnings.Count == 3, "Each fallback logged");
    string atomicPath = Path.Combine(temp, "atomic", "settings.ini");
    Put(Path.GetDirectoryName(atomicPath), "settings.ini", "original");
    bool threw = false;
    try
    {
        PersistenceService.WriteAtomically(atomicPath, stream =>
        {
            stream.WriteByte(1);
            throw new IOException("Simulated interrupted write");
        });
    }
    catch (IOException) { threw = true; }
    Check(threw && File.ReadAllText(atomicPath) == "original", "Interrupted save keeps existing file");
    Check(!Directory.EnumerateFiles(Path.GetDirectoryName(atomicPath), "*.tmp-*").Any(), "Staging file cleaned");
    PersistenceService.WriteAtomically(atomicPath, stream => stream.WriteByte((byte)'a'));
    Check(File.ReadAllText(atomicPath) == "a", "Successful save replaces existing file");

    Console.WriteLine("17 Store data and version checks passed.");
}
finally
{
    Directory.Delete(temp, recursive: true);
}

var previousCulture = System.Globalization.CultureInfo.CurrentCulture;
try
{
    System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("fr-FR");
    Check(SettingPrecision.Display(-2.126f) == "-2,13", "display uses local decimal separator");
    foreach (string input in new[] { "-2,126", "-2.126", " -2,126 " })
        Check(SettingPrecision.TryParse(input, out float number) && number == -2.13f, "input normalizes " + input);
    foreach (string invalid in new[] { "", "-", ".", "NaN", "Infinity", "1e90", "3,4,5" })
        Check(!SettingPrecision.TryParse(invalid, out _), "reject invalid input " + invalid);
    float legacySetting = 6.520123f;
    Check(SettingPrecision.Serialize(ref legacySetting) == "6.52" && legacySetting == 6.52f,
        "serialization normalizes the in-memory value and writes invariant decimal");
    Check(SettingPrecision.Display(4f) == "4,00", "display shows two places");
    Check(SettingPrecision.Round(0.1f) == 0.1f && SettingPrecision.Round(24f) == 24f,
        "slider endpoints are representable");
}
finally
{
    System.Globalization.CultureInfo.CurrentCulture = previousCulture;
}
Console.WriteLine("Setting precision checks passed.");

namespace TwilightInputOverlay
{
    internal static class Plugin
    {
        internal static TestLogger Logger { get; } = new();
    }
    internal sealed class TestLogger
    {
        internal void LogWarning(string message) => Console.WriteLine(message);
    }
}
