using System.Collections;
using HarmonyLib;
using Jaosnake.CeniReload.Core;
using Jaosnake.CeniReload.Localization;
using Jaosnake.CeniReload.Storage;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace Jaosnake.CeniReload.Hooks
{
    [HarmonyPatch]
    public static class RunLifecycleHooks
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PreGameController), "Start")]
        private static void OnPreGameStartPostfix(PreGameController __instance)
        {
            if (SnapshotRestore.AutoLaunchOnPreGame && __instance != null)
            {
                __instance.StartCoroutine(SnapshotRestore.TryAutoLaunchFromPreGame(__instance));
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Run), "Start")]
        private static void OnRunStartPrefix(Run __instance)
        {
            if (NetworkServer.active && SnapshotRestore.IsRestoringRun)
            {
                SnapshotRestore.ApplyToRunStart(__instance);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Run), "SetupUserCharacterMaster")]
        private static void OnSetupUserCharacterMasterPostfix(NetworkUser user)
        {
            if (NetworkServer.active && SnapshotRestore.IsRestoringRun && user != null && user.master != null)
            {
                SnapshotRestore.ApplyToPlayerMaster(user.master);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Stage), "Start")]
        private static void OnStageStartPostfix(Stage __instance)
        {
            if (!NetworkServer.active || __instance == null) return;

            if (SnapshotRestore.IsRestoringRun)
            {
                __instance.StartCoroutine(SnapshotRestore.FinishRestoreOnStageStart(__instance));
                return;
            }

            if (CeniReloadPlugin.AutoSaveEnabled != null && CeniReloadPlugin.AutoSaveEnabled.Value)
            {
                __instance.StartCoroutine(AutoSaveRoutine(__instance));
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Run), "OnClientGameOver")]
        private static void OnClientGameOverPostfix()
        {
            SnapshotRestore.ClearPending();
        }

        private static IEnumerator AutoSaveRoutine(Stage stage)
        {
            yield return new WaitForSeconds(0.8f);

            if (stage == null || Run.instance == null || !NetworkServer.active) yield break;

            var sceneDef = SceneCatalog.GetSceneDefForCurrentScene();
            if (sceneDef == null || sceneDef.sceneType != SceneType.Stage) yield break;

            var snapshot = SnapshotCapture.CaptureCurrentRun();
            if (snapshot == null) yield break;

            bool saved = VaultRepository.SaveSnapshot(snapshot);
            if (CeniReloadPlugin.ShowChatNotifications != null && CeniReloadPlugin.ShowChatNotifications.Value)
            {
                string stageName = CeniReloadStrings.LocalizeGameToken(snapshot.stageDisplayToken, snapshot.stageSceneName);
                if (saved)
                {
                    Chat.AddMessage(CeniReloadStrings.Format("CENIRELOAD_CHAT_SAVED", snapshot.slotIndex, stageName, snapshot.stageClearCount + 1));
                }
                else
                {
                    Chat.AddMessage(CeniReloadStrings.Format("CENIRELOAD_CHAT_SAVE_ERROR", snapshot.stageClearCount + 1));
                }
            }
        }
    }
}
