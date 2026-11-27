using System;
using System.Collections.Generic;
using Jaosnake.CeniReload.Model;
using Jaosnake.CeniReload.Localization;
using RoR2;

namespace Jaosnake.CeniReload.Core
{
    public sealed class CheckpointIssue
    {
        public string Key;
        public string Detail;
        public bool Blocking;
        private static readonly Dictionary<string, string> DataLabels = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["checkpoint"] = "CENIRELOAD_DATA_0",
            ["stage/run"] = "CENIRELOAD_DATA_1",
            ["players"] = "CENIRELOAD_DATA_2",
            ["player"] = "CENIRELOAD_DATA_3",
            ["co-op identity"] = "CENIRELOAD_DATA_4",
            ["player identity"] = "CENIRELOAD_DATA_5",
            ["RuleBook"] = "CENIRELOAD_DATA_6",
            ["Loadout"] = "CENIRELOAD_DATA_7",
            ["player position"] = "CENIRELOAD_DATA_8",
            ["items"] = "CENIRELOAD_DATA_9",
            ["temporary items"] = "CENIRELOAD_DATA_10",
            ["luck"] = "CENIRELOAD_DATA_11",
            ["health/shield/barrier"] = "CENIRELOAD_DATA_12",
            ["skills"] = "CENIRELOAD_DATA_13",
            ["ally"] = "CENIRELOAD_DATA_14",
            ["ally position"] = "CENIRELOAD_DATA_15",
            ["equipment"] = "CENIRELOAD_DATA_16",
            ["random state"] = "CENIRELOAD_DATA_17",
            ["run"] = "CENIRELOAD_DATA_18",
            ["stage"] = "CENIRELOAD_DATA_19",
            ["Simulacrum inventory"] = "CENIRELOAD_DATA_20",
            ["Evolution inventory"] = "CENIRELOAD_DATA_21",
            ["Devotion inventory"] = "CENIRELOAD_DATA_22",
            ["inventory"] = "CENIRELOAD_DATA_23",
            ["active equipment slot"] = "CENIRELOAD_DATA_24",
            ["active equipment set"] = "CENIRELOAD_DATA_25",
        };
        public override string ToString()
        {
            string detail = Detail;
            if ((Key == "CENIRELOAD_VALIDATE_DATA" || Key == "CENIRELOAD_RESULT_ERROR") && DataLabels.TryGetValue(detail ?? "", out var label)) detail = CeniReloadStrings.Get(label);
            return CeniReloadStrings.Format(Key, Escape(detail));
        }
        internal static string Escape(string value) => (value ?? "").Replace("<", "‹").Replace(">", "›");
    }

    public sealed class CheckpointValidation
    {
        public readonly List<CheckpointIssue> Issues = new List<CheckpointIssue>();
        public bool CanLoad => !Issues.Exists(i => i.Blocking);
        public void Add(string key, string detail = "", bool blocking = false)
        {
            if (!Issues.Exists(i => i.Key == key && i.Detail == detail)) Issues.Add(new CheckpointIssue { Key = key, Detail = detail, Blocking = blocking });
        }
        public override string ToString() => string.Join("\n", Issues.ConvertAll(i => "• " + i));

        public static CheckpointValidation Inspect(CheckpointSnapshot snapshot)
        {
            var result = InspectStructure(snapshot);
            if (!result.CanLoad) return result;
            if (SceneCatalog.GetSceneDefFromSceneName(snapshot.stageSceneName) == null) result.Add("CENIRELOAD_VALIDATE_SCENE", snapshot.stageSceneName, true);
            if (GameModeCatalog.FindGameModeIndex(snapshot.gameModeName) == GameModeIndex.Invalid) result.Add("CENIRELOAD_VALIDATE_MODE", snapshot.gameModeName, true);
            if (!BodyCatalog.availability.available) result.Add("CENIRELOAD_VALIDATE_CATALOG", "", true);
            else foreach (var player in snapshot.players)
            {
                if (IsRequiredPlayer(snapshot, player) && BodyCatalog.FindBodyPrefab(player.bodyName) == null)
                    result.Add("CENIRELOAD_VALIDATE_SURVIVOR", player.bodyName, true);
            }
            if (snapshot.HasModMismatch()) result.Add("CENIRELOAD_UI_MOD_MISMATCH");
            foreach (var player in snapshot.players)
            {
                CheckItems(result, player.items); CheckTempItems(result, player.tempItems); CheckEquipment(result, player.equipments);
                CheckItems(result, player.devotionItems);
                if (player.minions == null) continue;
                foreach (var ally in player.minions)
                {
                    if (ally == null) continue;
                    if (MasterCatalog.FindMasterPrefab(ally.masterName) == null) result.Add("CENIRELOAD_VALIDATE_ALLY", ally.masterName);
                    CheckItems(result, ally.items); CheckTempItems(result, ally.tempItems); CheckEquipment(result, ally.equipments);
                }
            }
            CheckItems(result, snapshot.evolutionMonsterItems); CheckItems(result, snapshot.simulacrumEnemyItems);
            return result;
        }

        internal static bool IsRequiredPlayer(CheckpointSnapshot snapshot, PlayerSnapshot player)
        {
            if (!snapshot.IsMultiplayer()) return true;
            if (PreGameController.instance == null) return false;
            foreach (var user in NetworkUser.readOnlyInstancesList)
                if (string.Equals(player.playerId, SnapshotCapture.BuildPlayerIdentity(user), StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        // Kept independent of Unity's catalogs for malformed/legacy-save checks.
        public static CheckpointValidation InspectStructure(CheckpointSnapshot snapshot)
        {
            var result = new CheckpointValidation();
            if (snapshot == null) { result.Add("CENIRELOAD_VALIDATE_DATA", "checkpoint", true); return result; }
            if (snapshot.schemaVersion < 1 || snapshot.schemaVersion > 3) result.Add("CENIRELOAD_VALIDATE_SCHEMA", snapshot.schemaVersion.ToString(), true);
            if (string.IsNullOrEmpty(snapshot.stageSceneName) || string.IsNullOrEmpty(snapshot.gameModeName) ||
                snapshot.stageClearCount < 0 || snapshot.loopClearCount < 0 || !Finite(snapshot.runFixedTime) || snapshot.runFixedTime < 0)
                result.Add("CENIRELOAD_VALIDATE_DATA", "stage/run", true);
            if (snapshot.players == null || snapshot.players.Count == 0) result.Add("CENIRELOAD_VALIDATE_DATA", "players", true);
            CheckBase64(result, snapshot.ruleBookBase64, "RuleBook");
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (snapshot.players != null) foreach (var player in snapshot.players)
            {
                if (player == null || string.IsNullOrEmpty(player.bodyName)) { result.Add("CENIRELOAD_VALIDATE_DATA", "player", true); continue; }
                if (snapshot.IsMultiplayer() && string.IsNullOrEmpty(player.playerId)) result.Add("CENIRELOAD_VALIDATE_DATA", "co-op identity", true);
                if (!string.IsNullOrEmpty(player.playerId) && !ids.Add(player.playerId)) result.Add("CENIRELOAD_VALIDATE_DATA", "player identity", true);
                CheckBase64(result, player.loadoutBase64, "Loadout");
                if (player.hasBodyPosition && (!Finite(player.bodyFootPosition.x) || !Finite(player.bodyFootPosition.y) || !Finite(player.bodyFootPosition.z) || !Finite(player.bodyYaw)))
                    result.Add("CENIRELOAD_VALIDATE_DATA", "player position", true);
                ValidateInventory(result, player.items, player.tempItems, player.equipments);
                ValidateInventory(result, player.devotionItems, null, null);
                if (!Finite(player.luck)) result.Add("CENIRELOAD_VALIDATE_DATA", "luck", true);
                if (player.hasVitalState && (!Finite(player.health) || !Finite(player.shield) || !Finite(player.barrier) || player.health <= 0 || player.shield < 0 || player.barrier < 0))
                    result.Add("CENIRELOAD_VALIDATE_DATA", "health/shield/barrier", true);
                if (player.skills != null && player.skills.Exists(skill => skill == null || skill.componentIndex < 0 || skill.stock < 0 || !Finite(skill.rechargeStopwatch) || skill.rechargeStopwatch < 0))
                    result.Add("CENIRELOAD_VALIDATE_DATA", "skills", true);
                if (player.minions != null) foreach (var ally in player.minions)
                {
                    if (ally == null || string.IsNullOrEmpty(ally.masterName)) { result.Add("CENIRELOAD_VALIDATE_DATA", "ally", true); continue; }
                    ValidateInventory(result, ally.items, ally.tempItems, ally.equipments);
                    if (ally.hasBodyPosition && (!Finite(ally.bodyFootPosition.x) || !Finite(ally.bodyFootPosition.y) || !Finite(ally.bodyFootPosition.z) || !Finite(ally.bodyYaw)))
                        result.Add("CENIRELOAD_VALIDATE_DATA", "ally position", true);
                }
            }
            ValidateInventory(result, snapshot.evolutionMonsterItems, null, null);
            ValidateInventory(result, snapshot.simulacrumEnemyItems, null, null);
            var generators = new HashSet<string>(StringComparer.Ordinal);
            if (snapshot.randomStates != null) foreach (var rng in snapshot.randomStates)
                if (rng == null || string.IsNullOrEmpty(rng.field) || !generators.Add(rng.field) || (rng.state0 | rng.state1) == 0UL)
                    result.Add("CENIRELOAD_VALIDATE_DATA", "random state", true);
            if (snapshot.schemaVersion < 3) result.Add("CENIRELOAD_VALIDATE_LEGACY");
            return result;
        }
        internal static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static void ValidateInventory(CheckpointValidation result, List<ItemEntry> items, List<TempItemEntry> temporary, List<EquipmentEntry> equipment)
        {
            if (items != null && items.Exists(i => i == null || string.IsNullOrEmpty(i.itemName) || i.count < 0)) result.Add("CENIRELOAD_VALIDATE_DATA", "items", true);
            if (temporary != null && temporary.Exists(i => i == null || string.IsNullOrEmpty(i.itemName) || !Finite(i.rawValue) || i.rawValue < 0)) result.Add("CENIRELOAD_VALIDATE_DATA", "temporary items", true);
            if (equipment != null && equipment.Exists(i => i == null || string.IsNullOrEmpty(i.equipmentName) || i.slot > 255 || i.set > 255 || !Finite(i.cooldownRemaining) || i.cooldownRemaining < 0))
                result.Add("CENIRELOAD_VALIDATE_DATA", "equipment", true);
        }
        private static void CheckBase64(CheckpointValidation result, string value, string label)
        {
            try { if (string.IsNullOrEmpty(value)) throw new FormatException(); Convert.FromBase64String(value); }
            catch (FormatException) { result.Add("CENIRELOAD_VALIDATE_DATA", label, true); }
        }
        private static void CheckItems(CheckpointValidation result, List<ItemEntry> items)
        {
            if (items != null) foreach (var item in items)
                if (item != null && ItemCatalog.FindItemIndex(item.itemName) == ItemIndex.None) result.Add("CENIRELOAD_VALIDATE_ITEM", item.itemName);
        }
        private static void CheckTempItems(CheckpointValidation result, List<TempItemEntry> items)
        {
            if (items != null) foreach (var item in items)
                if (item != null && ItemCatalog.FindItemIndex(item.itemName) == ItemIndex.None) result.Add("CENIRELOAD_VALIDATE_ITEM", item.itemName);
        }
        private static void CheckEquipment(CheckpointValidation result, List<EquipmentEntry> items)
        {
            if (items != null) foreach (var item in items)
                if (item != null && EquipmentCatalog.FindEquipmentIndex(item.equipmentName) == EquipmentIndex.None) result.Add("CENIRELOAD_VALIDATE_EQUIPMENT", item.equipmentName);
        }
    }
}
