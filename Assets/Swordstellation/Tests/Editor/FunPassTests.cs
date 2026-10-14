using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Swordstellation.Core;

namespace Swordstellation.Tests
{
    public sealed class FunPassTests
    {
        [Test]
        public void CriticalChanceGrowsWithStars()
        {
            Assert.That(RunBalanceRules.CriticalChance(1), Is.EqualTo(0.10f).Within(0.001f));
            Assert.That(
                RunBalanceRules.CriticalChance(5),
                Is.GreaterThan(RunBalanceRules.CriticalChance(1)));
        }

        [Test]
        public void SwordsLandCriticalHitsForDoubleDamage()
        {
            RunInventory inventory = new RunInventory();
            for (int index = 0; index < 4; index++)
            {
                inventory.TryAdd(new SwordInstance("ember_blade", 1, (ulong)(index + 1), $"e{index}"), out _);
            }
            CombinationDefenseSession session = Session(inventory);
            List<BattleAttackEvent> attacks = new List<BattleAttackEvent>();
            session.SwordAttacked += attacks.Add;
            for (int step = 0; step < 900; step++)
            {
                session.Advance(0.1f);
            }

            List<BattleAttackEvent> onNormal = attacks
                .Where(attack =>
                    !attack.Targets[0].Definition.IsElite &&
                    !attack.Targets[0].Definition.IsBoss)
                .ToList();
            Assume.That(onNormal.Count, Is.GreaterThan(80));
            float share = onNormal.Count(attack => attack.IsCritical) / (float)onNormal.Count;
            Assert.That(share, Is.InRange(0.04f, 0.25f));

            // Same sword, same kind of target: a crit is exactly double.
            int slot = onNormal
                .GroupBy(attack => attack.SlotIndex)
                .First(group =>
                    group.Any(attack => attack.IsCritical) &&
                    group.Any(attack => !attack.IsCritical))
                .Key;
            float critical = onNormal.First(attack => attack.SlotIndex == slot && attack.IsCritical).DamagePerTarget;
            float plain = onNormal.First(attack => attack.SlotIndex == slot && !attack.IsCritical).DamagePerTarget;
            Assert.That(critical, Is.EqualTo(plain * RunBalanceRules.CriticalMultiplier).Within(0.01f));
        }

        [Test]
        public void JackpotSummonsAreRareAndComeInAtTwoStars()
        {
            Assert.That(RunBalanceRules.JackpotSummonChance, Is.InRange(0.03f, 0.08f));
            Assert.That(RunBalanceRules.SummonStars(0.01f), Is.EqualTo(2));
            Assert.That(RunBalanceRules.SummonStars(0.5f), Is.EqualTo(1));
        }

        [Test]
        public void BossTreasureOffersThreeDifferentRewards()
        {
            for (ulong seed = 1; seed < 20; seed++)
            {
                IReadOnlyList<BossTreasure> options = BossTreasureRules.Draw(new SeededRandom(seed));
                Assert.That(options.Count, Is.EqualTo(3));
                Assert.That(options.Distinct().Count(), Is.EqualTo(3));
            }
        }

        private static CombinationDefenseSession Session(RunInventory inventory)
        {
            ContentCatalog catalog = new ContentCatalog();
            return new CombinationDefenseSession(
                catalog,
                catalog.GetStage("stage_starlit_forge"),
                RunMode.Standard,
                inventory,
                4242,
                0,
                150,
                enemyLimit: 1000);
        }
    }
}
