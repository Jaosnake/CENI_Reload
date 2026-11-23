using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Jaosnake.CeniReload.Localization;
using Jaosnake.CeniReload.Model;
using RoR2;
using RoR2.Artifacts;
using UnityEngine;
using UnityEngine.Networking;

namespace Jaosnake.CeniReload.Core
{
    public static class SnapshotRestore
    {
        private static readonly FieldInfo SimulacrumPatternIdxField = AccessTools.Field(typeof(InfiniteTowerRun), "enemyItemPatternIndex");
        private static readonly FieldInfo SimulacrumEnemyInvField = AccessTools.Field(typeof(InfiniteTowerRun), "enemyInventory");
        private static readonly FieldInfo EvolutionInventoryField = AccessTools.Field(typeof(MonsterTeamGainsItemsArtifactManager), "monsterTeamInventory");
        private static readonly FieldInfo EvolutionIteratorField = AccessTools.Field(typeof(MonsterTeamGainsItemsArtifactManager), "currentItemIterator");
        private static readonly FieldInfo DevotionInstanceListField = AccessTools.Field(typeof(DevotionInventoryController), "InstanceList");
        private static readonly FieldInfo DevotionInventoryField = AccessTools.Field(typeof(DevotionInventoryController), "_devotionMinionInventory");
        public static CheckpointSnapshot PendingSnapshot { get; private set; }
        public static bool IsRestoringRun => PendingSnapshot != null;
        public static bool AutoLaunchOnPreGame { get; private set; }
        public static bool StageSceneApplied { get; private set; }
        private static int _restoredPlayerCursor;
        private static readonly HashSet<int> _matchedPlayerIndices = new HashSet<int>();
        private static readonly Dictionary<CharacterMaster, PlayerSnapshot> _matchedMasters = new Dictionary<CharacterMaster, PlayerSnapshot>();

        public static void ClearPending()
        {
            PendingSnapshot = null;
            AutoLaunchOnPreGame = false;
            StageSceneApplied = false;
            _restoredPlayerCursor = 0;
            _matchedPlayerIndices.Clear();
            _matchedMasters.Clear();
        }

        public static bool TryOverrideInitialStageScene(Run run)
        {
            if (run == null || PendingSnapshot == null || StageSceneApplied) return false;
            var targetScene = SceneCatalog.GetSceneDefFromSceneName(PendingSnapshot.stageSceneName);
            if (targetScene == null) return false;

            run.nextStageScene = targetScene;
            StageSceneApplied = true;
            CeniReloadPlugin.Log?.LogInfo($"Overriding initial run stage scene to '{targetScene.baseSceneName}' (Stage {PendingSnapshot.stageClearCount + 1}).");
            return true;
        }

