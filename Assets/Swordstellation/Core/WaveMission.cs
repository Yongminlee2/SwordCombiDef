using System;

namespace Swordstellation.Core
{
    public enum WaveMissionKind
    {
        Summon,
        Merge,
        Forge,
        Sell,
        Ultimate
    }

    // One small goal per wave; finishing it pays gold.
    public sealed class WaveMission
    {
        public WaveMissionKind Kind { get; }
        public int Target { get; }
        public int Reward { get; }
        public int Progress { get; private set; }
        public bool IsComplete => Progress >= Target;

        public WaveMission(WaveMissionKind kind, int target, int reward)
        {
            Kind = kind;
            Target = Math.Max(1, target);
            Reward = Math.Max(0, reward);
        }

        // True only on the action that completes the mission.
        public bool Record(WaveMissionKind kind)
        {
            if (kind != Kind || IsComplete)
            {
                return false;
            }
            Progress++;
            return IsComplete;
        }
    }

    public static class WaveMissionRules
    {
        public static WaveMission Create(int wave, SeededRandom random)
        {
            WaveMissionKind[] kinds = (WaveMissionKind[])Enum.GetValues(typeof(WaveMissionKind));
            WaveMissionKind kind = kinds[random.NextInt(kinds.Length)];
            bool late = wave >= 8;
            int target = kind switch
            {
                WaveMissionKind.Summon => late ? 6 : 4,
                WaveMissionKind.Merge => late ? 3 : 2,
                WaveMissionKind.Forge => late ? 2 : 1,
                WaveMissionKind.Sell => 2,
                _ => 1
            };
            return new WaveMission(kind, target, Reward(wave));
        }

        public static int Reward(int wave)
        {
            return 25 + Math.Max(1, wave) * 6;
        }
    }
}
