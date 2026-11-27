using System;
using System.Collections.Generic;
using System.Globalization;
using BepInEx.Bootstrap;
using RoR2;
using UnityEngine;

namespace Jaosnake.CeniReload.Model
{
    [Serializable]
    public class CheckpointSnapshot
    {
        public int schemaVersion = 3;
        [NonSerialized] public bool isBackupPreview;
        public int slotIndex;
        public bool isLocked;
        public bool isMultiplayerRun;
        public string vaultBucket;
        public string savedUtcIso;
        public string profileKey;

        // Survivor summary
        public string survivorBodyName;
        public string survivorDisplayToken;

        // Run & Stage state
        public string gameModeName;
        public string stageSceneName;
        public string stageDisplayToken;
        public int stageClearCount;
        public int loopClearCount;
        public int stageClearCountAtLoopStart;
        public float runFixedTime;
        public string difficultyName;
        public string difficultyDisplayToken;
        public ulong seed;
        public bool hasStageRandomState;
        public ulong stageRandomState0;
        public ulong stageRandomState1;
        public int shopPortalCount;
        public string ruleBookBase64;
        public ulong teamExperience;
        public List<string> eventFlags = new List<string>();
        public List<RandomSnapshot> randomStates = new List<RandomSnapshot>();

        // Extra modes & artifacts (Simulacrum / Evolution)
        public int simulacrumWaveIndex;
        public int simulacrumEnemyItemPatternIndex;
        public List<ItemEntry> simulacrumEnemyItems = new List<ItemEntry>();
        public int evolutionItemIterator;
        public List<ItemEntry> evolutionMonsterItems = new List<ItemEntry>();

        // Entities
        public List<PlayerSnapshot> players = new List<PlayerSnapshot>();
        public List<PluginStamp> loadedPlugins = new List<PluginStamp>();

        public string FormatRunTime()
        {
            var span = TimeSpan.FromSeconds(Math.Max(0, runFixedTime));
            return span.TotalHours >= 1.0
                ? $"{(int)span.TotalHours:D2}:{span.Minutes:D2}:{span.Seconds:D2}"
                : $"{span.Minutes:D2}:{span.Seconds:D2}";
        }

        public string FormatSavedDate()
        {
            if (DateTime.TryParse(savedUtcIso, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime parsed))
            {
                return parsed.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
            }
            return savedUtcIso ?? "-";
        }

        public int GetTotalItemCount()
        {
            int total = 0;
            if (players == null) return 0;
            foreach (var p in players)
            {
                if (p == null || p.items == null) continue;
                foreach (var item in p.items)
                {
                    if (item != null && item.count > 0) total += item.count;
                }
            }
            return total;
        }

        public int GetTotalTempItemCount()
        {
            int total = 0;
            if (players == null) return 0;
            foreach (var p in players)
            {
                if (p == null || p.tempItems == null) continue;
                foreach (var t in p.tempItems)
                {
                    if (t != null && !float.IsNaN(t.rawValue) && !float.IsInfinity(t.rawValue) && t.rawValue > 0f)
                    {
                        total += Mathf.CeilToInt(t.rawValue);
                    }
                }
            }
            return total;
        }

        public int GetTotalMinionCount()
        {
            int total = 0;
            if (players == null) return 0;
            foreach (var p in players)
            {
                if (p != null && p.minions != null) total += p.minions.Count;
            }
            return total;
        }

        public bool IsMultiplayer()
        {
            return isMultiplayerRun || (players != null && players.Count > 1);
        }

        public string GetStorageBucket()
        {
            if (!string.IsNullOrEmpty(vaultBucket))
                return vaultBucket;
            return IsMultiplayer() ? "Multiplayer" : survivorBodyName;
        }