        public static bool BeginLoadFromLobby(CheckpointSnapshot snapshot)
        {
            if (snapshot == null) return false;
            if (snapshot.HasMissingSurvivors())
            {
                CeniReloadPlugin.Log?.LogWarning($"Cannot load checkpoint Slot {snapshot.slotIndex}: missing survivor '{snapshot.GetFirstMissingSurvivorName()}'.");
                return false;
            }

            var preGame = PreGameController.instance;
            if (preGame == null)
            {
                CeniReloadPlugin.Log?.LogWarning("Cannot load checkpoint: PreGameController.instance is null.");
                return false;
            }

            try
            {
                PendingSnapshot = snapshot;
                AutoLaunchOnPreGame = false;
                StageSceneApplied = false;
                _restoredPlayerCursor = 0;
                _matchedPlayerIndices.Clear();
                _matchedMasters.Clear();

                ApplyPreGameSettings(preGame, snapshot);

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

        public static bool BeginLoadFromMainMenu(CheckpointSnapshot snapshot)
        {
            if (snapshot == null) return false;
            if (snapshot.HasMissingSurvivors())
            {
                CeniReloadPlugin.Log?.LogWarning($"Cannot load checkpoint Slot {snapshot.slotIndex}: missing survivor '{snapshot.GetFirstMissingSurvivorName()}'.");
                return false;
            }

            if (RoR2.Console.instance == null)
            {
                CeniReloadPlugin.Log?.LogWarning("Cannot load checkpoint from main menu: RoR2.Console.instance is null.");
                ClearPending();
                return false;
            }

            try
            {
                PendingSnapshot = snapshot;
                AutoLaunchOnPreGame = true;
                StageSceneApplied = false;
                _restoredPlayerCursor = 0;
                _matchedPlayerIndices.Clear();
                _matchedMasters.Clear();

                string gm = "ClassicRun";
                if (!string.IsNullOrEmpty(snapshot.gameModeName) &&
                    GameModeCatalog.FindGameModeIndex(snapshot.gameModeName) != GameModeIndex.Invalid)
                {
                    gm = snapshot.gameModeName;
                }

                CeniReloadPlugin.Log?.LogInfo($"Transitioning from main menu to load checkpoint Slot {snapshot.slotIndex} ({snapshot.survivorBodyName}, Mode {gm}, Stage {snapshot.stageClearCount + 1}: {snapshot.stageSceneName})");
                RoR2.Console.instance.SubmitCmd((NetworkUser)null, $"transition_command \"gamemode {gm}; host 0;\"", false);
                return true;
            }
            catch (Exception ex)
            {
                CeniReloadPlugin.Log?.LogError($"Failed to launch checkpoint from main menu: {ex}");
                ClearPending();
                return false;
            }
        }

        public static IEnumerator TryAutoLaunchFromPreGame(PreGameController preGame)
        {
            yield return null;
            yield return null;

            if (!AutoLaunchOnPreGame || PendingSnapshot == null || preGame == null)
                yield break;

            AutoLaunchOnPreGame = false;

            try
            {
                var snapshot = PendingSnapshot;
                ApplyPreGameSettings(preGame, snapshot);
                CeniReloadPlugin.Log?.LogInfo($"Auto-launching run from PreGame for checkpoint Slot {snapshot.slotIndex} ({snapshot.survivorBodyName}, Stage {snapshot.stageClearCount + 1}: {snapshot.stageSceneName})");
                preGame.StartLaunch();
            }
            catch (Exception ex)
            {
                CeniReloadPlugin.Log?.LogError($"Failed to auto-launch checkpoint from PreGame: {ex}");
                ClearPending();
            }
        }

        private static void ApplyPreGameSettings(PreGameController preGame, CheckpointSnapshot snapshot)
        {
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
        }

        public static void ApplyToRunStart(Run run)
        {
            var snap = PendingSnapshot;
            if (snap == null || run == null) return;

            try
            {
                run.seed = snap.seed;
                run.NetworkstageClearCount = snap.stageClearCount;
                run.Network_loopClearCount = snap.loopClearCount;
                run.NetworkstageClearCountAtLoopStart = snap.stageClearCountAtLoopStart;
                run.shopPortalCount = snap.shopPortalCount;
                run.NetworkfixedTime = snap.runFixedTime;
                run.SetRunStopwatch(snap.runFixedTime);

                if (snap.eventFlags != null)
                {
                    for (int i = 0; i < snap.eventFlags.Count; i++)
                    {
                        string flag = snap.eventFlags[i];
                        if (!string.IsNullOrEmpty(flag))
                        {
                            run.SetEventFlag(flag);
                        }
                    }
                }

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

                if (run is InfiniteTowerRun towerRun && snap.simulacrumWaveIndex > 0)
                {
                    towerRun.Network_waveIndex = snap.simulacrumWaveIndex;
                    if (SimulacrumPatternIdxField != null)
                    {
                        SimulacrumPatternIdxField.SetValue(towerRun, snap.simulacrumEnemyItemPatternIndex);
                    }
                }
            }
            catch (Exception ex)
            {
                CeniReloadPlugin.Log?.LogError($"Error applying checkpoint to Run.Start: {ex}");
            }
        }

        private static PlayerSnapshot MatchPlayerSnapshotForUser(CheckpointSnapshot snap, NetworkUser user)
        {
            if (snap == null || snap.players == null || snap.players.Count == 0) return null;

            string userIdentity = SnapshotCapture.BuildPlayerIdentity(user);
            bool anySavedHasIdentity = false;

            for (int i = 0; i < snap.players.Count; i++)
            {
                var p = snap.players[i];
                if (p == null) continue;
                if (!string.IsNullOrEmpty(p.playerId))
                {
                    anySavedHasIdentity = true;
                    if (!_matchedPlayerIndices.Contains(i) &&
                        !string.IsNullOrEmpty(userIdentity) &&
                        string.Equals(p.playerId, userIdentity, StringComparison.OrdinalIgnoreCase))
                    {
                        _matchedPlayerIndices.Add(i);
                        return p;
                    }
                }
            }

            // In Multiplayer saves with recorded identities, a new player who wasn't in the original save starts fresh
            // (never steals an absent player's survivor, items, or drones).
            if (snap.IsMultiplayer() && anySavedHasIdentity)
            {
                string name = user != null ? user.userName : "Unknown";
                CeniReloadPlugin.Log?.LogInfo($"Multiplayer checkpoint Slot {snap.slotIndex}: player '{name}' was not in the saved run; starting fresh.");
                return null;
            }

            // Singleplayer (or legacy save without playerId): match sequentially
            while (_restoredPlayerCursor < snap.players.Count)
            {
                int idx = _restoredPlayerCursor++;
                if (_matchedPlayerIndices.Add(idx) && snap.players[idx] != null)
                {
                    return snap.players[idx];
                }
            }

            return null;
        }

        public static void ApplyToPlayerMaster(NetworkUser user, CharacterMaster master)
        {
            var snap = PendingSnapshot;
            if (snap == null || master == null || snap.players == null || snap.players.Count == 0) return;

            var playerSnap = MatchPlayerSnapshotForUser(snap, user);
            if (playerSnap == null) return;

            _matchedMasters[master] = playerSnap;

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
                    RestoreInventory(master.inventory, playerSnap.items, playerSnap.tempItems, playerSnap.equipments, playerSnap.infusionBonus,
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
                    RestoreExtraModeAndArtifactState(Run.instance, snap);
                }

                var players = PlayerCharacterMasterController.instances;
                if (snap.players != null)
                {
                    for (int i = 0; i < players.Count; i++)
                    {
                        var pcmc = players[i];
                        if (pcmc == null || pcmc.master == null) continue;

                        if (!_matchedMasters.TryGetValue(pcmc.master, out var pSnap) || pSnap == null)
                        {
                            // Fallback only for singleplayer if SetupUserCharacterMaster did not register this master
                            if (!snap.IsMultiplayer() && i < snap.players.Count)
                            {
                                pSnap = snap.players[i];
                            }
                            else
                            {
                                continue;
                            }
                        }

                        // Re-apply currency & luck after stage entry so stage transition resets don't zero out saved gold
                        pcmc.master.money = pSnap.money;
                        pcmc.master.voidCoins = pSnap.voidCoins;
                        pcmc.master.luck = pSnap.luck;

                        if (pSnap.devotionItems != null && pSnap.devotionItems.Count > 0)
                        {
                            RestoreDevotionItems(pcmc.master, pSnap.devotionItems);
                        }

                        if (pSnap.minions == null) continue;

                        var bodyObj = pcmc.master.GetBodyObject();
                        Vector3 basePos = bodyObj != null ? bodyObj.transform.position : Vector3.zero;
                        Quaternion baseRot = bodyObj != null ? bodyObj.transform.rotation : Quaternion.identity;

                        for (int m = 0; m < pSnap.minions.Count; m++)
                        {
                            var minionSnap = pSnap.minions[m];
                            if (minionSnap == null || string.IsNullOrEmpty(minionSnap.masterName)) continue;

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
                                RestoreInventory(spawnedMaster.inventory, minionSnap.items, minionSnap.tempItems, minionSnap.equipments, 0, 0, 0);
                            }
                        }
                    }
                }

                if (CeniReloadPlugin.ShowChatNotifications == null || CeniReloadPlugin.ShowChatNotifications.Value)
                {
                    string stageName = CeniReloadStrings.LocalizeGameToken(snap.stageDisplayToken, snap.stageSceneName);
                    Chat.AddMessage(CeniReloadStrings.Format("CENIRELOAD_CHAT_LOADED", snap.slotIndex, stageName, snap.stageClearCount + 1));
                }
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

        private static void RestoreExtraModeAndArtifactState(Run run, CheckpointSnapshot snap)
        {
            if (run is InfiniteTowerRun towerRun)
            {
                if (snap.simulacrumWaveIndex > 0)
                {
                    towerRun.Network_waveIndex = snap.simulacrumWaveIndex;
                }
                if (SimulacrumPatternIdxField != null && snap.simulacrumEnemyItemPatternIndex > 0)
                {
                    SimulacrumPatternIdxField.SetValue(towerRun, snap.simulacrumEnemyItemPatternIndex);
                }
                if (SimulacrumEnemyInvField?.GetValue(towerRun) is Inventory enemyInv && enemyInv != null &&
                    snap.simulacrumEnemyItems != null && snap.simulacrumEnemyItems.Count > 0)
                {
                    ReplacePermanentInventory(enemyInv, snap.simulacrumEnemyItems);
                }
            }

            if (snap.evolutionMonsterItems != null && snap.evolutionMonsterItems.Count > 0 &&
                EvolutionInventoryField?.GetValue(null) is Inventory monsterInv && monsterInv != null)
            {
                ReplacePermanentInventory(monsterInv, snap.evolutionMonsterItems);
                if (EvolutionIteratorField != null)
                {
                    EvolutionIteratorField.SetValue(null, snap.evolutionItemIterator);
                }
            }
        }

        private static void RestoreDevotionItems(CharacterMaster master, List<ItemEntry> devotionItems)
        {
            if (master == null || devotionItems == null || devotionItems.Count == 0) return;
            var list = DevotionInstanceListField?.GetValue(null) as List<DevotionInventoryController>;
            if (list == null) return;

            for (int i = 0; i < list.Count; i++)
            {
                var ctrl = list[i];
                if (ctrl == null || ctrl.SummonerMaster != master) continue;

                if (DevotionInventoryField?.GetValue(ctrl) is Inventory devInv && devInv != null)
                {
                    ReplacePermanentInventory(devInv, devotionItems);
                }
                break;
            }
        }

        private static void ReplacePermanentInventory(Inventory inventory, List<ItemEntry> targetItems)
        {
            if (inventory == null || targetItems == null) return;

            var existingOrder = inventory.itemAcquisitionOrder;
            if (existingOrder != null && existingOrder.Count > 0)
            {
                var snapshotExisting = new List<ItemIndex>(existingOrder);
                for (int i = 0; i < snapshotExisting.Count; i++)
                {
                    var idx = snapshotExisting[i];
                    if (idx == ItemIndex.None) continue;
                    int cur = inventory.GetItemCountPermanent(idx);
                    if (cur > 0)
                    {
                        inventory.RemoveItemPermanent(idx, cur);
                    }
                }
            }

            var seen = new HashSet<ItemIndex>();
            for (int i = 0; i < targetItems.Count; i++)
            {
                var entry = targetItems[i];
                if (entry == null || entry.count <= 0 || string.IsNullOrEmpty(entry.itemName)) continue;

                var idx = ItemCatalog.FindItemIndex(entry.itemName);
                if (idx == ItemIndex.None || ItemCatalog.GetItemDef(idx) == null) continue;
                if (!seen.Add(idx)) continue;

                inventory.GiveItemPermanent(idx, entry.count);
            }
        }

        private static void RestoreInventory(
            Inventory inventory,
            List<ItemEntry> items,
            List<TempItemEntry> tempItems,
            List<EquipmentEntry> equipments,
            uint infusionBonus,
            byte activeSlot,
            byte activeSet)
        {
            if (inventory == null) return;

            if (items != null)
            {
                var seenPermanent = new HashSet<ItemIndex>();
                for (int i = 0; i < items.Count; i++)
                {
                    var entry = items[i];
                    if (entry == null || entry.count <= 0 || string.IsNullOrEmpty(entry.itemName)) continue;

                    var itemIndex = ItemCatalog.FindItemIndex(entry.itemName);
                    if (itemIndex == ItemIndex.None || ItemCatalog.GetItemDef(itemIndex) == null) continue;
                    if (!seenPermanent.Add(itemIndex)) continue;

                    int existing = inventory.GetItemCountPermanent(itemIndex);
                    int delta = entry.count - existing;
                    if (delta > 0)
                    {
                        inventory.GiveItemPermanent(itemIndex, delta);
                    }
                }
            }

            if (tempItems != null)
            {
                var seenTemp = new HashSet<ItemIndex>();
                for (int i = 0; i < tempItems.Count; i++)
                {
                    var temp = tempItems[i];
                    if (temp == null || string.IsNullOrEmpty(temp.itemName) ||
                        float.IsNaN(temp.rawValue) || float.IsInfinity(temp.rawValue) || temp.rawValue <= 0.001f)
                    {
                        continue;
                    }

                    var itemIdx = ItemCatalog.FindItemIndex(temp.itemName);
                    if (itemIdx == ItemIndex.None || ItemCatalog.GetItemDef(itemIdx) == null) continue;
                    if (!seenTemp.Add(itemIdx)) continue;

                    float existingTemp = inventory.GetTempItemRawValue(itemIdx);
                    float deltaTemp = temp.rawValue - existingTemp;
                    if (deltaTemp > 0.001f)
                    {
                        inventory.GiveItemTemp(itemIdx, deltaTemp);
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
