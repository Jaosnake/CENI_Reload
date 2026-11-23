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
        public int schemaVersion = 1;
        public int slotIndex;
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
        public int shopPortalCount;
        public string ruleBookBase64;
        public ulong teamExperience;
        public List<string> eventFlags = new List<string>();

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

        public bool HasMissingSurvivors()
        {
            return !string.IsNullOrEmpty(GetFirstMissingSurvivorName());
        }

        public string GetFirstMissingSurvivorName()
        {
            if (!BodyCatalog.availability.available) return string.Empty;

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
        public string bodyName;
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
        public List<ItemEntry> items = new List<ItemEntry>();
        public List<TempItemEntry> tempItems = new List<TempItemEntry>();
        public List<EquipmentEntry> equipments = new List<EquipmentEntry>();
    }

    [Serializable]
    public class PluginStamp
    {
        public string guid;
        public string version;
    }
}