        public bool IsSimulacrum()
        {
            return simulacrumWaveIndex > 0 ||
                   (!string.IsNullOrEmpty(gameModeName) &&
                    gameModeName.IndexOf("InfiniteTower", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        public bool HasEvolutionOrDevotion()
        {
            if (evolutionMonsterItems != null && evolutionMonsterItems.Count > 0)
                return true;
            if (players != null)
            {
                for (int i = 0; i < players.Count; i++)
                {
                    if (players[i]?.devotionItems != null && players[i].devotionItems.Count > 0)
                        return true;
                }
            }
            return false;
        }

        public bool IsSpecialModeOrArtifact()
        {
            return IsSimulacrum() || HasEvolutionOrDevotion();
        }

        public bool HasMissingSurvivors()
        {
            return !string.IsNullOrEmpty(GetFirstMissingSurvivorName());
        }

        public string GetFirstMissingSurvivorName()
        {
            if (!BodyCatalog.availability.available) return string.Empty;

            // In identity-based co-op saves only present participants are restored.
            // The main menu has no final roster yet: validate again in PreGame.
            if (IsMultiplayer() && players != null && players.Exists(p => p != null && !string.IsNullOrEmpty(p.playerId)))
            {
                if (PreGameController.instance == null) return string.Empty;
                var presentIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var user in NetworkUser.readOnlyInstancesList)
                    presentIds.Add(Core.SnapshotCapture.BuildPlayerIdentity(user));
                return GetMissingSurvivorForParticipants(presentIds, name => BodyCatalog.FindBodyPrefab(name) != null);
            }

            if (!string.IsNullOrEmpty(survivorBodyName) && BodyCatalog.FindBodyPrefab(survivorBodyName) == null)
            {
                return survivorBodyName;
            }

            if (players != null)
            {
                for (int i = 0; i < players.Count; i++)
                {
                    var p = players[i];
                    if (p != null && !string.IsNullOrEmpty(p.bodyName) && BodyCatalog.FindBodyPrefab(p.bodyName) == null)
                    {
                        return p.bodyName;
                    }
                }
            }

            return string.Empty;
        }

        public string GetMissingSurvivorForParticipants(ISet<string> presentIds, Func<string, bool> survivorExists)
        {
            if (players == null) return string.Empty;
            foreach (var player in players)
            {
                if (player == null || string.IsNullOrEmpty(player.playerId) || !presentIds.Contains(player.playerId)) continue;
                if (!string.IsNullOrEmpty(player.bodyName) && !survivorExists(player.bodyName)) return player.bodyName;
            }
            return string.Empty;
        }

        public bool HasModMismatch()
        {
            if (loadedPlugins == null || loadedPlugins.Count == 0) return false;
            var current = Chainloader.PluginInfos;
            if (current == null) return false;

            if (current.Count != loadedPlugins.Count) return true;

            foreach (var stamp in loadedPlugins)
            {
                if (stamp == null || string.IsNullOrEmpty(stamp.guid)) return true;
                if (!current.TryGetValue(stamp.guid, out var info)) return true;
                string ver = info?.Metadata?.Version?.ToString() ?? "";
                if (!string.Equals(ver, stamp.version, StringComparison.Ordinal)) return true;
            }
            return false;
        }
    }

    [Serializable]
    public class PlayerSnapshot
    {
        public int userIndex;
        public string playerId;
        public string playerName;
        public string bodyName;
        public bool hasBodyPosition;
        public Vector3 bodyFootPosition;
        public float bodyYaw;
        public bool hasVitalState;
        public float health;
        public float shield;
        public float barrier;
        public List<SkillSnapshot> skills = new List<SkillSnapshot>();
        public uint money;
        public uint voidCoins;
        public float luck;
        public uint infusionBonus;
        public int activeEquipmentSlot;
        public int activeEquipmentSet;
        public string loadoutBase64;
        public List<ItemEntry> items = new List<ItemEntry>();
        public List<TempItemEntry> tempItems = new List<TempItemEntry>();
        public List<EquipmentEntry> equipments = new List<EquipmentEntry>();
        public List<MinionSnapshot> minions = new List<MinionSnapshot>();
        public List<ItemEntry> devotionItems = new List<ItemEntry>();

        public PlayerSnapshot ForLaterStage()
        {
            // Inventory/loadout entries are immutable during restoration. Clone
            // the position-bearing records so the source checkpoint is untouched.
            var copy = (PlayerSnapshot)MemberwiseClone();
            copy.hasBodyPosition = false;
            copy.minions = minions?.ConvertAll(m => m?.ForLaterStage());
            return copy;
        }
    }

    [Serializable]
    public class ItemEntry
    {
        public string itemName;
        public int count;
    }

    [Serializable]
    public class TempItemEntry
    {
        public string itemName;
        public float rawValue;
    }

    [Serializable]
    public class EquipmentEntry
    {
        public uint slot;
        public uint set;
        public string equipmentName;
        public byte charges;
        public float cooldownRemaining;
    }

    [Serializable]
    public class MinionSnapshot
    {
        public string masterName;
        public string bodyName;
        public bool hasBodyPosition;
        public Vector3 bodyFootPosition;
        public float bodyYaw;
        public List<ItemEntry> items = new List<ItemEntry>();
        public List<TempItemEntry> tempItems = new List<TempItemEntry>();
        public List<EquipmentEntry> equipments = new List<EquipmentEntry>();

        public MinionSnapshot ForLaterStage()
        {
            var copy = (MinionSnapshot)MemberwiseClone();
            copy.hasBodyPosition = false;
            return copy;
        }
    }

    [Serializable]
    public class PluginStamp
    {
        public string guid;
        public string version;
    }

    [Serializable]
    public class RandomSnapshot
    {
        public string field;
        public ulong state0;
        public ulong state1;
    }

    [Serializable]
    public class SkillSnapshot
    {
        public int componentIndex;
        public string skillName;
        public string definitionName;
        public int stock;
        public float rechargeStopwatch;
    }
}
