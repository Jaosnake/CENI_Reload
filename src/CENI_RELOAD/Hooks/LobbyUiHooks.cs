using HarmonyLib;
using Jaosnake.CeniReload.UI;
using RoR2.UI;

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
            CeniReloadVaultController.AttachTo(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CharacterSelectController), "OnEnable")]
        private static void OnCharacterSelectEnable(CharacterSelectController __instance)
        {
            if (__instance == null) return;
            var vault = CeniReloadVaultController.AttachTo(__instance);
            vault?.RefreshTriggerLabel();
        }
    }
}
