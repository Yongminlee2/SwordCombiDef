using System.Linq;
using NUnit.Framework;
using Swordstellation.Core;

namespace Swordstellation.Tests
{
    public sealed class CombinationDefenseSessionTests
    {
        [Test]
        public void SwordAttacksDamageEnemiesAndKillsGrantGold()
        {
            ContentCatalog catalog = new ContentCatalog();
            RunInventory inventory = new RunInventory();
            inventory.TryAdd(
                new SwordInstance(
                    "sword_constellation",
                    5,
                    1,
                    "test-mythic"),
                out _);
            CombinationDefenseSession session =
                CreateSession(
                    catalog,
                    inventory,
                    480);
            int attacks = 0;
            int kills = 0;
            session.SwordAttacked += _ => attacks++;
            session.EnemyDefeated += (_, _) => kills++;

            Advance(session, 18f);

            Assert.That(attacks, Is.GreaterThan(0));
            Assert.That(session.DamageDealt, Is.GreaterThan(0f));
            Assert.That(kills, Is.GreaterThan(0));
            Assert.That(session.Gold, Is.GreaterThan(480));
        }

        [Test]
        public void EmptyBoardEventuallyLosesAtEnemyLimit()
        {
            ContentCatalog catalog = new ContentCatalog();
            CombinationDefenseSession session =
                CreateSession(
                    catalog,
                    new RunInventory(),
                    480);

            Advance(session, 120f);

            Assert.That(
                session.State,
                Is.EqualTo(
                    CombinationDefenseState.Defeat));
            Assert.That(
                session.AliveEnemyCount,
                Is.GreaterThanOrEqualTo(
                    CombinationDefenseSession
                        .DefaultEnemyLimit));
        }

        [Test]
        public void BossAppearsEveryFifthWave()
        {
            ContentCatalog catalog = new ContentCatalog();
            RunInventory inventory = new RunInventory();
            for (int index = 0;
                 index < RunInventory.SlotCount;
                 index++)
            {
                inventory.TryAdd(
                    new SwordInstance(
                        "sword_constellation",
                        5,
                        (ulong)(index + 1),
                        $"mythic-{index}"),
                    out _);
            }
            CombinationDefenseSession session =
                CreateSession(
                    catalog,
                    inventory,
                    480);
            int bossSpawns = 0;
            session.EnemySpawned += enemy =>
            {
                if (enemy.Definition.IsBoss)
                {
                    bossSpawns++;
                }
            };

            // Breaks are short now, so give the wave-10 boss time to fall.
            while (!session.IsEnded &&
                   session.Wave < 12)
            {
                session.Advance(0.1f);
            }

            Assert.That(
                bossSpawns,
                Is.GreaterThanOrEqualTo(2));
            Assert.That(
                session.DefeatedBosses,
                Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void EveryBoardSlotCanReachTheLoop()
        {
            ContentCatalog catalog =
                new ContentCatalog();
            for (int slot = 0;
                 slot < RunInventory.MaximumSlotCount;
                 slot++)
            {
                BattlePoint point =
                    CombinationDefenseSession
                        .BoardSlotPosition(
                            slot,
                            RunInventory
                                .MaximumSlotCount);
                float nearest = Enumerable
                    .Range(0, 400)
                    .Select(index =>
                        point.DistanceSquared(
                            CombinationDefenseSession
                                .LoopPosition(index / 400f)))
                    .Min();
                foreach (string swordId in
                         catalog.BaseSwordIds)
                {
                    float range =
                        CombinationDefenseSession
                            .EffectiveAttackRange(
                                slot,
                                RunInventory
                                    .MaximumSlotCount,
                                catalog.GetSword(
                                    swordId));
                    Assert.That(
                        nearest,
                        Is.LessThan(
                            range * range),
                        $"slot {slot}, sword {swordId}");
                }
            }
        }

        [Test]
        public void PermanentGrowthExpandsBoardAndAdvancedSummons()
        {
            Assert.That(
                RunBalanceRules.BoardSlotCount(0),
                Is.EqualTo(12));
            Assert.That(
                RunBalanceRules.BoardSlotCount(10),
                Is.EqualTo(13));
            Assert.That(
                RunBalanceRules.BoardSlotCount(20),
                Is.EqualTo(14));
            Assert.That(
                RunBalanceRules.BoardSlotCount(30),
                Is.EqualTo(15));
            Assert.That(
                new RunInventory(15).Capacity,
                Is.EqualTo(15));
            Assert.That(
                RunBalanceRules.AdvancedSummonChance(0),
                Is.Zero);
            Assert.That(
                RunBalanceRules.AdvancedSummonChance(30),
                Is.EqualTo(0.12f).Within(0.0001f));
        }

        [Test]
        public void TwelveSwordFamiliesHaveUniqueBaseRepresentatives()
        {
            ContentCatalog catalog = new ContentCatalog();
            SwordFamily[] families = catalog.BaseSwordIds
                .Select(id =>
                    SwordFamilyRules.Resolve(
                        catalog.GetSword(id)))
                .Distinct()
                .ToArray();

            Assert.That(families, Has.Length.EqualTo(12));
        }

        [Test]
        public void EliteAndBossAbilitiesTriggerDuringARealRun()
        {
            ContentCatalog catalog = new ContentCatalog();
            // Denser waves would end an empty-board run before the first elite acts.
            CombinationDefenseSession session =
                new CombinationDefenseSession(
                    catalog,
                    catalog.GetStage("stage_starlit_forge"),
                    RunMode.Standard,
                    new RunInventory(),
                    12345,
                    0,
                    480,
                    enemyLimit: 1000);
            int events = 0;
            session.EnemyAbilityUsed += _ => events++;

            Advance(session, 55f);

            Assert.That(events, Is.GreaterThan(0));
            Assert.That(
                session.EnemyAbilitiesTriggered,
                Is.EqualTo(events));
        }

        [Test]
        public void PressureBurstRuneDamagesExistingEnemies()
        {
            ContentCatalog catalog = new ContentCatalog();
            CombinationDefenseSession session =
                CreateSession(
                    catalog,
                    new RunInventory(),
                    480);
            Advance(session, 4f);
            Assert.That(session.Enemies, Is.Not.Empty);
            float healthBefore =
                session.Enemies.Sum(enemy =>
                    enemy.Health + enemy.Shield);

            session.ApplyRune(
                RunContentCatalog.GetRune(
                    "repair_moss"));

            float healthAfter =
                session.Enemies.Sum(enemy =>
                    enemy.Health + enemy.Shield);
            Assert.That(
                healthAfter,
                Is.LessThan(healthBefore));
        }

        private static CombinationDefenseSession
            CreateSession(
                ContentCatalog catalog,
                RunInventory inventory,
                int gold)
        {
            return new CombinationDefenseSession(
                catalog,
                catalog.GetStage(
                    "stage_starlit_forge"),
                RunMode.Standard,
                inventory,
                12345,
                0,
                gold);
        }

        private static void Advance(
            CombinationDefenseSession session,
            float seconds)
        {
            int steps = (int)(seconds / 0.1f);
            for (int index = 0;
                 index < steps &&
                 !session.IsEnded;
                 index++)
            {
                session.Advance(0.1f);
            }
        }
    }
}
