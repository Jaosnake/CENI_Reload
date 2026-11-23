using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using Jaosnake.CeniReload.Localization;
using Jaosnake.CeniReload.Model;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace Jaosnake.CeniReload.Core
{
    public static class SnapshotRestore
    {
        public static CheckpointSnapshot PendingSnapshot { get; private set; }
        public static bool IsRestoringRun => PendingSnapshot != null;
        private static int _restoredPlayerCursor;

        public static void ClearPending()
        {
            PendingSnapshot = null;
            _restoredPlayerCursor = 0;
        }

        public static bool BeginLoadFromLobby(CheckpointSnapshot snapshot)
        {
            if (snapshot == null) return false;

            var preGame = PreGameController.instance;
            if (preGame == null)
            {
                CeniReloadPlugin.Log?.LogWarning("Cannot load checkpoint: PreGameController.instance is null.");
                return false;
            }

            try
            {
                PendingSnapshot = snapshot;
                _restoredPlayerCursor = 0;

                if (!string.IsNullOrEmpty(snapshot.gameModeName))
                {
                    var gmIdx = GameModeCatalog.FindGameModeIndex(snapshot.gameModeName);
                    if (gmIdx != GameModeIndex.Invalid)
                    {
                        preGame.gameModeIndex = gmIdx;
                    }
                }

                var restoredRules = DeserializeRuleBook(snapshot.ruleBookBase64);
                if (restoredRules != null)
                {
                    var setRuleBookMethod = AccessTools.Method(typeof(PreGameController), "SetRuleBook");
                    setRuleBookMethod?.Invoke(preGame, new object[] { restoredRules });
                }

                CeniReloadPlugin.Log?.LogInfo($"Launching run from checkpoint Slot {snapshot.slotIndex} ({snapshot.survivorBodyName}, Stage {snapshot.stageClearCount + 1}: {snapshot.stageSceneName})");
                preGame.StartLaunch();
                return true;
            }
            catch (Exception ex)
            {
                CeniReloadPlugin.Log?.LogError($"Failed to launch checkpoint from lobby: {ex}");
                ClearPending();
                return false;
            }
        }

        public static void ApplyToRunStart(Run run)
        {
            var snap = PendingSnapshot;
            if (snap == null || run == null) return;

            try
            {
                run.seed = snap.seed;
                run.NetworkstageClearCount = snap.stageClearCount;
                run.shopPortalCount = snap.shopPortalCount;
                run.NetworkfixedTime = snap.runFixedTime;
                run.SetRunStopwatch(snap.runFixedTime);

                var restoredRules = DeserializeRuleBook(snap.ruleBookBase64);
                if (restoredRules != null)
                {
                    run.SetRuleBook(restoredRules);
                }

                var targetScene = SceneCatalog.GetSceneDefFromSceneName(snap.stageSceneName);
                if (targetScene != null)
                {
                    run.nextStageScene = targetScene;
                }
            }
            catch (Exception ex)
            {
                CeniReloadPlugin.Log?.LogError($"Error applying checkpoint to Run.Start: {ex}");
            }
        }

        public static void ApplyToPlayerMaster(CharacterMaster master)
        {
            var snap = PendingSnapshot;
            if (snap == null || master == null || snap.players == null || snap.players.Count == 0) return;

            int idx = _restoredPlayerCursor < snap.players.Count ? _restoredPlayerCursor : 0;
            _restoredPlayerCursor++;
            var playerSnap = snap.players[idx];
            if (playerSnap == null) return;

            try
            {
                if (!string.IsNullOrEmpty(playerSnap.bodyName))
                {
                    var bodyPrefab = BodyCatalog.FindBodyPrefab(playerSnap.bodyName);
                    if (bodyPrefab != null)
                    {
                        master.bodyPrefab = bodyPrefab;
                    }
                }

                var loadout = DeserializeLoadout(playerSnap.loadoutBase64);
                if (loadout != null)
                {
                    master.SetLoadoutServer(loadout);
                }

                master.money = playerSnap.money;
                master.voidCoins = playerSnap.voidCoins;
                master.luck = playerSnap.luck;

                if (master.inventory != null)
                {
                    RestoreInventory(master.inventory, playerSnap.items, playerSnap.equipments, playerSnap.infusionBonus,
                        (byte)Mathf.Clamp(playerSnap.activeEquipmentSlot, 0, 255),
                        (byte)Mathf.Clamp(playerSnap.activeEquipmentSet, 0, 255));
                }
            }
            catch (Exception ex)
            {
                CeniReloadPlugin.Log?.LogError($"Error applying checkpoint to CharacterMaster: {ex}");
            }
        }

        public static IEnumerator FinishRestoreOnStageStart(Stage stage)
        {
            var snap = PendingSnapshot;
            if (snap == null) yield break;

            yield return new WaitForSeconds(0.5f);

            try
            {
                if (TeamManager.instance != null && snap.teamExperience > 0)
                {
                    ulong currentXp = TeamManager.instance.GetTeamExperience(TeamIndex.Player);
                    if (snap.teamExperience > currentXp)
                    {
                        TeamManager.instance.GiveTeamExperience(TeamIndex.Player, snap.teamExperience - currentXp);
                    }
                }

                if (Run.instance != null)
                {
                    Run.instance.SetRunStopwatch(snap.runFixedTime);
                }

                var players = PlayerCharacterMasterController.instances;
                for (int i = 0; i < players.Count && i < snap.players.Count; i++)
                {
                    var pcmc = players[i];
                    var pSnap = snap.players[i];
                    if (pcmc == null || pcmc.master == null || pSnap.minions == null) continue;

                    var bodyObj = pcmc.master.GetBodyObject();
                    Vector3 basePos = bodyObj != null ? bodyObj.transform.position : Vector3.zero;
                    Quaternion baseRot = bodyObj != null ? bodyObj.transform.rotation : Quaternion.identity;

                    for (int m = 0; m < pSnap.minions.Count; m++)
                    {
                        var minionSnap = pSnap.minions[m];
                        var masterPrefab = MasterCatalog.FindMasterPrefab(minionSnap.masterName);
                        if (masterPrefab == null) continue;

                        Vector3 offset = new Vector3(
                            Mathf.Cos(m * 1.1f) * 3f,
                            1.5f,
                            Mathf.Sin(m * 1.1f) * 3f
                        );

                        var summon = new MasterSummon
                        {
                            masterPrefab = masterPrefab,
                            position = basePos + offset,
                            rotation = baseRot,
                            summonerBodyObject = bodyObj,
                            ignoreTeamMemberLimit = true,
                            useAmbientLevel = true
                        };

                        var spawnedMaster = summon.Perform();
                        if (spawnedMaster != null && spawnedMaster.inventory != null)
                        {
                            RestoreInventory(spawnedMaster.inventory, minionSnap.items, minionSnap.equipments, 0, 0, 0);
                        }
                    }
                }

                string stageName = CeniReloadStrings.LocalizeGameToken(snap.stageDisplayToken, snap.stageSceneName);
                Chat.AddMessage(CeniReloadStrings.Format("CENIRELOAD_CHAT_LOADED", snap.slotIndex, stageName, snap.stageClearCount + 1));
            }
            catch (Exception ex)
            {
                CeniReloadPlugin.Log?.LogError($"Error finalizing checkpoint restore on stage start: {ex}");
            }
            finally
            {
                ClearPending();
            }
        }

        private static void RestoreInventory(
            Inventory inventory,
            List<ItemEntry> items,
            List<EquipmentEntry> equipments,
            uint infusionBonus,
            byte activeSlot,
            byte activeSet)
        {
            if (inventory == null) return;

            if (items != null)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    var entry = items[i];
                    if (entry == null || entry.count <= 0 || string.IsNullOrEmpty(entry.itemName)) continue;

                    var itemIndex = ItemCatalog.FindItemIndex(entry.itemName);
                    if (itemIndex == ItemIndex.None) continue;

                    int existing = inventory.GetItemCountPermanent(itemIndex);
                    int delta = entry.count - existing;
                    if (delta > 0)
                    {
                        inventory.GiveItemPermanent(itemIndex, delta);
                    }
                }
            }

            if (infusionBonus > 0)
            {
                inventory.AddInfusionBonus(infusionBonus);
            }

            if (equipments != null)
            {
                for (int i = 0; i < equipments.Count; i++)
                {
                    var eq = equipments[i];
                    if (eq == null || string.IsNullOrEmpty(eq.equipmentName)) continue;

                    var eqIdx = EquipmentCatalog.FindEquipmentIndex(eq.equipmentName);
                    if (eqIdx == EquipmentIndex.None) continue;

                    var chargeFinish = eq.cooldownRemaining > 0.01f
                        ? Run.FixedTimeStamp.now + eq.cooldownRemaining
                        : Run.FixedTimeStamp.negativeInfinity;

                    var state = new EquipmentState(eqIdx, chargeFinish, eq.charges, false);
#pragma warning disable CS0618
                    inventory.SetEquipment(state, eq.slot, eq.set);
#pragma warning restore CS0618
                }
            }

            inventory.SetActiveEquipmentSlot(activeSlot);
            inventory.SetActiveEquipmentSet(activeSet);
        }

        private static RuleBook DeserializeRuleBook(string base64)
        {
            if (string.IsNullOrEmpty(base64)) return null;
            try
            {
                byte[] bytes = Convert.FromBase64String(base64);
                var reader = new NetworkReader(bytes);
                var rb = new RuleBook();
                rb.Deserialize(reader);
                return rb;
            }
            catch
            {
                return null;
            }
        }

        private static Loadout DeserializeLoadout(string base64)
        {
            if (string.IsNullOrEmpty(base64)) return null;
            try
            {
                byte[] bytes = Convert.FromBase64String(base64);
                var reader = new NetworkReader(bytes);
                var loadout = new Loadout();
                loadout.Deserialize(reader);
                return loadout;
            }
            catch
            {
                return null;
            }
        }
    }
}
