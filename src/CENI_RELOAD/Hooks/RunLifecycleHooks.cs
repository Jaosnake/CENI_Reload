using System.Collections;
using System.Reflection;
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
        private static Run _initializingRestoredRun;
        private static readonly MethodInfo GenerateLoopRng = typeof(Run).GetMethod("GenerateLoopRNG", BindingFlags.Instance | BindingFlags.NonPublic);

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PreGameController), "Start")]
        private static void OnPreGameStartPostfix(PreGameController __instance)
        {
            if (SnapshotRestore.PendingSnapshot != null && __instance != null)
            {
                __instance.StartCoroutine(SnapshotRestore.TryAutoLaunchFromPreGame(__instance));
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PreGameController), "StartLaunch")]
        private static bool OnStartLaunchPrefix(PreGameController __instance)
        {
            return !NetworkServer.active || SnapshotRestore.BeforeLaunch(__instance);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Run), "Start")]
        private static void OnRunStartPrefix(Run __instance)
        {
            if (NetworkServer.active && SnapshotRestore.IsRestoringRun)
            {
                _initializingRestoredRun = __instance;
                SnapshotRestore.ApplyToRunStart(__instance);
            }
            else if (NetworkServer.active) SnapshotRestore.ClearPending();
        }

        [HarmonyFinalizer]
        [HarmonyPatch(typeof(Run), "Start")]
        private static void OnRunStartFinalizer(Run __instance)
        {
            if (ReferenceEquals(_initializingRestoredRun, __instance)) _initializingRestoredRun = null;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Run), "OnLoopBeginServer")]
        private static bool OnLoopBeginPrefix(Run __instance)
        {
            if (!ReferenceEquals(_initializingRestoredRun, __instance)) return true;
            // A reload resumes an existing loop. Initialize its generator without
            // incrementing counters, clearing flags or replaying new-loop rewards.
            GenerateLoopRng.Invoke(__instance, null);
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Run), "GenerateStageRNG")]
        private static void OnGenerateStageRngPrefix(Run __instance)
        {
            if (NetworkServer.active) StageRandomState.BeforeGenerate(__instance, SnapshotRestore.PendingSnapshot);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Run), "PickNextStageScene")]
        private static bool OnPickNextStageScenePrefix(Run __instance)
        {
            if (NetworkServer.active && SnapshotRestore.IsRestoringRun && !SnapshotRestore.StageSceneApplied)
            {
                if (SnapshotRestore.TryOverrideInitialStageScene(__instance))
                {
                    return false;
                }
            }
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Run), "SetupUserCharacterMaster")]
        private static void OnSetupUserCharacterMasterPostfix(NetworkUser user)
        {
            if (NetworkServer.active && SnapshotRestore.CanRestorePlayers && user != null && user.master != null)
            {
                SnapshotRestore.ApplyToPlayerMaster(user, user.master);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Run), "ShouldAllowNewParticipant")]
        private static bool OnShouldAllowNewParticipantPrefix(ref bool __result)
        {
            // The base game's DropIn rule can otherwise reject late users before
            // a master exists. Enable this only for a resumed CENI run.
            if (!NetworkServer.active || !SnapshotRestore.CanRestorePlayers) return true;
            __result = true;
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Stage), "Start")]
        private static void OnStageStartPostfix(Stage __instance)
        {
            if (!NetworkServer.active || __instance == null) return;

            if (SnapshotRestore.IsRestoringRun)
            {
                SnapshotRestore.MarkRestoreStageStarted();
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

            // Never auto-save initial Stage 1 (stageClearCount == 0 && loopClearCount == 0):
            // players often start Stage 1 to test survivors, mods, or artifacts.
            // On Loop 1+ (Stage 6+), both stageClearCount (>= 5) and loopClearCount (>= 1) stay > 0, so loops ALWAYS save.
            if (Run.instance.stageClearCount < 1 && Run.instance.loopClearCount < 1) yield break;

            var sceneDef = SceneCatalog.GetSceneDefForCurrentScene();
            if (sceneDef == null || sceneDef.sceneType != SceneType.Stage) yield break;

            var snapshot = SnapshotCapture.CaptureCurrentRun();
            if (snapshot == null) yield break;

            var saveResult = VaultRepository.SaveSnapshotWithStatus(snapshot);
            if (CeniReloadPlugin.ShowChatNotifications != null && CeniReloadPlugin.ShowChatNotifications.Value)
            {
                string stageName = CeniReloadStrings.LocalizeGameToken(snapshot.stageDisplayToken, snapshot.stageSceneName);
                if (saveResult == SaveSlotResult.Saved)
                {
                    Chat.AddMessage(CeniReloadStrings.Format("CENIRELOAD_CHAT_SAVED", snapshot.slotIndex, stageName, snapshot.stageClearCount + 1));
                }
                else if (saveResult == SaveSlotResult.AllSlotsLocked)
                {
                    string bucketDisplay = snapshot.IsMultiplayer()
                        ? "Multiplayer"
                        : CeniReloadStrings.LocalizeGameToken(snapshot.survivorDisplayToken, snapshot.survivorBodyName);
                    Chat.AddMessage(CeniReloadStrings.Format("CENIRELOAD_CHAT_ALL_LOCKED", bucketDisplay));
                }
                else
                {
                    Chat.AddMessage(CeniReloadStrings.Format("CENIRELOAD_CHAT_SAVE_ERROR", snapshot.stageClearCount + 1));
                }
            }
        }
    }
}
