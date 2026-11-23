using System;
using System.Collections.Generic;
using BepInEx.Bootstrap;

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
        public float runFixedTime;
        public string difficultyName;
        public string difficultyDisplayToken;
        public ulong seed;
        public int shopPortalCount;
        public string ruleBookBase64;
        public ulong teamExperience;

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
            if (DateTime.TryParse(savedUtcIso, out DateTime parsed))
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
                if (p.items == null) continue;
                foreach (var item in p.items) total += item.count;
            }
            return total;
        }

        public int GetTotalMinionCount()
        {
            int total = 0;
            if (players == null) return 0;
            foreach (var p in players)
            {
                if (p.minions != null) total += p.minions.Count;
            }
            return total;
        }

        public bool HasModMismatch()
        {
            if (loadedPlugins == null || loadedPlugins.Count == 0) return false;
            var current = Chainloader.PluginInfos;
            if (current == null) return false;

            if (current.Count != loadedPlugins.Count) return true;

            foreach (var stamp in loadedPlugins)
            {
                if (!current.TryGetValue(stamp.guid, out var info)) return true;
                string ver = info.Metadata?.Version?.ToString() ?? "";
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
        public List<EquipmentEntry> equipments = new List<EquipmentEntry>();
    }

    [Serializable]
    public class PluginStamp
    {
        public string guid;
        public string version;
    }
}
