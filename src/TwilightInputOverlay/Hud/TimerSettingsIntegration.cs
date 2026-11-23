using System;
using System.Linq;
using System.Reflection;
using BepInEx.Unity.IL2CPP;

namespace TwilightInputOverlay
{
    internal static class TimerSettingsIntegration
    {
        internal const string TimerGuid = "HSRTimer";

        internal static bool TryRegister(ConfigService config)
        {
            var chainloader = IL2CPPChainloader.Instance;
            if (chainloader == null || !chainloader.Plugins.TryGetValue(TimerGuid, out var plugin)
                || plugin.Instance == null)
                return false;

            return TryRegister(config, plugin.Instance.GetType().Assembly);
        }

        internal static bool TryRegister(ConfigService config, Assembly timerAssembly)
        {
            try
            {
                var registryType = timerAssembly.GetType("HSRTimer.SettingsPanelTabRegistry");
                var register = registryType?.GetMethod("RegisterCallbacks", new[]
                {
                    typeof(string), typeof(Func<string>), typeof(Action), typeof(Action),
                    typeof(Action), typeof(Func<bool>), typeof(Func<string[]>), typeof(Action<string>),
                });
                if (register == null || register.ReturnType != typeof(bool) || register.IsStatic)
                {
                    Plugin.Logger.LogInfo("HSRTimer callback API unavailable; using standalone settings.");
                    return false;
                }

                var registry = registryType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                return registry != null && (bool)register.Invoke(registry, new object[]
                {
                    PluginInfo.PLUGIN_GUID,
                    (Func<string>)(() => config.Localization.Get("TAB_TITLE")),
                    (Action)(() => PanelContent.Draw(true)),
                    (Action)PanelContent.ResetTransientState,
                    (Action)config.SaveSettings,
                    (Func<bool>)(() => PanelContent.CapturesInput),
                    (Func<string[]>)(() => config.Localization.Languages.Select(lang => lang.Code).ToArray()),
                    (Action<string>)(code => config.Localization.SetLanguage(code)),
                });
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogWarning($"HSRTimer settings registration failed; using standalone settings: {ex}");
                return false;
            }
        }
    }
}
