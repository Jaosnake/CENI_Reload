using System.Reflection;
using Jaosnake.CeniReload.Model;
using RoR2;

namespace Jaosnake.CeniReload.Core
{
    internal static class StageRandomState
    {
        private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly FieldInfo Generator = typeof(Run).GetField("stageRngGenerator", Fields);
        private static readonly FieldInfo State0 = typeof(Xoroshiro128Plus).GetField("state0", Fields);
        private static readonly FieldInfo State1 = typeof(Xoroshiro128Plus).GetField("state1", Fields);
        private static Run _run;
        private static ulong _state0;
        private static ulong _state1;

        public static void BeforeGenerate(Run run, CheckpointSnapshot saved)
        {
            // Capture the generator BEFORE it is consumed for scene generation.
            // The state at checkpoint time has already been advanced by the stage.
            var rng = Generator.GetValue(run) as Xoroshiro128Plus;
            if (rng == null) return;
            if (saved != null && saved.hasStageRandomState && (saved.stageRandomState0 | saved.stageRandomState1) != 0UL)
            {
                State0.SetValue(rng, saved.stageRandomState0);
                State1.SetValue(rng, saved.stageRandomState1);
            }
            _run = run;
            _state0 = (ulong)State0.GetValue(rng);
            _state1 = (ulong)State1.GetValue(rng);
        }

        public static void Capture(Run run, CheckpointSnapshot snapshot)
        {
            if (!ReferenceEquals(_run, run)) return;
            snapshot.hasStageRandomState = true;
            snapshot.stageRandomState0 = _state0;
            snapshot.stageRandomState1 = _state1;
        }
    }
}
