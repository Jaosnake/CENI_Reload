using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Bootstrap;
using HarmonyLib;
using Jaosnake.CeniReload.Model;
using Jaosnake.CeniReload.Storage;
using RoR2;
using RoR2.Artifacts;
using UnityEngine;
using UnityEngine.Networking;

namespace Jaosnake.CeniReload.Core
{
    public static class SnapshotCapture
    {
        private static readonly FieldInfo EventFlagsField = AccessTools.Field(typeof(Run), "eventFlags");
        private static readonly FieldInfo SimulacrumPatternIdxField = AccessTools.Field(typeof(InfiniteTowerRun), "enemyItemPatternIndex");
        private static readonly FieldInfo SimulacrumEnemyInvField = AccessTools.Field(typeof(InfiniteTowerRun), "enemyInventory");
        private static readonly FieldInfo EvolutionInventoryField = AccessTools.Field(typeof(MonsterTeamGainsItemsArtifactManager), "monsterTeamInventory");
        private static readonly FieldInfo EvolutionIteratorField = AccessTools.Field(typeof(MonsterTeamGainsItemsArtifactManager), "currentItemIterator");
        private static readonly FieldInfo DevotionInstanceListField = AccessTools.Field(typeof(DevotionInventoryController), "InstanceList");
        private static readonly FieldInfo DevotionInventoryField = AccessTools.Field(typeof(DevotionInventoryController), "_devotionMinionInventory");

        public static string BuildPlayerIdentity(NetworkUser user)
        {
            if (user == null) return string.Empty;
            var uid = user.id;
            if (uid.value != 0UL)
            {
                return $"steam:{uid.value}:{uid.subId}";
            }
            if (!string.IsNullOrEmpty(uid.strValue))
            {
                return $"eos:{uid.strValue}:{uid.subId}";
            }
            if (!string.IsNullOrEmpty(user.userName))
            {
                return $"name:{user.userName}:{uid.subId}";
            }
            return string.Empty;
        }

        public static CheckpointSnapshot CaptureCurrentRun()
        {
            var run = Run.instance;
            var stage = Stage.instance;
            if (run == null || stage == null) return null;

            var sceneDef = SceneCatalog.GetSceneDefForCurrentScene();
            if (sceneDef == null || sceneDef.sceneType != SceneType.Stage) return null;

            var snapshot = new CheckpointSnapshot
            {
                schemaVersion = 1,
                profileKey = VaultRepository.GetCurrentProfileKey(),
                gameModeName = GameModeCatalog.GetGameModeName(run.gameModeIndex),
                stageSceneName = sceneDef.baseSceneName,
                stageDisplayToken = sceneDef.nameToken,
                stageClearCount = run.stageClearCount,
                loopClearCount = run.loopClearCount,
                stageClearCountAtLoopStart = run.NetworkstageClearCountAtLoopStart,
                runFixedTime = run.GetRunStopwatch(),
                seed = run.seed,
                shopPortalCount = run.shopPortalCount,
                ruleBookBase64 = SerializeRuleBook(run.ruleBook),
                teamExperience = TeamManager.instance != null
                    ? TeamManager.instance.GetTeamExperience(TeamIndex.Player)
                    : 0UL
            };

            if (EventFlagsField?.GetValue(run) is HashSet<string> flags)
            {
                foreach (string flag in flags)
                {
                    if (!string.IsNullOrEmpty(flag))
                    {
                        snapshot.eventFlags.Add(flag);
                    }
                }
            }

            CaptureExtraModeAndArtifactState(run, snapshot);

            var diffDef = DifficultyCatalog.GetDifficultyDef(run.selectedDifficulty);
            snapshot.difficultyName = run.selectedDifficulty.ToString();
            snapshot.difficultyDisplayToken = diffDef != null ? diffDef.nameToken : string.Empty;

            var players = PlayerCharacterMasterController.instances;
            var ordered = new List<PlayerCharacterMasterController>(players.Count);
            for (int i = 0; i < players.Count; i++)
            {
                var p = players[i];
                if (p == null || p.master == null) continue;
                if (p.networkUser != null && p.networkUser.isLocalPlayer)
                {
                    ordered.Insert(0, p);
                }
                else
                {
                    ordered.Add(p);
                }
            }

            for (int i = 0; i < ordered.Count; i++)
            {
                var pcmc = ordered[i];
                var master = pcmc.master;
                var playerSnap = CapturePlayer(master, pcmc.networkUser, i);
                if (playerSnap == null) continue;

                snapshot.players.Add(playerSnap);

                if (string.IsNullOrEmpty(snapshot.survivorBodyName))
                {
                    snapshot.survivorBodyName = playerSnap.bodyName;
                    var bodyPrefab = master.bodyPrefab;
                    var survDef = bodyPrefab != null ? SurvivorCatalog.FindSurvivorDefFromBody(bodyPrefab) : null;
                    snapshot.survivorDisplayToken = survDef != null
                        ? survDef.displayNameToken
                        : (bodyPrefab != null && bodyPrefab.TryGetComponent<CharacterBody>(out var cb) ? cb.baseNameToken : playerSnap.bodyName);
                }
            }

            if (snapshot.players.Count == 0 || string.IsNullOrEmpty(snapshot.survivorBodyName))
                return null;

            bool isMultiplayer = snapshot.players.Count > 1 ||
                                 (NetworkUser.readOnlyInstancesList != null && NetworkUser.readOnlyInstancesList.Count > 1);
            snapshot.isMultiplayerRun = isMultiplayer;
            snapshot.vaultBucket = isMultiplayer ? VaultRepository.MultiplayerBucket : snapshot.survivorBodyName;

            if (Chainloader.PluginInfos != null)
            {
                foreach (var kv in Chainloader.PluginInfos)
                {
                    snapshot.loadedPlugins.Add(new PluginStamp
                    {
                        guid = kv.Key,
                        version = kv.Value?.Metadata?.Version?.ToString() ?? string.Empty
                    });
                }
            }

            return snapshot;
        }

