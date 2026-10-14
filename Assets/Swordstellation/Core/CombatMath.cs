using System;

namespace Swordstellation.Core
{
    public static class CombatMath
    {
        public static float SwordDamage(
            SwordDefinition definition,
            SwordInstance instance,
            int swordControlLevel,
            RunModifierState modifiers,
            bool isElite,
            bool isBoss,
            int swordMasteryLevel = 0)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            modifiers ??= new RunModifierState();
            float styleMultiplier = definition.AttackStyle switch
            {
                AttackStyle.Dash => 1.18f,
                AttackStyle.Chain => 0.92f,
                AttackStyle.Area => 0.78f,
                _ => 1f
            };
            float enemyMultiplier = isBoss
                ? modifiers.BossDamageMultiplier
                : isElite
                    ? modifiers.EliteDamageMultiplier
                    : 1f;

            return Math.Max(
                0f,
                definition.BaseDamage *
                RunBalanceRules.StarDamageMultiplier(instance.Stars) *
                RunBalanceRules.SwordControlDamageMultiplier(swordControlLevel) *
                RunBalanceRules.SwordMasteryDamageMultiplier(
                    swordMasteryLevel) *
                modifiers.DamageMultiplier *
                styleMultiplier *
                enemyMultiplier);
        }

        public static float AttackCooldown(
            SwordDefinition definition,
            int swordControlLevel,
            RunModifierState modifiers)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            modifiers ??= new RunModifierState();
            float speed =
                RunBalanceRules.SwordControlSpeedMultiplier(swordControlLevel) *
                modifiers.AttackSpeedMultiplier;
            return Math.Max(0.16f, definition.AttackInterval / speed);
        }

        public static int TargetCount(
            SwordDefinition definition,
            RunModifierState modifiers,
            int aliveCount)
        {
            if (definition == null || aliveCount <= 0)
            {
                return 0;
            }

            modifiers ??= new RunModifierState();
            return Math.Min(
                aliveCount,
                Math.Max(1, definition.TargetCount + modifiers.ExtraTargets));
        }
    }
}
