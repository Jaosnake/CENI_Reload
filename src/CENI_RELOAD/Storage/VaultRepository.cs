using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using BepInEx;
using Jaosnake.CeniReload.Model;
using RoR2;
using UnityEngine;
using IOPath = System.IO.Path;

namespace Jaosnake.CeniReload.Storage
{
    public enum SaveSlotResult
    {
        Saved,
        AllSlotsLocked,
        Failed
    }

    public static class VaultRepository
    {
        public const int MaxSlotsPerSurvivor = 5;
        public const int NoAvailableSlot = -1;
        private const string FileExtension = ".cenireload.json";

        private static string VaultRoot => IOPath.Combine(Paths.ConfigPath, "Jaosnake.CENI_RELOAD", "Vault");

        public static string GetCurrentProfileKey()
        {
            var localUser = LocalUserManager.GetFirstLocalUser();
            string raw = localUser?.userProfile?.fileName;
            if (string.IsNullOrWhiteSpace(raw)) raw = "default";
            return SanitizePathSegment(raw);
        }

        public static string SanitizePathSegment(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "unknown";
            char[] invalid = IOPath.GetInvalidFileNameChars();
            var chars = input.Trim().Select(c => invalid.Contains(c) || c == '.' ? '_' : c).ToArray();
            string cleaned = new string(chars);
            return string.IsNullOrWhiteSpace(cleaned) ? "unknown" : cleaned;
        }

        private static string GetSurvivorDirectory(string profileKey, string survivorBodyName)
        {
            return IOPath.Combine(VaultRoot, SanitizePathSegment(profileKey), SanitizePathSegment(survivorBodyName));
        }

        private static string GetSlotFilePath(string profileKey, string survivorBodyName, int slotIndex)
        {
            return IOPath.Combine(GetSurvivorDirectory(profileKey, survivorBodyName), $"slot_{slotIndex}{FileExtension}");
        }

        public static int PickAutoSaveSlot(string profileKey, string survivorBodyName)
        {
            var slots = GetSurvivorSlots(profileKey, survivorBodyName);

            for (int i = 0; i < MaxSlotsPerSurvivor; i++)
            {
                if (slots[i] == null) return i + 1;
            }

            int oldestUnlockedSlot = NoAvailableSlot;
            DateTime oldestTime = DateTime.MaxValue;
            for (int i = 0; i < MaxSlotsPerSurvivor; i++)
            {
                var snap = slots[i];
                if (snap == null || snap.isLocked) continue;

                if (DateTime.TryParse(snap.savedUtcIso, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out DateTime t))
                {
                    if (t < oldestTime)
                    {
                        oldestTime = t;
                        oldestUnlockedSlot = i + 1;
                    }
                }
                else if (oldestUnlockedSlot == NoAvailableSlot)
                {
                    oldestUnlockedSlot = i + 1;
                }
            }
            return oldestUnlockedSlot;
        }

        public static bool AreAllSlotsLocked(string profileKey, string survivorBodyName)
        {
            if (string.IsNullOrEmpty(survivorBodyName)) return false;
            var slots = GetSurvivorSlots(profileKey, survivorBodyName);
            for (int i = 0; i < MaxSlotsPerSurvivor; i++)
            {
                if (slots[i] == null || !slots[i].isLocked)
                {
                    return false;
                }
            }
            return true;
        }

        public static bool ToggleSlotLock(string profileKey, string survivorBodyName, int slotIndex, out bool newLockedState)
        {
            newLockedState = false;
            var snapshot = LoadSlot(profileKey, survivorBodyName, slotIndex);
            if (snapshot == null) return false;

            snapshot.isLocked = !snapshot.isLocked;
            newLockedState = snapshot.isLocked;

            try
            {
                string dir = GetSurvivorDirectory(profileKey, survivorBodyName);
                Directory.CreateDirectory(dir);
                string finalPath = GetSlotFilePath(profileKey, survivorBodyName, slotIndex);
                string tempPath = finalPath + ".tmp";

                string json = JsonUtility.ToJson(snapshot, true);
                File.WriteAllText(tempPath, json);

                if (File.Exists(finalPath))
                {
                    File.Delete(finalPath);
                }
                File.Move(tempPath, finalPath);

                CeniReloadPlugin.Log?.LogInfo($"{(newLockedState ? "Locked" : "Unlocked")} checkpoint: {survivorBodyName} Slot {slotIndex}");
                return true;
            }
            catch (Exception ex)
            {
                CeniReloadPlugin.Log?.LogError($"Failed to toggle lock on {survivorBodyName} Slot {slotIndex}: {ex}");
                return false;
            }
        }

