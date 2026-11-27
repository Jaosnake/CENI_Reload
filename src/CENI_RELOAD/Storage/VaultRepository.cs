using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx;
using Jaosnake.CeniReload.Model;
using RoR2;
using UnityEngine;
using Zio;
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
        public const string MultiplayerBucket = "Multiplayer";
        private const string FileExtension = ".cenireload.json";
        private static bool _loggedCloudFallback;

        private static string LocalVaultRoot => IOPath.Combine(Paths.ConfigPath, "Jaosnake.CENI_RELOAD", "Vault");

        public static bool IsUsingCloudStorage()
        {
            if (CeniReloadPlugin.UseCloudStorage == null || !CeniReloadPlugin.UseCloudStorage.Value)
                return false;

            if (RoR2Application.cloudStorage != null)
                return true;

            if (!_loggedCloudFallback)
            {
                _loggedCloudFallback = true;
                CeniReloadPlugin.Log?.LogWarning("UseCloudStorage is enabled, but RoR2Application.cloudStorage is unavailable. Falling back to local storage.");
            }
            return false;
        }

        private static UPath GetCloudVaultRoot()
        {
            string sub = CeniReloadPlugin.CloudStorageSubDirectory != null
                ? SanitizePathSegment(CeniReloadPlugin.CloudStorageSubDirectory.Value)
                : "CENI_RELOAD";
            if (string.IsNullOrWhiteSpace(sub) || sub == "unknown") sub = "CENI_RELOAD";
            return new UPath($"/{sub}/Vault");
        }

        private static UPath GetCloudBucketDir(string profileKey, string bucketKey)
        {
            return GetCloudVaultRoot() / SanitizePathSegment(profileKey) / SanitizePathSegment(bucketKey);
        }

        private static UPath GetCloudSlotPath(string profileKey, string bucketKey, int slotIndex)
        {
            return GetCloudBucketDir(profileKey, bucketKey) / $"slot_{slotIndex}{FileExtension}";
        }

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
            var chars = input.Trim().Select(c => invalid.Contains(c) || c == '.' || c == '/' || c == '\\' ? '_' : c).ToArray();
            string cleaned = new string(chars);
            return string.IsNullOrWhiteSpace(cleaned) ? "unknown" : cleaned;
        }

        private static string GetLocalBucketDirectory(string profileKey, string bucketKey)
        {
            return IOPath.Combine(LocalVaultRoot, SanitizePathSegment(profileKey), SanitizePathSegment(bucketKey));
        }

        private static string GetLocalSlotFilePath(string profileKey, string bucketKey, int slotIndex)
        {
            return IOPath.Combine(GetLocalBucketDirectory(profileKey, bucketKey), $"slot_{slotIndex}{FileExtension}");
        }

        private static bool WriteSlotJson(string profileKey, string bucketKey, int slotIndex, string json)
        {
            if (IsUsingCloudStorage())
            {
                var fs = RoR2Application.cloudStorage;
                UPath dir = GetCloudBucketDir(profileKey, bucketKey);
                UPath filePath = GetCloudSlotPath(profileKey, bucketKey, slotIndex);
                if (!fs.DirectoryExists(dir))
                {
                    fs.CreateDirectory(dir);
                }
                if (fs.FileExists(filePath)) fs.CopyFile(filePath, new UPath(filePath.FullName + ".bak"), true);
                fs.WriteAllText(filePath, json, new UTF8Encoding(false, true));
                return true;
            }

            string localDir = GetLocalBucketDirectory(profileKey, bucketKey);
            Directory.CreateDirectory(localDir);

            string finalPath = GetLocalSlotFilePath(profileKey, bucketKey, slotIndex);
            AtomicCheckpointFile.Write(finalPath, json);
            return true;
        }

        private static string ReadSlotJson(string profileKey, string bucketKey, int slotIndex, bool backup = false)
        {
            if (IsUsingCloudStorage())
            {
                var fs = RoR2Application.cloudStorage;
                UPath path = GetCloudSlotPath(profileKey, bucketKey, slotIndex);
                if (backup) path = new UPath(path.FullName + ".bak");
                if (!fs.FileExists(path)) return null;
                return fs.ReadAllText(path, new UTF8Encoding(false, true));
            }

            string localPath = GetLocalSlotFilePath(profileKey, bucketKey, slotIndex) + (backup ? ".bak" : "");
            if (!File.Exists(localPath)) return null;
            return File.ReadAllText(localPath, new UTF8Encoding(false, true));
        }

        private static bool SlotFileExists(string profileKey, string bucketKey, int slotIndex)
        {
            if (IsUsingCloudStorage())
            {
                var path = GetCloudSlotPath(profileKey, bucketKey, slotIndex);
                return RoR2Application.cloudStorage.FileExists(path) || RoR2Application.cloudStorage.FileExists(new UPath(path.FullName + ".bak"));
            }
            string pathLocal = GetLocalSlotFilePath(profileKey, bucketKey, slotIndex);
            return File.Exists(pathLocal) || File.Exists(pathLocal + ".bak");
        }

        private static void DeleteSlotFile(string profileKey, string bucketKey, int slotIndex)
        {
            if (IsUsingCloudStorage())
            {
                UPath path = GetCloudSlotPath(profileKey, bucketKey, slotIndex);
                if (RoR2Application.cloudStorage.FileExists(path))
                {
                    RoR2Application.cloudStorage.DeleteFile(path);
                }
                UPath backup = new UPath(path.FullName + ".bak");
                if (RoR2Application.cloudStorage.FileExists(backup)) RoR2Application.cloudStorage.DeleteFile(backup);
                return;
            }

            string localPath = GetLocalSlotFilePath(profileKey, bucketKey, slotIndex);
            if (File.Exists(localPath))
            {
                File.Delete(localPath);
            }
            if (File.Exists(localPath + ".bak")) File.Delete(localPath + ".bak");
        }

        public static int PickAutoSaveSlot(string profileKey, string bucketKey)
        {
            var slots = GetSurvivorSlots(profileKey, bucketKey);

            for (int i = 0; i < MaxSlotsPerSurvivor; i++)
            {
                if (slots[i] == null) return i + 1;
            }

            int oldestUnlockedSlot = NoAvailableSlot;
            DateTime oldestTime = DateTime.MaxValue;
            for (int i = 0; i < MaxSlotsPerSurvivor; i++)
            {
                var snap = slots[i];
                if (snap == null || snap.isLocked || snap.isBackupPreview) continue;

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

        public static bool AreAllSlotsLocked(string profileKey, string bucketKey)
        {
            if (string.IsNullOrEmpty(bucketKey)) return false;
            var slots = GetSurvivorSlots(profileKey, bucketKey);
            for (int i = 0; i < MaxSlotsPerSurvivor; i++)
            {
                if (slots[i] == null || !slots[i].isLocked)
                {
                    return false;
                }
            }
            return true;
        }

        public static bool HasAnyMultiplayerSnapshots(string profileKey)
        {
            var mpSlots = GetSurvivorSlots(profileKey, MultiplayerBucket);
            for (int i = 0; i < mpSlots.Length; i++)
            {
                if (mpSlots[i] != null) return true;
            }
            return false;
        }

        public static bool ToggleSlotLock(string profileKey, string bucketKey, int slotIndex, out bool newLockedState)
        {
            newLockedState = false;
            var snapshot = LoadSlot(profileKey, bucketKey, slotIndex);
            if (snapshot == null || snapshot.isBackupPreview) return false;
            if (!Core.CheckpointValidation.InspectStructure(snapshot).CanLoad) return false;

            snapshot.isLocked = !snapshot.isLocked;
            newLockedState = snapshot.isLocked;

            try
            {
                string json = JsonUtility.ToJson(snapshot, true);
                WriteSlotJson(profileKey, bucketKey, slotIndex, json);
                CeniReloadPlugin.Log?.LogInfo($"{(newLockedState ? "Locked" : "Unlocked")} checkpoint: {bucketKey} Slot {slotIndex}");
                return true;
            }
            catch (Exception ex)
            {
                CeniReloadPlugin.Log?.LogError($"Failed to toggle lock on {bucketKey} Slot {slotIndex}: {ex}");
                return false;
            }
        }

        public static SaveSlotResult SaveSnapshotWithStatus(CheckpointSnapshot snapshot, int targetSlot = -1)
        {
            if (snapshot == null || string.IsNullOrEmpty(snapshot.survivorBodyName))
                return SaveSlotResult.Failed;
            if (snapshot.isBackupPreview || !Core.CheckpointValidation.InspectStructure(snapshot).CanLoad)
                return SaveSlotResult.Failed;

            try
            {
                string profileKey = string.IsNullOrEmpty(snapshot.profileKey)
                    ? GetCurrentProfileKey()
                    : snapshot.profileKey;
                snapshot.profileKey = profileKey;

                string bucketKey = snapshot.GetStorageBucket();
                if (string.IsNullOrEmpty(bucketKey)) bucketKey = snapshot.survivorBodyName;
                snapshot.vaultBucket = bucketKey;

                int slot;
                if (targetSlot >= 1 && targetSlot <= MaxSlotsPerSurvivor)
                {
                    var existing = LoadSlot(profileKey, bucketKey, targetSlot);
                    if (existing != null && existing.isBackupPreview) return SaveSlotResult.Failed;
                    if (existing != null && existing.isLocked)
                    {
                        CeniReloadPlugin.Log?.LogWarning($"Refused to overwrite locked checkpoint: {bucketKey} Slot {targetSlot}.");
                        return SaveSlotResult.AllSlotsLocked;
                    }
                    slot = targetSlot;
                }
                else
                {
                    slot = PickAutoSaveSlot(profileKey, bucketKey);
                    if (slot == NoAvailableSlot)
                    {
                        if (GetSurvivorSlots(profileKey, bucketKey).Any(s => s != null && s.isBackupPreview)) return SaveSlotResult.Failed;
                        CeniReloadPlugin.Log?.LogWarning($"Auto-save skipped: all {MaxSlotsPerSurvivor} slots for {bucketKey} are locked.");
                        return SaveSlotResult.AllSlotsLocked;
                    }
                }

                snapshot.slotIndex = slot;
                snapshot.isLocked = false;
                snapshot.savedUtcIso = DateTime.UtcNow.ToString("O");

                string json = JsonUtility.ToJson(snapshot, true);
                WriteSlotJson(profileKey, bucketKey, slot, json);

                string storageTag = IsUsingCloudStorage() ? "Cloud" : "Local";
                CeniReloadPlugin.Log?.LogInfo($"Saved checkpoint [{storageTag}]: {bucketKey} Slot {slot} (Stage {snapshot.stageClearCount + 1}: {snapshot.stageSceneName})");
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

        private static CheckpointSnapshot ParseSlot(string json, string profileKey, string bucketKey, int slotIndex, bool backup)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            StrictJson.Validate(json);
            var snapshot = JsonUtility.FromJson<CheckpointSnapshot>(json);
            if (snapshot == null || string.IsNullOrEmpty(snapshot.stageSceneName) || snapshot.players == null || snapshot.players.Count == 0)
                throw new FormatException("Incomplete checkpoint.");
            // Storage address is authoritative; never trust an embedded slot/path.
            snapshot.slotIndex = slotIndex;
            snapshot.profileKey = profileKey;
            snapshot.vaultBucket = bucketKey;
            snapshot.isBackupPreview = backup;
            return snapshot;
        }

        public static CheckpointSnapshot LoadBackup(string profileKey, string bucketKey, int slotIndex)
        {
            if (slotIndex < 1 || slotIndex > MaxSlotsPerSurvivor || string.IsNullOrEmpty(bucketKey)) return null;
            try { return ParseSlot(ReadSlotJson(profileKey, bucketKey, slotIndex, true), profileKey, bucketKey, slotIndex, true); }
            catch (Exception ex) { CeniReloadPlugin.Log?.LogWarning($"Unreadable backup for {bucketKey} Slot {slotIndex}: {ex.Message}"); return null; }
        }

        public static CheckpointSnapshot LoadSlot(string profileKey, string bucketKey, int slotIndex)
        {
            if (slotIndex < 1 || slotIndex > MaxSlotsPerSurvivor || string.IsNullOrEmpty(bucketKey)) return null;
            try
            {
                var primary = ParseSlot(ReadSlotJson(profileKey, bucketKey, slotIndex), profileKey, bucketKey, slotIndex, false);
                if (primary != null) return primary;
            }
            catch (Exception ex) { CeniReloadPlugin.Log?.LogWarning($"Unreadable checkpoint at '{bucketKey}/slot_{slotIndex}': {ex.Message}"); }
            // Offer the readable backup in the Vault; do not silently overwrite anything.
            return LoadBackup(profileKey, bucketKey, slotIndex);
        }

        public static bool RecoverBackup(string profileKey, string bucketKey, int slotIndex)
        {
            try
            {
                var backup = LoadBackup(profileKey, bucketKey, slotIndex);
                if (backup == null || !Core.CheckpointValidation.InspectStructure(backup).CanLoad) return false;
                var current = LoadSlot(profileKey, bucketKey, slotIndex);
                if (current != null && !current.isBackupPreview && current.isLocked) return false;
                string json = ReadSlotJson(profileKey, bucketKey, slotIndex, true);
                if (IsUsingCloudStorage())
                {
                    var fs = RoR2Application.cloudStorage;
                    var path = GetCloudSlotPath(profileKey, bucketKey, slotIndex);
                    if (fs.FileExists(path)) fs.CopyFile(path, new UPath(path.FullName + ".damaged"), true);
                    fs.CopyFile(new UPath(path.FullName + ".bak"), path, true);
                }
                else
                {
                    string path = GetLocalSlotFilePath(profileKey, bucketKey, slotIndex);
                    AtomicCheckpointFile.Write(path, json, path + ".damaged");
                }
                return true;
            }
            catch (Exception ex) { CeniReloadPlugin.Log?.LogError($"Backup recovery failed: {ex}"); return false; }
        }

        public static CheckpointSnapshot[] GetSurvivorSlots(string profileKey, string bucketKey)
        {
            var result = new CheckpointSnapshot[MaxSlotsPerSurvivor];
            if (string.IsNullOrEmpty(bucketKey)) return result;

            for (int i = 1; i <= MaxSlotsPerSurvivor; i++)
            {
                result[i - 1] = LoadSlot(profileKey, bucketKey, i);
            }
            return result;
        }

        public static List<CheckpointSnapshot> GetAllProfileSnapshots(string profileKey)
        {
            var list = new List<CheckpointSnapshot>();

            try
            {
                if (IsUsingCloudStorage())
                {
                    var fs = RoR2Application.cloudStorage;
                    UPath profileDir = GetCloudVaultRoot() / SanitizePathSegment(profileKey);
                    if (fs.DirectoryExists(profileDir))
                    {
                        foreach (UPath bucketDir in fs.EnumerateDirectories(profileDir))
                        {
                            string bucketName = bucketDir.GetName();
                            foreach (var snapshot in GetSurvivorSlots(profileKey, bucketName))
                                if (snapshot != null) list.Add(snapshot);
                        }
                    }
                }
                else
                {
                    string profileDir = IOPath.Combine(LocalVaultRoot, SanitizePathSegment(profileKey));
                    if (Directory.Exists(profileDir))
                    {
                        foreach (string bucketDir in Directory.GetDirectories(profileDir))
                        {
                            string bucketName = IOPath.GetFileName(bucketDir);
                            foreach (var snapshot in GetSurvivorSlots(profileKey, bucketName))
                                if (snapshot != null) list.Add(snapshot);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CeniReloadPlugin.Log?.LogWarning($"Failed to enumerate checkpoints: {ex.Message}");
            }

            list.Sort((a, b) => string.Compare(b.savedUtcIso, a.savedUtcIso, StringComparison.Ordinal));
            return list;
        }

        public static bool DeleteSlot(string profileKey, string bucketKey, int slotIndex)
        {
            if (slotIndex < 1 || slotIndex > MaxSlotsPerSurvivor || string.IsNullOrEmpty(bucketKey)) return false;
            if (!SlotFileExists(profileKey, bucketKey, slotIndex)) return false;

            var existing = LoadSlot(profileKey, bucketKey, slotIndex);
            if (existing != null && existing.isLocked)
            {
                CeniReloadPlugin.Log?.LogWarning($"Refused to delete locked checkpoint: {bucketKey} Slot {slotIndex}");
                return false;
            }

            try
            {
                DeleteSlotFile(profileKey, bucketKey, slotIndex);
                CeniReloadPlugin.Log?.LogInfo($"Deleted checkpoint: {bucketKey} Slot {slotIndex}");
                return true;
            }
            catch (Exception ex)
            {
                CeniReloadPlugin.Log?.LogError($"Failed to delete checkpoint '{bucketKey}/slot_{slotIndex}': {ex}");
                return false;
            }
        }
    }
}
