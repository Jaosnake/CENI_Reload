using System;
using System.Collections.Generic;
using System.Reflection;
using Jaosnake.CeniReload.Model;
using RoR2;

namespace Jaosnake.CeniReload.Core
{
    internal static class RunRandomState
    {
        private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly FieldInfo State0 = typeof(Xoroshiro128Plus).GetField("state0", Fields);
        private static readonly FieldInfo State1 = typeof(Xoroshiro128Plus).GetField("state1", Fields);
        private static readonly Dictionary<string, FieldInfo> Generators = Discover();
        private static Dictionary<string, FieldInfo> Discover()
        {
            var result = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
            foreach (var field in typeof(Run).GetFields(Fields))
                if (field.FieldType == typeof(Xoroshiro128Plus)) result.Add(field.Name, field);
            return result;
        }
        public static List<RandomSnapshot> Capture(Run run)
        {
            var result = new List<RandomSnapshot>();
            foreach (var pair in Generators)
            {
                var rng = pair.Value.GetValue(run) as Xoroshiro128Plus;
                if (rng != null) result.Add(new RandomSnapshot { field = pair.Key, state0 = (ulong)State0.GetValue(rng), state1 = (ulong)State1.GetValue(rng) });
            }
            return result;
        }
        public static void Restore(Run run, List<RandomSnapshot> snapshots, RestoreOutcome outcome)
        {
            if (snapshots == null || snapshots.Count == 0) { outcome.Warn("CENIRELOAD_VALIDATE_LEGACY"); return; }
            foreach (var saved in snapshots)
            {
                if (saved == null || (saved.state0 | saved.state1) == 0 || !Generators.TryGetValue(saved.field ?? "", out var field))
                { outcome.Warn("CENIRELOAD_RESULT_RANDOM", saved?.field); continue; }
                var rng = field.GetValue(run) as Xoroshiro128Plus;
                if (rng == null) { rng = new Xoroshiro128Plus(1UL); field.SetValue(run, rng); }
                State0.SetValue(rng, saved.state0); State1.SetValue(rng, saved.state1);
            }
        }
    }
}