        public static SaveSlotResult SaveSnapshotWithStatus(CheckpointSnapshot snapshot, int targetSlot = -1)
        {
            if (snapshot == null || string.IsNullOrEmpty(snapshot.survivorBodyName))
                return SaveSlotResult.Failed;

            try
            {
                string profileKey = string.IsNullOrEmpty(snapshot.profileKey)
                    ? GetCurrentProfileKey()
                    : snapshot.profileKey;
                snapshot.profileKey = profileKey;

                int slot;
                if (targetSlot >= 1 && targetSlot <= MaxSlotsPerSurvivor)
                {
                    var existing = LoadSlot(profileKey, snapshot.survivorBodyName, targetSlot);
                    if (existing != null && existing.isLocked)
                    {
                        CeniReloadPlugin.Log?.LogWarning($"Refused to overwrite locked checkpoint: {snapshot.survivorBodyName} Slot {targetSlot}.");
                        return SaveSlotResult.AllSlotsLocked;
                    }
                    slot = targetSlot;
                }
                else
                {
                    slot = PickAutoSaveSlot(profileKey, snapshot.survivorBodyName);
                    if (slot == NoAvailableSlot)
                    {
                        CeniReloadPlugin.Log?.LogWarning($"Auto-save skipped: all {MaxSlotsPerSurvivor} slots for {snapshot.survivorBodyName} are locked.");
                        return SaveSlotResult.AllSlotsLocked;
                    }
                }

                snapshot.slotIndex = slot;
                snapshot.isLocked = false;
                snapshot.savedUtcIso = DateTime.UtcNow.ToString("O");

                string dir = GetSurvivorDirectory(profileKey, snapshot.survivorBodyName);
                Directory.CreateDirectory(dir);

                string finalPath = GetSlotFilePath(profileKey, snapshot.survivorBodyName, slot);
                string tempPath = finalPath + ".tmp";

                string json = JsonUtility.ToJson(snapshot, true);
                File.WriteAllText(tempPath, json);

                if (File.Exists(finalPath))
                {
                    File.Delete(finalPath);
                }
                File.Move(tempPath, finalPath);

                CeniReloadPlugin.Log?.LogInfo($"Saved checkpoint: {snapshot.survivorBodyName} Slot {slot} (Stage {snapshot.stageClearCount + 1}: {snapshot.stageSceneName})");
                return SaveSlotResult.Saved;
            }
            catch (Exception ex)
            {
                CeniReloadPlugin.Log?.LogError($"Failed to save checkpoint: {ex}");
                return SaveSlotResult.Failed;
            }
        }

        public static bool SaveSnapshot(CheckpointSnapshot snapshot, int targetSlot = -1)
        {
            return SaveSnapshotWithStatus(snapshot, targetSlot) == SaveSlotResult.Saved;
        }

        public static CheckpointSnapshot LoadSlot(string profileKey, string survivorBodyName, int slotIndex)
        {
            if (slotIndex < 1 || slotIndex > MaxSlotsPerSurvivor) return null;

            string path = GetSlotFilePath(profileKey, survivorBodyName, slotIndex);
            if (!File.Exists(path)) return null;

            try
            {
                string json = File.ReadAllText(path);
                var snapshot = JsonUtility.FromJson<CheckpointSnapshot>(json);
                if (snapshot == null || string.IsNullOrEmpty(snapshot.stageSceneName)) return null;
                snapshot.slotIndex = slotIndex;
                return snapshot;
            }
            catch (Exception ex)
            {
                CeniReloadPlugin.Log?.LogWarning($"Corrupted checkpoint at '{path}': {ex.Message}");
                return null;
            }
        }

        public static CheckpointSnapshot[] GetSurvivorSlots(string profileKey, string survivorBodyName)
        {
            var result = new CheckpointSnapshot[MaxSlotsPerSurvivor];
            if (string.IsNullOrEmpty(survivorBodyName)) return result;

            for (int i = 1; i <= MaxSlotsPerSurvivor; i++)
            {
                result[i - 1] = LoadSlot(profileKey, survivorBodyName, i);
            }
            return result;
        }

        public static List<CheckpointSnapshot> GetAllProfileSnapshots(string profileKey)
        {
            var list = new List<CheckpointSnapshot>();
            string profileDir = IOPath.Combine(VaultRoot, SanitizePathSegment(profileKey));
            if (!Directory.Exists(profileDir)) return list;

            foreach (string survivorDir in Directory.GetDirectories(profileDir))
            {
                foreach (string file in Directory.GetFiles(survivorDir, "*" + FileExtension))
                {
                    try
                    {
                        string json = File.ReadAllText(file);
                        var snap = JsonUtility.FromJson<CheckpointSnapshot>(json);
                        if (snap != null && !string.IsNullOrEmpty(snap.stageSceneName))
                        {
                            list.Add(snap);
                        }
                    }
                    catch
                    {
                        // Ignore unreadable files
                    }
                }
            }

            list.Sort((a, b) => string.Compare(b.savedUtcIso, a.savedUtcIso, StringComparison.Ordinal));
            return list;
        }

        public static bool DeleteSlot(string profileKey, string survivorBodyName, int slotIndex)
        {
            if (slotIndex < 1 || slotIndex > MaxSlotsPerSurvivor) return false;
            string path = GetSlotFilePath(profileKey, survivorBodyName, slotIndex);
            if (!File.Exists(path)) return false;

            var existing = LoadSlot(profileKey, survivorBodyName, slotIndex);
            if (existing != null && existing.isLocked)
            {
                CeniReloadPlugin.Log?.LogWarning($"Refused to delete locked checkpoint: {survivorBodyName} Slot {slotIndex}");
                return false;
            }

            try
            {
                File.Delete(path);
                CeniReloadPlugin.Log?.LogInfo($"Deleted checkpoint: {survivorBodyName} Slot {slotIndex}");
                return true;
            }
            catch (Exception ex)
            {
                CeniReloadPlugin.Log?.LogError($"Failed to delete checkpoint '{path}': {ex}");
                return false;
            }
        }
    }
}
