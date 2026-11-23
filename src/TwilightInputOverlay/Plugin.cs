using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using Il2CppInterop.Runtime.Injection;

namespace TwilightInputOverlay
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency(TimerSettingsIntegration.TimerGuid, BepInDependency.DependencyFlags.SoftDependency)]
    public sealed class Plugin : BasePlugin
    {
        internal static ManualLogSource Logger;

        internal static bool IntegratedSettings { get; private set; }

        public override void Load()
        {
            Logger = Log;
            PersistenceService.InitialiseDataRoot();
            Logger.LogInfo($"Loading {PluginInfo.PLUGIN_NAME} v{PluginInfo.PLUGIN_VERSION}. Settings: {PersistenceService.PluginDir}");

            var config = new ConfigService();
            ConfigService.Init(config);
            config.EnsureDefaultLangFiles();
            config.Load();
            PresetStore.EnsureInitialized(config);

            ClassInjector.RegisterTypeInIl2Cpp<InputHud>();
            ClassInjector.RegisterTypeInIl2Cpp<SettingsPanel>();
            ClassInjector.RegisterTypeInIl2Cpp<MouseCursorOverlay>();
            ClassInjector.RegisterTypeInIl2Cpp<RainingKeysOverlay>();
            AddComponent<InputHud>();
            AddComponent<MouseCursorOverlay>();
            AddComponent<RainingKeysOverlay>();
            IntegratedSettings = TimerSettingsIntegration.TryRegister(config);
            Logger.LogInfo(IntegratedSettings ? "Settings: integrated into HSRTimer." : "Settings: standalone window.");
            AddComponent<SettingsPanel>();
        }
    }
}
