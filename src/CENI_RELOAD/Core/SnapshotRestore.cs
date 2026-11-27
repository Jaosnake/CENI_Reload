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
        public static CheckpointSnapshot ActiveSnapshot { get; private set; }
        public static bool CanRestorePlayers => PendingSnapshot != null || ActiveSnapshot != null;
        public static bool WaitingForLobby { get; private set; }
        public static RestoreOutcome LastOutcome { get; private set; }
        private static RestoreOutcome _outcome;
        private static bool _restoringStageStarted;
        public static void MarkRestoreStageStarted() { _restoringStageStarted = true; }
        public static bool IsPrepared(CheckpointSnapshot snapshot) => WaitingForLobby && PendingSnapshot != null && snapshot != null &&
            PendingSnapshot.slotIndex == snapshot.slotIndex && PendingSnapshot.GetStorageBucket() == snapshot.GetStorageBucket();
        public static void CancelPrepared() { if (WaitingForLobby) ClearPending(); }

        public static CheckpointValidation ValidateCheckpoint(CheckpointSnapshot snapshot)
        {
            var result = CheckpointValidation.Inspect(snapshot);
            if (!result.CanLoad) return result;
            if (DeserializeRuleBook(snapshot.ruleBookBase64) == null) result.Add("CENIRELOAD_VALIDATE_DATA", "RuleBook", true);
            foreach (var player in snapshot.players)
                if (CheckpointValidation.IsRequiredPlayer(snapshot, player) && DeserializeLoadout(player.loadoutBase64) == null)
                    result.Add("CENIRELOAD_VALIDATE_DATA", "Loadout", true);
            return result;
        }

        public static bool BeforeLaunch(PreGameController preGame)
        {
            if (PendingSnapshot == null) return true;
            var result = ValidateCheckpoint(PendingSnapshot);
            if (!result.CanLoad)
            {
                foreach (var issue in result.Issues) Chat.AddMessage("[CENI RELOAD] " + issue);
                WaitingForLobby = true;
                return false;
            }
            _outcome = new RestoreOutcome();
            foreach (var issue in result.Issues) _outcome.Details.Add(issue.Key, issue.Detail, issue.Blocking);
            WaitingForLobby = false;
            ApplyPreGameSettings(preGame, PendingSnapshot);
            return true;
        }
        public static bool AutoLaunchOnPreGame { get; private set; }
        public static bool StageSceneApplied { get; private set; }
        private static readonly HashSet<CharacterMaster> FreshMasters = new HashSet<CharacterMaster>();
        private static readonly HashSet<CharacterMaster> LateMasters = new HashSet<CharacterMaster>();
        private static readonly PlayerRestoreSession Participants = new PlayerRestoreSession();
        private static readonly Dictionary<CharacterMaster, PlayerSnapshot> _matchedMasters = new Dictionary<CharacterMaster, PlayerSnapshot>();

        public static void ClearPending()
        {
            PendingSnapshot = null;
            ActiveSnapshot = null;
            WaitingForLobby = false;
            _outcome = null;
            _restoringStageStarted = false;
            AutoLaunchOnPreGame = false;
            StageSceneApplied = false;
            Participants.Clear();
            FreshMasters.Clear();
            LateMasters.Clear();
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
            var check = ValidateCheckpoint(snapshot);
            if (!check.CanLoad) { CeniReloadPlugin.Log?.LogWarning(check.ToString()); return false; }
            if (snapshot.isBackupPreview) return false;

            var preGame = PreGameController.instance;
            if (preGame == null)
            {
                CeniReloadPlugin.Log?.LogWarning("Cannot load checkpoint: PreGameController.instance is null.");
                return false;
            }

            try
            {
                bool alreadyPrepared = IsPrepared(snapshot);
                ClearPending();
                PendingSnapshot = snapshot;
                AutoLaunchOnPreGame = false;
                StageSceneApplied = false;
                Participants.Begin(snapshot);
                _matchedMasters.Clear();

                ApplyPreGameSettings(preGame, snapshot);

                if (snapshot.IsMultiplayer() && !alreadyPrepared)
                {
                    WaitingForLobby = true;
                    Chat.AddMessage("[CENI RELOAD] " + CeniReloadStrings.Get("CENIRELOAD_LOBBY_WAITING"));
                    return true;
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

        public static bool BeginLoadFromMainMenu(CheckpointSnapshot snapshot)
        {
            if (snapshot == null) return false;
            var check = ValidateCheckpoint(snapshot);
            if (!check.CanLoad) { CeniReloadPlugin.Log?.LogWarning(check.ToString()); return false; }
            if (snapshot.isBackupPreview) return false;

            if (RoR2.Console.instance == null)
            {
                CeniReloadPlugin.Log?.LogWarning("Cannot load checkpoint from main menu: RoR2.Console.instance is null.");
                ClearPending();
                return false;
            }

            try
            {
                ClearPending();
                PendingSnapshot = snapshot;
                WaitingForLobby = snapshot.IsMultiplayer();
                AutoLaunchOnPreGame = !WaitingForLobby;
                StageSceneApplied = false;
                Participants.Begin(snapshot);
                _matchedMasters.Clear();

                string gm = "ClassicRun";
                if (!string.IsNullOrEmpty(snapshot.gameModeName) &&
                    GameModeCatalog.FindGameModeIndex(snapshot.gameModeName) != GameModeIndex.Invalid)
                {
                    gm = snapshot.gameModeName;
                }

                CeniReloadPlugin.Log?.LogInfo($"Transitioning from main menu to load checkpoint Slot {snapshot.slotIndex} ({snapshot.survivorBodyName}, Mode {gm}, Stage {snapshot.stageClearCount + 1}: {snapshot.stageSceneName})");
                RoR2.Console.instance.SubmitCmd((NetworkUser)null, $"transition_command \"gamemode {gm}; host {(snapshot.IsMultiplayer() ? 1 : 0)};\"", false);
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

            if (PendingSnapshot == null || preGame == null) yield break;
            float deadline = Time.realtimeSinceStartup + 10f;
            while (NetworkUser.readOnlyInstancesList.Count == 0 && Time.realtimeSinceStartup < deadline) yield return null;
            if (PendingSnapshot == null || preGame == null) yield break;

            AutoLaunchOnPreGame = false;

            try
            {
                var snapshot = PendingSnapshot;
                if (!ValidateCheckpoint(snapshot).CanLoad)
                {
                    CeniReloadPlugin.Log?.LogWarning("Cannot auto-launch checkpoint: validation failed.");
                    WaitingForLobby = true;
                    Chat.AddMessage("[CENI RELOAD] " + ValidateCheckpoint(snapshot));
                    yield break;
                }
                ApplyPreGameSettings(preGame, snapshot);
                if (WaitingForLobby)
                {
                    Chat.AddMessage("[CENI RELOAD] " + CeniReloadStrings.Get("CENIRELOAD_LOBBY_WAITING"));
                    UI.CeniReloadVaultController.OpenPreparedLobby();
                    yield break;
                }
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
                if (_outcome != null) { _outcome.FatalError = true; _outcome.Warn("CENIRELOAD_RESULT_ERROR", "run"); }
                CeniReloadPlugin.Log?.LogError($"Error applying checkpoint to Run.Start: {ex}");
            }
        }

        private static PlayerSnapshot MatchPlayerSnapshotForUser(CheckpointSnapshot snap, NetworkUser user)
        {
            return Participants.Claim(SnapshotCapture.BuildPlayerIdentity(user));
        }

        public static void ApplyToPlayerMaster(NetworkUser user, CharacterMaster master)
        {
            var snap = PendingSnapshot ?? ActiveSnapshot;
            if (snap == null || master == null || snap.players == null || snap.players.Count == 0 || _matchedMasters.ContainsKey(master)) return;
            bool lateJoin = PendingSnapshot == null || _restoringStageStarted;
            var playerOutcome = lateJoin ? new RestoreOutcome() : _outcome;

            var playerSnap = MatchPlayerSnapshotForUser(snap, user);
            if (playerSnap == null)
            {
                if (!Participants.IsKnown(SnapshotCapture.BuildPlayerIdentity(user)) && FreshMasters.Add(master))
                {
                    if (lateJoin) LateMasters.Add(master);
                    if (lateJoin) Chat.AddMessage("[CENI RELOAD] " + CeniReloadStrings.Format("CENIRELOAD_RESULT_NEW", CheckpointIssue.Escape(user?.userName)));
                    else if (playerOutcome != null) { playerOutcome.FreshPlayers++; playerOutcome.Warn("CENIRELOAD_RESULT_NEW", user?.userName); }
                    if (lateJoin && Run.instance != null) Run.instance.StartCoroutine(FinishFreshJoin(user, master));
                }
                return;
            }

            if (BodyCatalog.FindBodyPrefab(playerSnap.bodyName) == null || DeserializeLoadout(playerSnap.loadoutBase64) == null)
            {
                playerOutcome?.Warn("CENIRELOAD_VALIDATE_SURVIVOR", playerSnap.bodyName);
                if (lateJoin) { LastOutcome = playerOutcome; playerOutcome.Publish(); }
                return;
            }
            _matchedMasters[master] = playerSnap;
            if (lateJoin) LateMasters.Add(master);

            try
            {
                if (!string.IsNullOrEmpty(playerSnap.bodyName))
                {
                    var bodyPrefab = BodyCatalog.FindBodyPrefab(playerSnap.bodyName);
                    if (bodyPrefab != null)
                    {
                        master.PushBodyHandlingConfig(new BodyHandlingConfig(BodyCatalog.FindBodyIndex(playerSnap.bodyName), BodyHandlingPresetIndex.Initial), bodyPrefab, false);
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
                        (byte)Mathf.Clamp(playerSnap.activeEquipmentSet, 0, 255), playerOutcome);
                }
            }
            catch (Exception ex)
            {
                playerOutcome?.Warn("CENIRELOAD_RESULT_ERROR", playerSnap.bodyName);
                CeniReloadPlugin.Log?.LogError($"Error applying checkpoint to CharacterMaster: {ex}");
            }
            if (lateJoin && Run.instance != null) Run.instance.StartCoroutine(FinishLateJoin(user, master, playerSnap, snap, playerOutcome));
        }

        public static IEnumerator FinishRestoreOnStageStart(Stage stage)
        {
            var snap = PendingSnapshot;
            if (snap == null) yield break;
            if (_outcome == null) _outcome = new RestoreOutcome();

            // Stage.Start is itself a coroutine. Wait for real bodies rather than
            // assuming every survivor has finished spawning after half a second.
            float deadline = Time.realtimeSinceStartup + 10f;
            while (stage != null && Time.realtimeSinceStartup < deadline)
            {
                bool ready = _matchedMasters.Count + FreshMasters.Count > 0;
                foreach (var master in _matchedMasters.Keys)
                    if (!LateMasters.Contains(master) && (master == null || master.GetBody() == null)) ready = false;
                foreach (var master in FreshMasters)
                    if (!LateMasters.Contains(master) && (master == null || master.GetBody() == null)) ready = false;
                if (ready) break;
                yield return null;
            }
            if (stage == null || !ReferenceEquals(PendingSnapshot, snap)) yield break;
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
                        if (LateMasters.Contains(pcmc.master)) continue;

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

                        if (pcmc.master.GetBody() == null)
                        {
                            _outcome.Warn("CENIRELOAD_RESULT_BODY", pSnap.bodyName);
                            LateMasters.Add(pcmc.master);
                            Run.instance.StartCoroutine(FinishLateJoin(pcmc.networkUser, pcmc.master, pSnap, snap, new RestoreOutcome()));
                            continue;
                        }
                        if (pSnap.hasBodyPosition) RestoreBodyPosition(pcmc.master.GetBody(), pSnap.bodyFootPosition, pSnap.bodyYaw);
                        CharacterCheckpointState.Restore(pcmc.master.GetBody(), pSnap, _outcome);
                        _outcome.PlayersRestored++;

                        if (pSnap.devotionItems != null && pSnap.devotionItems.Count > 0)
                        {
                            RestoreDevotionItems(pcmc.master, pSnap.devotionItems, _outcome);
                        }

                        RestoreMinions(pcmc.master, pSnap.minions, snap.seed, _outcome);
                    }
                }

                // Scene construction uses the saved stage-entry generator. Only after
                // population/spawn do we restore the continuation streams for future events.
                _outcome.FreshPlayers = 0;
                foreach (var master in FreshMasters)
                {
                    if (LateMasters.Contains(master)) continue;
                    if (master != null && master.GetBody() != null) _outcome.FreshPlayers++;
                    else _outcome.Warn("CENIRELOAD_RESULT_BODY", "new player");
                }
                if (Run.instance != null) RunRandomState.Restore(Run.instance, snap.randomStates, _outcome);
                for (int i = 0; i < snap.players.Count; i++)
                    if (!Participants.HasClaimed(i)) _outcome.Warn("CENIRELOAD_RESULT_WAITING", snap.players[i]?.playerName);

            }
            catch (Exception ex)
            {
                _outcome.Warn("CENIRELOAD_RESULT_ERROR", "stage");
                CeniReloadPlugin.Log?.LogError($"Error finalizing checkpoint restore on stage start: {ex}");
            }
            finally
            {
                ActiveSnapshot = snap;
                PendingSnapshot = null;
                AutoLaunchOnPreGame = false;
                StageSceneApplied = false;
                LastOutcome = _outcome;
                _outcome.Publish();
                _outcome = null;
            }
        }

        public static void AppendWaitingPlayers(CheckpointSnapshot next)
        {
            if (ActiveSnapshot != null) Participants.AppendUnclaimedTo(next);
        }

        private static IEnumerator EnsureLateBody(NetworkUser user, CharacterMaster master)
        {
            float deadline = Time.realtimeSinceStartup + 60f;
            bool attemptedSpawn = false;
            while (user != null && master != null && Run.instance != null && master.GetBody() == null && Time.realtimeSinceStartup < deadline)
            {
                var stage = Stage.instance;
                if (!attemptedSpawn && stage != null && !stage.sceneDef.suppressPlayerEntry && user.serverIsClientLoaded)
                {
                    attemptedSpawn = true;
                    stage.RespawnCharacter(master);
                }
                yield return null;
            }
        }

        private static IEnumerator FinishFreshJoin(NetworkUser user, CharacterMaster master)
        {
            yield return EnsureLateBody(user, master);
            if (master == null || master.GetBody() == null)
            {
                var report = new RestoreOutcome { FatalError = true };
                report.Warn("CENIRELOAD_RESULT_BODY", user?.userName);
                LastOutcome = report; report.Publish();
            }
        }

        private static IEnumerator FinishLateJoin(NetworkUser user, CharacterMaster master, PlayerSnapshot player, CheckpointSnapshot source, RestoreOutcome outcome)
        {
            yield return EnsureLateBody(user, master);
            try
            {
                if (master == null || master.GetBody() == null) { outcome.Warn("CENIRELOAD_RESULT_BODY", player.bodyName); yield break; }
                CharacterCheckpointState.Restore(master.GetBody(), player, outcome);
                // Late joins always use the current stage's normal spawn, never old coordinates.
                var minions = player.ForLaterStage().minions;
                RestoreMinions(master, minions, source.seed, outcome);
                if (player.devotionItems != null && player.devotionItems.Count > 0) RestoreDevotionItems(master, player.devotionItems, outcome);
                outcome.PlayersRestored++;
                outcome.Warn("CENIRELOAD_RESULT_LATE", player.playerName);
            }
            catch (Exception ex) { outcome.Warn("CENIRELOAD_RESULT_ERROR", player.bodyName); CeniReloadPlugin.Log?.LogError(ex); }
            finally { LastOutcome = outcome; outcome.Publish(); }
        }

        private static bool IsFinite(Vector3 position)
        {
            return !float.IsNaN(position.x) && !float.IsInfinity(position.x) &&
                   !float.IsNaN(position.y) && !float.IsInfinity(position.y) &&
                   !float.IsNaN(position.z) && !float.IsInfinity(position.z);
        }

        private static void RestoreBodyPosition(CharacterBody body, Vector3 footPosition, float yaw)
        {
            if (body == null || !IsFinite(footPosition) || float.IsNaN(yaw) || float.IsInfinity(yaw)) return;
            TeleportHelper.TeleportBody(new TeleportHelper.TeleportBodyArgs
            {
                body = body,
                targetPosition = footPosition,
                targetRotation = Quaternion.Euler(0f, yaw, 0f),
                teleportMinions = false
            });
            // This game's TeleportBody overload moves the body but does not apply
            // targetRotation. Restore facing explicitly as well.
            body.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            if (body.characterDirection != null) body.characterDirection.yaw = yaw;
        }

        private static bool IsSkillManagedMinion(CharacterMaster owner, GameObject prefab)
        {
            var body = owner.GetBody();
            if (body == null) return false;
            foreach (var skill in body.GetComponents<GenericSkill>())
            {
                if (skill.skillDef is RoR2.Skills.EnsureDroneMasterSkillDef droneSkill && droneSkill.masterPrefab == prefab)
                    return true;
            }
            return false;
        }

        private static void RestoreMinions(CharacterMaster owner, List<MinionSnapshot> minions, ulong seed, RestoreOutcome outcome)
        {
            var ownerBody = owner.GetBody();
            if (ownerBody == null || minions == null) return;
            var reused = new HashSet<CharacterMaster>();
            for (int i = 0; i < minions.Count; i++)
            {
                var saved = minions[i];
                if (saved == null || string.IsNullOrEmpty(saved.masterName)) continue;
                var prefab = MasterCatalog.FindMasterPrefab(saved.masterName);
                if (prefab == null) { outcome.Warn("CENIRELOAD_VALIDATE_ALLY", saved.masterName); continue; }

                CharacterMaster restored = null;
                foreach (var candidate in CharacterMaster.readOnlyInstancesList)
                {
                    if (candidate == null || reused.Contains(candidate) || candidate.minionOwnership?.ownerMaster != owner) continue;
                    if (string.Equals(candidate.name.Replace("(Clone)", "").Trim(), prefab.name, StringComparison.Ordinal))
                    {
                        restored = candidate;
                        break;
                    }
                }

                if (restored == null)
                {
                    // DroneTech's skill/repair queue owns these companions. It may
                    // finish after this coroutine; manually summoning creates doubles.
                    if (IsSkillManagedMinion(owner, prefab)) { outcome.Warn("CENIRELOAD_RESULT_SKILL_ALLY", saved.masterName); continue; }
                    var masterComponent = prefab.GetComponent<CharacterMaster>();
                    var bodyPrefab = masterComponent != null ? masterComponent.bodyPrefab : null;
                    var body = bodyPrefab != null ? bodyPrefab.GetComponent<CharacterBody>() : null;
                    if (body == null) { outcome.Warn("CENIRELOAD_RESULT_ALLY", saved.masterName); continue; }

                    Vector3 position;
                    if (saved.hasBodyPosition && IsFinite(saved.bodyFootPosition)) position = saved.bodyFootPosition;
                    else
                    {
                        // Old saves have no coordinates. Use the game's hull-aware
                        // ground/air placement instead of an arbitrary upward offset.
                        var safe = TeleportHelper.FindSafeTeleportDestination(ownerBody.footPosition, body,
                            new Xoroshiro128Plus(seed + (ulong)i));
                        if (!safe.HasValue)
                        {
                            outcome.Warn("CENIRELOAD_RESULT_ALLY", saved.masterName);
                            CeniReloadPlugin.Log?.LogWarning($"No safe position found for checkpoint ally '{saved.masterName}'.");
                            continue;
                        }
                        position = safe.Value;
                    }
                    restored = new MasterSummon
                    {
                        masterPrefab = prefab,
                        position = position,
                        rotation = Quaternion.Euler(0f, saved.hasBodyPosition ? saved.bodyYaw : ownerBody.transform.eulerAngles.y, 0f),
                        summonerBodyObject = ownerBody.gameObject,
                        ignoreTeamMemberLimit = true,
                        useAmbientLevel = true
                    }.Perform();
                }

                if (restored == null) { outcome.Warn("CENIRELOAD_RESULT_ALLY", saved.masterName); continue; }
                outcome.AlliesRestored++;
                reused.Add(restored);
                if (saved.hasBodyPosition) RestoreBodyPosition(restored.GetBody(), saved.bodyFootPosition, saved.bodyYaw);
                if (restored.inventory != null)
                    RestoreInventory(restored.inventory, saved.items, saved.tempItems, saved.equipments, 0, 0, 0, outcome);
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
                else if (snap.simulacrumEnemyItems != null && snap.simulacrumEnemyItems.Count > 0)
                    _outcome?.Warn("CENIRELOAD_RESULT_ERROR", "Simulacrum inventory");
            }
            else if (snap.simulacrumEnemyItems != null && snap.simulacrumEnemyItems.Count > 0)
                _outcome?.Warn("CENIRELOAD_RESULT_ERROR", "Simulacrum inventory");

            if (snap.evolutionMonsterItems != null && snap.evolutionMonsterItems.Count > 0 &&
                EvolutionInventoryField?.GetValue(null) is Inventory monsterInv && monsterInv != null)
            {
                ReplacePermanentInventory(monsterInv, snap.evolutionMonsterItems);
                if (EvolutionIteratorField != null)
                {
                    EvolutionIteratorField.SetValue(null, snap.evolutionItemIterator);
                }
            }
            else if (snap.evolutionMonsterItems != null && snap.evolutionMonsterItems.Count > 0)
                _outcome?.Warn("CENIRELOAD_RESULT_ERROR", "Evolution inventory");
        }

        private static void RestoreDevotionItems(CharacterMaster master, List<ItemEntry> devotionItems, RestoreOutcome outcome)
        {
            if (master == null || devotionItems == null || devotionItems.Count == 0) return;
            var list = DevotionInstanceListField?.GetValue(null) as List<DevotionInventoryController>;
            if (list == null) { outcome?.Warn("CENIRELOAD_RESULT_ERROR", "Devotion inventory"); return; }

            for (int i = 0; i < list.Count; i++)
            {
                var ctrl = list[i];
                if (ctrl == null || ctrl.SummonerMaster != master) continue;

                if (DevotionInventoryField?.GetValue(ctrl) is Inventory devInv && devInv != null)
                {
                    ReplacePermanentInventory(devInv, devotionItems);
                }
                else outcome?.Warn("CENIRELOAD_RESULT_ERROR", "Devotion inventory");
                return;
            }
            outcome?.Warn("CENIRELOAD_RESULT_ERROR", "Devotion inventory");
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
            byte activeSet, RestoreOutcome outcome = null)
        {
            if (inventory == null) { outcome?.Warn("CENIRELOAD_RESULT_ERROR", "inventory"); return; }

            if (items != null)
            {
                var targets = new Dictionary<ItemIndex, int>();
                for (int i = 0; i < items.Count; i++)
                {
                    var entry = items[i];
                    if (entry == null || entry.count <= 0 || string.IsNullOrEmpty(entry.itemName)) continue;

                    var itemIndex = ItemCatalog.FindItemIndex(entry.itemName);
                    if (itemIndex == ItemIndex.None || ItemCatalog.GetItemDef(itemIndex) == null) { outcome?.Warn("CENIRELOAD_VALIDATE_ITEM", entry.itemName); continue; }
                    if (!targets.ContainsKey(itemIndex)) targets.Add(itemIndex, entry.count);
                }
                for (int i = 0; i < ItemCatalog.itemCount; i++)
                {
                    var itemIndex = (ItemIndex)i;
                    targets.TryGetValue(itemIndex, out int target);
                    int delta = target - inventory.GetItemCountPermanent(itemIndex);
                    if (delta > 0)
                    {
                        inventory.GiveItemPermanent(itemIndex, delta);
                    }
                    else if (delta < 0) inventory.RemoveItemPermanent(itemIndex, -delta);
                }
            }

            if (tempItems != null)
            {
                var targets = new Dictionary<ItemIndex, float>();
                for (int i = 0; i < tempItems.Count; i++)
                {
                    var temp = tempItems[i];
                    if (temp == null || string.IsNullOrEmpty(temp.itemName) ||
                        float.IsNaN(temp.rawValue) || float.IsInfinity(temp.rawValue) || temp.rawValue <= 0.001f)
                    {
                        continue;
                    }

                    var itemIdx = ItemCatalog.FindItemIndex(temp.itemName);
                    if (itemIdx == ItemIndex.None || ItemCatalog.GetItemDef(itemIdx) == null) { outcome?.Warn("CENIRELOAD_VALIDATE_ITEM", temp.itemName); continue; }
                    if (!targets.ContainsKey(itemIdx)) targets.Add(itemIdx, temp.rawValue);
                }
                for (int i = 0; i < ItemCatalog.itemCount; i++)
                {
                    var itemIdx = (ItemIndex)i;
                    targets.TryGetValue(itemIdx, out float target);
                    float deltaTemp = target - inventory.GetTempItemRawValue(itemIdx);
                    if (deltaTemp > 0.001f)
                    {
                        inventory.GiveItemTemp(itemIdx, deltaTemp);
                    }
                    else if (deltaTemp < -0.001f) inventory.RemoveItemTemp(itemIdx, -deltaTemp);
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
                    if (eqIdx == EquipmentIndex.None) { outcome?.Warn("CENIRELOAD_VALIDATE_EQUIPMENT", eq.equipmentName); continue; }
                    if (eq.slot >= inventory.GetEquipmentSlotCount() || eq.set >= inventory.GetEquipmentSetCount())
                    { outcome?.Warn("CENIRELOAD_VALIDATE_EQUIPMENT", eq.equipmentName); continue; }

                    var chargeFinish = eq.cooldownRemaining > 0.01f
                        ? Run.FixedTimeStamp.now + eq.cooldownRemaining
                        : Run.FixedTimeStamp.negativeInfinity;

                    var state = new EquipmentState(eqIdx, chargeFinish, eq.charges, false);
#pragma warning disable CS0618
                    inventory.SetEquipment(state, eq.slot, eq.set);
#pragma warning restore CS0618
                }
            }

            if (activeSlot < inventory.GetEquipmentSlotCount()) inventory.SetActiveEquipmentSlot(activeSlot);
            else outcome?.Warn("CENIRELOAD_RESULT_ERROR", "active equipment slot");
            if (activeSet < inventory.GetEquipmentSetCount()) inventory.SetActiveEquipmentSet(activeSet);
            else outcome?.Warn("CENIRELOAD_RESULT_ERROR", "active equipment set");
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
