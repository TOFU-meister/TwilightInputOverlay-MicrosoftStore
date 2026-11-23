using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using Il2CppInterop.Runtime.Injection;

namespace TwilightInputOverlay
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public sealed class Plugin : BasePlugin
    {
        internal static ManualLogSource Logger;

        public override void Load()
        {
            Logger = Log;
            Logger.LogInfo($"Loading {PluginInfo.PLUGIN_NAME} v{PluginInfo.PLUGIN_VERSION}. Settings: {PersistenceService.PluginDir}");

            var config = new ConfigService();
            ConfigService.Init(config);
            config.EnsureDefaultLangFiles();
            config.Load();
            PresetStore.EnsureInitialized(config);

            ClassInjector.RegisterTypeInIl2Cpp<InputHud>();
            ClassInjector.RegisterTypeInIl2Cpp<SettingsPanel>();
            AddComponent<InputHud>();
            AddComponent<SettingsPanel>();
        }
    }
}
