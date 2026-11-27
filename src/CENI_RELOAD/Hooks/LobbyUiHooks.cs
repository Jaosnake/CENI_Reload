using HarmonyLib;
using Jaosnake.CeniReload.Core;
using Jaosnake.CeniReload.UI;
using RoR2.UI;
using RoR2.UI.MainMenu;

namespace Jaosnake.CeniReload.Hooks
{
    [HarmonyPatch]
    public static class LobbyUiHooks
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CharacterSelectController), "Awake")]
        private static void OnCharacterSelectAwake(CharacterSelectController __instance)
        {
            if (__instance == null) return;
            CeniReloadVaultController.AttachToLobby(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CharacterSelectController), "OnEnable")]
        private static void OnCharacterSelectEnable(CharacterSelectController __instance)
        {
            if (__instance == null) return;
            var vault = CeniReloadVaultController.AttachToLobby(__instance);
            vault?.RefreshTriggerLabel();
            vault?.OpenPreparedCheckpoint();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MainMenuController), "Start")]
        private static void OnMainMenuStart(MainMenuController __instance)
        {
            SnapshotRestore.ClearPending();
            if (__instance == null) return;
            CeniReloadVaultController.AttachToMainMenu(__instance);
        }
    }
}
