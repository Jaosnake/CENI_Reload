using Jaosnake.CeniReload.Model;
using RoR2;
using UnityEngine;

namespace Jaosnake.CeniReload.Core
{
    internal static class CharacterCheckpointState
    {
        public static void Capture(CharacterBody body, PlayerSnapshot saved)
        {
            if (body == null) return;
            var hp = body.healthComponent;
            if (hp != null && hp.alive)
            {
                saved.hasVitalState = true; saved.health = hp.health; saved.shield = hp.shield; saved.barrier = hp.barrier;
            }
            var skills = body.GetComponents<GenericSkill>();
            for (int i = 0; i < skills.Length; i++)
            {
                var skill = skills[i];
                if (skill.skillDef == null) continue;
                saved.skills.Add(new SkillSnapshot { componentIndex = i, skillName = skill.skillName,
                    definitionName = ((ScriptableObject)skill.skillDef).name, stock = skill.stock, rechargeStopwatch = skill.rechargeStopwatch });
            }
        }
        public static void Restore(CharacterBody body, PlayerSnapshot saved, RestoreOutcome outcome)
        {
            if (body == null) { outcome.Warn("CENIRELOAD_RESULT_BODY", saved.bodyName); return; }
            body.RecalculateStats();
            var hp = body.healthComponent;
            if (saved.hasVitalState && hp != null)
            {
                hp.Networkhealth = Mathf.Clamp(saved.health, 1f, hp.fullHealth);
                hp.Networkshield = Mathf.Clamp(saved.shield, 0f, hp.fullShield);
                hp.Networkbarrier = Mathf.Clamp(saved.barrier, 0f, hp.fullCombinedHealth);
            }
            var skills = body.GetComponents<GenericSkill>();
            if (!Util.HasEffectiveAuthority(body.gameObject))
            {
                if (saved.skills != null && saved.skills.Count > 0) outcome.Warn("CENIRELOAD_RESULT_REMOTE_SKILLS", saved.playerName);
                return;
            }
            if (saved.skills == null) return;
            foreach (var state in saved.skills)
            {
                if (state == null || state.componentIndex < 0 || state.componentIndex >= skills.Length)
                { outcome.Warn("CENIRELOAD_RESULT_SKILL", state?.skillName); continue; }
                var skill = skills[state.componentIndex];
                if (skill.skillName != state.skillName || skill.skillDef == null || ((ScriptableObject)skill.skillDef).name != state.definitionName)
                { outcome.Warn("CENIRELOAD_RESULT_SKILL", state.skillName); continue; }
                skill.stock = Mathf.Clamp(state.stock, 0, skill.maxStock);
                skill.rechargeStopwatch = Mathf.Max(0f, state.rechargeStopwatch);
            }
        }
    }
}