        private static PlayerSnapshot CapturePlayer(CharacterMaster master, NetworkUser netUser, int userIndex)
        {
            string bodyName = master.bodyPrefab != null ? master.bodyPrefab.name : string.Empty;
            if (string.IsNullOrEmpty(bodyName)) return null;

            var player = new PlayerSnapshot
            {
                userIndex = userIndex,
                playerId = BuildPlayerIdentity(netUser),
                playerName = netUser != null && !string.IsNullOrEmpty(netUser.userName) ? netUser.userName : $"Player {userIndex + 1}",
                bodyName = bodyName,
                money = master.money,
                voidCoins = master.voidCoins,
                luck = master.luck,
                loadoutBase64 = SerializeLoadout(master.loadout)
            };

            if (master.inventory != null)
            {
                player.infusionBonus = master.inventory.infusionBonus;
                player.activeEquipmentSlot = master.inventory.activeEquipmentSlot;
                player.activeEquipmentSet = (int)master.inventory.GetActiveEquipmentSetInSlot(master.inventory.activeEquipmentSlot);
                CaptureInventory(master.inventory, player.items, player.tempItems, player.equipments);
            }

            var allMasters = CharacterMaster.readOnlyInstancesList;
            for (int m = 0; m < allMasters.Count; m++)
            {
                var candidate = allMasters[m];
                if (candidate == null || candidate == master) continue;
                if (candidate.minionOwnership == null || candidate.minionOwnership.ownerMaster != master) continue;
                if (candidate.IsDeadAndOutOfLivesServer()) continue;

                var masterIdx = MasterCatalog.FindMasterIndex(candidate.gameObject);
                string masterName = masterIdx != MasterCatalog.MasterIndex.none
                    ? MasterCatalog.GetMasterName(masterIdx)
                    : candidate.name.Replace("(Clone)", "").Trim();
                string minionBodyName = candidate.bodyPrefab != null ? candidate.bodyPrefab.name : string.Empty;
                if (string.IsNullOrEmpty(masterName)) continue;

                var minionSnap = new MinionSnapshot
                {
                    masterName = masterName,
                    bodyName = minionBodyName
                };

                if (candidate.inventory != null)
                {
                    CaptureInventory(candidate.inventory, minionSnap.items, minionSnap.tempItems, minionSnap.equipments);
                }

                player.minions.Add(minionSnap);
            }

            CaptureDevotionItems(master, player.devotionItems);

            return player;
        }

        private static void CaptureExtraModeAndArtifactState(Run run, CheckpointSnapshot snapshot)
        {
            if (run is InfiniteTowerRun towerRun)
            {
                snapshot.simulacrumWaveIndex = towerRun.waveIndex;
                if (SimulacrumPatternIdxField != null && SimulacrumPatternIdxField.GetValue(towerRun) is int patternIdx)
                {
                    snapshot.simulacrumEnemyItemPatternIndex = patternIdx;
                }
                if (SimulacrumEnemyInvField?.GetValue(towerRun) is Inventory enemyInv && enemyInv != null)
                {
                    CapturePermanentItemsOnly(enemyInv, snapshot.simulacrumEnemyItems);
                }
            }

            if (EvolutionInventoryField?.GetValue(null) is Inventory monsterInv && monsterInv != null)
            {
                CapturePermanentItemsOnly(monsterInv, snapshot.evolutionMonsterItems);
                if (EvolutionIteratorField != null && EvolutionIteratorField.GetValue(null) is int iter)
                {
                    snapshot.evolutionItemIterator = iter;
                }
            }
        }

