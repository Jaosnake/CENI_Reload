using System;
using System.Collections.Generic;
using BepInEx.Bootstrap;
using Jaosnake.CeniReload.Model;
using Jaosnake.CeniReload.Storage;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace Jaosnake.CeniReload.Core
{
    public static class SnapshotCapture
    {
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
                runFixedTime = run.GetRunStopwatch(),
                seed = run.seed,
                shopPortalCount = run.shopPortalCount,
                ruleBookBase64 = SerializeRuleBook(run.ruleBook),
                teamExperience = TeamManager.instance != null
                    ? TeamManager.instance.GetTeamExperience(TeamIndex.Player)
                    : 0UL
            };

            var diffDef = DifficultyCatalog.GetDifficultyDef(run.selectedDifficulty);
            snapshot.difficultyName = run.selectedDifficulty.ToString();
            snapshot.difficultyDisplayToken = diffDef != null ? diffDef.nameToken : string.Empty;

            var players = PlayerCharacterMasterController.instances;
            for (int i = 0; i < players.Count; i++)
            {
                var pcmc = players[i];
                if (pcmc == null || pcmc.master == null) continue;

                var master = pcmc.master;
                var playerSnap = CapturePlayer(master, i);
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

            foreach (var kv in Chainloader.PluginInfos)
            {
                snapshot.loadedPlugins.Add(new PluginStamp
                {
                    guid = kv.Key,
                    version = kv.Value?.Metadata?.Version?.ToString() ?? string.Empty
                });
            }

            return snapshot;
        }

        private static PlayerSnapshot CapturePlayer(CharacterMaster master, int userIndex)
        {
            string bodyName = master.bodyPrefab != null ? master.bodyPrefab.name : string.Empty;
            if (string.IsNullOrEmpty(bodyName)) return null;

            var player = new PlayerSnapshot
            {
                userIndex = userIndex,
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
                CaptureInventory(master.inventory, player.items, player.equipments);
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
                    CaptureInventory(candidate.inventory, minionSnap.items, minionSnap.equipments);
                }

                player.minions.Add(minionSnap);
            }

            return player;
        }

        private static void CaptureInventory(Inventory inventory, List<ItemEntry> itemsOut, List<EquipmentEntry> equipOut)
        {
            if (inventory == null) return;

            var order = inventory.itemAcquisitionOrder;
            for (int i = 0; i < order.Count; i++)
            {
                var idx = order[i];
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
