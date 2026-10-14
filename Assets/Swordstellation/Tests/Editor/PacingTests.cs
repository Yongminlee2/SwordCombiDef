using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Swordstellation.Core;

namespace Swordstellation.Tests
{
    public sealed class PacingTests
    {
        [Test]
        public void StandardRunsAreFifteenWavesWithThreeBosses()
        {
            Assert.That(RunBalanceRules.TotalWaves(RunMode.Standard), Is.EqualTo(15));
            Assert.That(RunBalanceRules.TotalWaves(RunMode.Daily), Is.EqualTo(15));
            Assert.That(
                Enumerable.Range(1, 15).Where(wave => RunBalanceRules.CreateWavePlan(wave).HasBoss),
                Is.EqualTo(new[] { 5, 10, 15 }));
        }

        [Test]
        public void FifteenWavesCoverTheOldThirtyWaveCurve()
        {
            Assert.That(
                RunBalanceRules.CreateWavePlan(15).NormalHealthMultiplier,
                Is.GreaterThan(RunBalanceRules.CreateWavePlan(10).NormalHealthMultiplier * 1.6f));
            Assert.That(
                RunBalanceRules.CreateWavePlan(1).SpawnCount,
                Is.GreaterThan(10));
        }

        [Test]
        public void BreaksBetweenWavesAreShort()
        {
            Assert.That(CombinationDefenseSession.IntermissionSeconds, Is.LessThanOrEqualTo(1.5f));
        }

        [Test]
        public void UnkilledBossRampagesAndCountsAsTen()
        {
            CombinationDefenseSession session = Session(new RunInventory(), 1000);
            bool rampaged = false;
            session.BossRampaged += _ => rampaged = true;
            BattleEnemyState boss = null;
            for (int step = 0; step < 6000 && boss == null; step++)
            {
                session.Advance(0.1f);
                boss = session.Enemies.FirstOrDefault(enemy => enemy.Definition.IsBoss);
            }
            Assume.That(boss, Is.Not.Null);
            Assert.That(session.BossTimeRemaining(boss), Is.GreaterThan(30f));
            Assert.That(session.PressureCount, Is.EqualTo(session.AliveEnemyCount));

            for (int step = 0; step < 420; step++)
            {
                session.Advance(0.1f);
            }

            Assert.That(rampaged, Is.True);
            Assert.That(session.BossTimeRemaining(boss), Is.EqualTo(0f));
            Assert.That(session.PressureCount, Is.EqualTo(session.AliveEnemyCount + 9));
        }

        [Test]
        public void ClearingTheScreenBeforeTheNextWavePaysABonus()
        {
            RunInventory inventory = new RunInventory();
            for (int index = 0; index < RunInventory.SlotCount; index++)
            {
                inventory.TryAdd(new SwordInstance("sword_constellation", 5, (ulong)(index + 1), $"m{index}"), out _);
            }
            CombinationDefenseSession session = Session(inventory, 60);
            List<(int wave, int bonus)> clears = new List<(int, int)>();
            session.WaveCleared += (wave, bonus) => clears.Add((wave, bonus));

            for (int step = 0; step < 400 && clears.Count == 0; step++)
            {
                session.Advance(0.1f);
            }

            Assert.That(clears.Count, Is.EqualTo(1));
            Assert.That(clears[0].wave, Is.EqualTo(1));
            Assert.That(clears[0].bonus, Is.GreaterThan(0));
        }

        private static CombinationDefenseSession Session(RunInventory inventory, int enemyLimit)
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
                enemyLimit: enemyLimit);
        }
    }
}