        private static void CaptureDevotionItems(CharacterMaster master, List<ItemEntry> devotionOut)
        {
            if (master == null || devotionOut == null) return;
            var list = DevotionInstanceListField?.GetValue(null) as List<DevotionInventoryController>;
            if (list == null) return;

            for (int i = 0; i < list.Count; i++)
            {
                var ctrl = list[i];
                if (ctrl == null || ctrl.SummonerMaster != master) continue;

                if (DevotionInventoryField?.GetValue(ctrl) is Inventory devInv && devInv != null)
                {
                    CapturePermanentItemsOnly(devInv, devotionOut);
                }
                break;
            }
        }

        private static void CapturePermanentItemsOnly(Inventory inventory, List<ItemEntry> itemsOut)
        {
            if (inventory == null || itemsOut == null) return;

            var order = inventory.itemAcquisitionOrder;
            if (order == null) return;

            var seen = new HashSet<ItemIndex>();
            for (int i = 0; i < order.Count; i++)
            {
                var idx = order[i];
                if (idx == ItemIndex.None || !seen.Add(idx)) continue;

                int count = inventory.GetItemCountPermanent(idx);
                if (count <= 0) continue;

                var def = ItemCatalog.GetItemDef(idx);
                if (def == null || string.IsNullOrEmpty(def.name)) continue;

                itemsOut.Add(new ItemEntry
                {
                    itemName = def.name,
                    count = count
                });
            }
        }

        private static void CaptureInventory(Inventory inventory, List<ItemEntry> itemsOut, List<TempItemEntry> tempItemsOut, List<EquipmentEntry> equipOut)
        {
            if (inventory == null) return;

            var order = inventory.itemAcquisitionOrder;
            if (order != null)
            {
                var seenPermanent = new HashSet<ItemIndex>();
                for (int i = 0; i < order.Count; i++)
                {
                    var idx = order[i];
                    if (idx == ItemIndex.None || !seenPermanent.Add(idx)) continue;

                    int count = inventory.GetItemCountPermanent(idx);
                    if (count <= 0) continue;

                    var def = ItemCatalog.GetItemDef(idx);
                    if (def == null || string.IsNullOrEmpty(def.name)) continue;

                    itemsOut.Add(new ItemEntry
                    {
                        itemName = def.name,
                        count = count
                    });
                }
            }

            if (tempItemsOut != null)
            {
                var seenTempNames = new HashSet<string>(StringComparer.Ordinal);
                int itemCount = ItemCatalog.itemCount;
                for (int i = 0; i < itemCount; i++)
                {
                    var idx = (ItemIndex)i;
                    if (idx == ItemIndex.None) continue;

                    float raw = inventory.GetTempItemRawValue(idx);
                    if (float.IsNaN(raw) || float.IsInfinity(raw) || raw <= 0.001f) continue;

                    var def = ItemCatalog.GetItemDef(idx);
                    if (def == null || string.IsNullOrEmpty(def.name)) continue;
                    if (!seenTempNames.Add(def.name)) continue;

                    tempItemsOut.Add(new TempItemEntry
                    {
                        itemName = def.name,
                        rawValue = raw
                    });
                }
            }

            int slotCount = inventory.GetEquipmentSlotCount();
            int setCount = Math.Max(1, inventory.GetEquipmentSetCount());
            for (uint slot = 0; slot < slotCount; slot++)
            {
                for (uint set = 0; set < setCount; set++)
                {
#pragma warning disable CS0618
                    var state = inventory.GetEquipment(slot, set);
#pragma warning restore CS0618
                    if (state.equipmentIndex == EquipmentIndex.None) continue;

                    var eqDef = EquipmentCatalog.GetEquipmentDef(state.equipmentIndex);
                    if (eqDef == null || string.IsNullOrEmpty(eqDef.name)) continue;

                    float remaining = state.chargeFinishTime.isInfinity
                        ? 0f
                        : Mathf.Max(0f, state.chargeFinishTime.timeUntil);

                    equipOut.Add(new EquipmentEntry
                    {
                        slot = slot,
                        set = set,
                        equipmentName = eqDef.name,
                        charges = state.charges,
                        cooldownRemaining = remaining
                    });
                }
            }
        }

        private static string SerializeRuleBook(RuleBook ruleBook)
        {
            if (ruleBook == null) return string.Empty;
            var writer = new NetworkWriter();
            ruleBook.Serialize(writer);
            return Convert.ToBase64String(writer.ToArray());
        }

        private static string SerializeLoadout(Loadout loadout)
        {
            if (loadout == null) return string.Empty;
            var writer = new NetworkWriter();
            loadout.Serialize(writer);
            return Convert.ToBase64String(writer.ToArray());
        }
    }
}
