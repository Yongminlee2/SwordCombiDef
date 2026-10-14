using System;

namespace Swordstellation.Core
{
    [Serializable]
    public sealed class HeroSkillStats
    {
        public float CooldownSeconds;
        public float DamageBoost;
        public float DamageBoostDuration;
        public int ForgeEnergy;
        public int CoreRepair;
    }

    public static class HeroSkillRules
    {
        public const string SkillId = "celestial_forge_command";

        public static HeroSkillStats Calculate(
            int playerLevel,
            int smithingLevel,
            int swordControlLevel,
            int guardianshipLevel)
        {
            int safePlayerLevel = Math.Max(1, playerLevel);
            int safeSmithing = Math.Max(0, smithingLevel);
            int safeSwordControl = Math.Max(0, swordControlLevel);
            int safeGuardianship = Math.Max(0, guardianshipLevel);

            return new HeroSkillStats
            {
                CooldownSeconds = Math.Max(
                    28f,
                    46f - safePlayerLevel * 0.18f - safeSwordControl * 0.35f),
                DamageBoost = 0.24f + safeSwordControl * 0.012f,
                DamageBoostDuration = 6f + safePlayerLevel * 0.04f,
                ForgeEnergy = 18 + safeSmithing * 2,
                CoreRepair = 2 + safeGuardianship / 4
            };
        }
    }
}
