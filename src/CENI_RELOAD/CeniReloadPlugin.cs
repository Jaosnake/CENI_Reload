using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Jaosnake.CeniReload.Localization;

namespace Jaosnake.CeniReload
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class CeniReloadPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.Jaosnake.CENI_RELOAD";
        public const string PluginName = "CENI_RELOAD";
        public const string PluginVersion = "1.0.0";

        public static ManualLogSource Log { get; private set; }
        public static ConfigEntry<bool> AutoSaveEnabled { get; private set; }
        public static ConfigEntry<bool> ShowChatNotifications { get; private set; }

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;

            AutoSaveEnabled = Config.Bind(
                "Checkpoints",
                "AutoSaveOnStageStart",
                true,
                "Automatically record a checkpoint at the start of every gameplay stage."
            );

            ShowChatNotifications = Config.Bind(
                "Notifications",
                "ShowChatMessages",
                true,
                "Show a chat notification when CENI_RELOAD saves or restores a checkpoint."
            );

            string pluginDir = Path.GetDirectoryName(Info.Location);
            CeniReloadStrings.LoadExternalOverrides(pluginDir);

            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll(typeof(CeniReloadPlugin).Assembly);

            Log.LogInfo($"{PluginName} v{PluginVersion} initialized (100% original standalone architecture).");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}
