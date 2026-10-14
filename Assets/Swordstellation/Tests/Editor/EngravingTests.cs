using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Swordstellation.Core;

namespace Swordstellation.Tests
{
    public sealed class EngravingTests
    {
        [Test]
        public void Catalog_HasBasicRunesPlusRareAndLegendaryEngravings()
        {
            IReadOnlyList<RuneDefinition> runes = RunContentCatalog.Runes;

            Assert.That(runes.Count(rune => rune.Tag == RuneTag.Basic), Is.EqualTo(24));
            Assert.That(runes.Count(rune => rune.Rarity == RuneRarity.Rare), Is.GreaterThanOrEqualTo(12));
            Assert.That(runes.Count(rune => rune.Rarity == RuneRarity.Legendary), Is.GreaterThanOrEqualTo(4));
            Assert.That(runes.Select(rune => rune.Id).Distinct().Count(), Is.EqualTo(runes.Count));
        }

        [Test]
        public void Draw_OffersThreeDistinctRunesTheRunDoesNotOwn()
        {
            string[] owned = { "keen_edge", "forge_memory" };

            IReadOnlyList<RuneDefinition> drawn = RunContentCatalog.DrawRunes(
                new SeededRandom(7),
                owned,
                false);

            Assert.That(drawn.Count, Is.EqualTo(3));
            Assert.That(drawn.Select(rune => rune.Id).Distinct().Count(), Is.EqualTo(3));
            Assert.That(drawn.Select(rune => rune.Id).Intersect(owned), Is.Empty);
        }

        [Test]
        public void Draw_BossWaveAlwaysIncludesRareOrBetter()
        {
            for (ulong seed = 1; seed <= 60; seed++)
            {
                IReadOnlyList<RuneDefinition> drawn = RunContentCatalog.DrawRunes(
                    new SeededRandom(seed),
                    new string[0],
                    true);

                Assert.That(
                    drawn.Any(rune => rune.Rarity >= RuneRarity.Rare),
                    Is.True,
                    $"seed {seed}");
            }
        }

        [Test]
        public void Draw_OffersOnlyWhatIsLeft()
        {
            List<string> allButTwo = RunContentCatalog.Runes
                .Skip(2)
                .Select(rune => rune.Id)
                .ToList();
            List<string> all = RunContentCatalog.Runes
                .Select(rune => rune.Id)
                .ToList();

            Assert.That(
                RunContentCatalog.DrawRunes(new SeededRandom(3), allButTwo, true).Count,
                Is.EqualTo(2));
            Assert.That(
                RunContentCatalog.DrawRunes(new SeededRandom(3), all, false),
                Is.Empty);
        }

        [Test]
        public void ForgeMemory_StacksDamageOnEveryForge()
        {
            RunModifierState modifiers = new RunModifierState();
            modifiers.RegisterForge();
            Assert.That(modifiers.ForgeStacks, Is.EqualTo(0));

            modifiers.Apply(RunContentCatalog.GetRune("forge_memory"));
            modifiers.RegisterForge();
            modifiers.RegisterForge();

            Assert.That(modifiers.ForgeStacks, Is.EqualTo(2));
            Assert.That(modifiers.ForgeDamageBonus, Is.EqualTo(0.04f).Within(0.0001f));
        }

        [Test]
        public void JoyOfDiscovery_CountsEachNewResultOnce()
        {
            RunModifierState modifiers = new RunModifierState();
            modifiers.Apply(RunContentCatalog.GetRune("joy_of_discovery"));

            modifiers.RegisterCraftedResult("scarlet_cyclone");
            modifiers.RegisterCraftedResult("scarlet_cyclone");
            modifiers.RegisterCraftedResult("magma_greatsword");

            Assert.That(modifiers.DiscoveryDamageBonus, Is.EqualTo(0.08f).Within(0.0001f));
        }

        [Test]
        public void ConstellationChain_BonusGrowsWithChainAndCaps()
        {
            RunModifierState modifiers = new RunModifierState();
            modifiers.SetForgeChain(3);
            Assert.That(modifiers.ChainDamageBonus, Is.EqualTo(0f));

            modifiers.Apply(RunContentCatalog.GetRune("constellation_chain"));
            modifiers.SetForgeChain(3);
            Assert.That(modifiers.ChainDamageBonus, Is.EqualTo(0.15f).Within(0.0001f));
            modifiers.SetForgeChain(20);
            Assert.That(modifiers.ChainDamageBonus, Is.EqualTo(0.30f).Within(0.0001f));
        }

        [Test]
        public void Burn_DealsDamageOverItsDuration()
        {
            BattleEnemyState enemy = Enemy();
            enemy.ApplyBurn(10f, 3f);

            float dealt = enemy.TickBurn(1f) + enemy.TickBurn(1f) + enemy.TickBurn(1f) + enemy.TickBurn(1f);

            Assert.That(dealt, Is.EqualTo(30f).Within(0.01f));
            Assert.That(enemy.MaxHealth - enemy.Health, Is.EqualTo(30f).Within(0.01f));
        }

        [Test]
        public void FrostShackle_SlowsMovementUntilItExpires()
        {
            BattleEnemyState plain = Enemy();
            BattleEnemyState slowed = Enemy();
            slowed.ApplySlow(0.7f, 2f);

            plain.Advance(1f);
            slowed.Advance(1f);
            Assert.That(slowed.Progress, Is.LessThan(plain.Progress));

            slowed.TickAbility(2.1f);
            float before = slowed.Progress;
            slowed.Advance(1f);
            Assert.That(slowed.Progress - before, Is.EqualTo(slowed.ProgressSpeed).Within(0.0001f));
        }

        [Test]
        public void Ultimate_WaitsForThePlayer()
        {
            // An empty board lets 20+ enemies pile up: the old code fired on its own here.
            CombinationDefenseSession session = Session(new RunInventory(), 1000);
            int used = 0;
            session.UltimateUsed += _ => used++;

            Advance(session, 40f);
            Assume.That(session.Enemies.Count, Is.GreaterThanOrEqualTo(20));
            Assert.That(session.TryUseUltimate(), Is.False);
            session.AddUltimateCharge(100f);
            Advance(session, 3f);
            Assert.That(used, Is.EqualTo(0));
            Assert.That(session.UltimateReady, Is.True);

            Assert.That(session.TryUseUltimate(), Is.True);
            Assert.That(used, Is.EqualTo(1));
            Assert.That(session.UltimateCharge, Is.EqualTo(0f));
            Assert.That(session.UltimateReady, Is.False);
        }

        [Test]
        public void AddUltimateCharge_ClampsAtFull()
        {
            CombinationDefenseSession session = Session(new RunInventory());

            session.AddUltimateCharge(70f);
            session.AddUltimateCharge(70f);

            Assert.That(session.UltimateCharge, Is.EqualTo(100f));
        }

        [Test]
        public void Focus_MakesSwordsHitTheMarkedEnemyFirst()
        {
            RunInventory inventory = Board("frost_blade", 1);
            CombinationDefenseSession session = Session(inventory);
            SwordDefinition frost = new ContentCatalog().GetSword("frost_blade");
            BattleEnemyState marked = null;
            for (int step = 0; step < 400 && marked == null; step++)
            {
                session.Advance(0.05f);
                List<BattleEnemyState> inRange = InRangeOfSlotZero(session, frost);
                if (inRange.Count >= 2)
                {
                    marked = inRange.Last();
                }
            }
            Assume.That(marked, Is.Not.Null, "two enemies never shared slot 0's range");

            BattleEnemyState firstTarget = null;
            session.SwordAttacked += attack => firstTarget ??= attack.Targets[0];
            session.SetFocus(marked);
            for (int step = 0; step < 40 && firstTarget == null; step++)
            {
                session.Advance(0.05f);
            }

            Assert.That(firstTarget, Is.SameAs(marked));
        }

        [Test]
        public void Focus_ClearsWhenTheMarkedEnemyDies()
        {
            CombinationDefenseSession session = Session(Board("sword_constellation", 5));
            Advance(session, 4f);
            Assume.That(session.Enemies.Count, Is.GreaterThan(0));
            BattleEnemyState marked = session.Enemies[0];
            session.SetFocus(marked, 30f);
            Assert.That(session.FocusedEnemy, Is.SameAs(marked));

            Advance(session, 15f);

            Assert.That(marked.IsAlive, Is.False);
            Assert.That(session.FocusedEnemy, Is.Null);
        }

        [Test]
        public void BonusKills_FromBurnAndChain_AreRewardedOnce()
        {
            RunInventory inventory = new RunInventory();
            inventory.TryAdd(new SwordInstance("ember_blade", 3, 1, "ember"), out _);
            inventory.TryAdd(new SwordInstance("thunder_blade", 3, 2, "thunder"), out _);
            inventory.TryAdd(new SwordInstance("ember_blade", 3, 3, "ember2"), out _);
            CombinationDefenseSession session = Session(inventory);
            session.ApplyRune(RunContentCatalog.GetRune("searing_flame"));
            session.ApplyRune(RunContentCatalog.GetRune("chain_lightning"));
            List<string> defeated = new List<string>();
            session.EnemyDefeated += (enemy, _) => defeated.Add(enemy.RuntimeId);

            Advance(session, 40f);

            Assert.That(defeated.Count, Is.GreaterThan(0));
            Assert.That(defeated.Distinct().Count(), Is.EqualTo(defeated.Count));
            Assert.That(session.DefeatedEnemies, Is.EqualTo(defeated.Count));
        }

        [Test]
        public void IntermissionStarts_AfterEveryWaveButTheLast()
        {
            RunInventory inventory = new RunInventory();
            for (int index = 0; index < RunInventory.SlotCount; index++)
            {
                inventory.TryAdd(
                    new SwordInstance("sword_constellation", 5, (ulong)(index + 1), $"m{index}"),
                    out _);
            }
            CombinationDefenseSession session = Session(inventory);
            List<int> waves = new List<int>();
            session.IntermissionStarted += wave => waves.Add(wave);

            Advance(session, 1500f);

            Assert.That(session.State, Is.EqualTo(CombinationDefenseState.Victory));
            Assert.That(waves, Is.EqualTo(Enumerable.Range(1, 14).ToList()));
        }

        [Test]
        public void BoardPower_RisesWithEngravings()
        {
            RunInventory inventory = Board("frost_blade", 3);
            CombinationDefenseSession session = Session(inventory);
            float before = session.BoardPower();

            session.ApplyRune(RunContentCatalog.GetRune("twin_stars"));
            float withExtraTarget = session.BoardPower();
            session.ApplyRune(RunContentCatalog.GetRune("forge_memory"));
            session.RegisterForge();
            float withStack = session.BoardPower();

            Assert.That(before, Is.GreaterThan(0f));
            Assert.That(withExtraTarget, Is.GreaterThan(before));
            Assert.That(withStack, Is.GreaterThan(withExtraTarget));
        }

        private static List<BattleEnemyState> InRangeOfSlotZero(
            CombinationDefenseSession session,
            SwordDefinition sword)
        {
            BattlePoint source = CombinationDefenseSession.BoardSlotPosition(0);
            float range = CombinationDefenseSession.EffectiveAttackRange(
                0,
                RunInventory.SlotCount,
                sword);
            return session.Enemies
                .Where(enemy =>
                    enemy.IsAlive &&
                    source.DistanceSquared(
                        CombinationDefenseSession.LoopPosition(enemy.Progress)) <
                    range * range * 0.8f)
                .OrderBy(enemy =>
                    source.DistanceSquared(
                        CombinationDefenseSession.LoopPosition(enemy.Progress)))
                .ToList();
        }

        private static BattleEnemyState Enemy()
        {
            return new BattleEnemyState(
                "test-enemy",
                new ContentCatalog().GetEnemy("slime_mint"),
                1,
                1f,
                1f,
                0.2f);
        }

        private static RunInventory Board(string swordId, int stars)
        {
            RunInventory inventory = new RunInventory();
            inventory.TryAdd(new SwordInstance(swordId, stars, 1, "board-0"), out _);
            return inventory;
        }

        private static CombinationDefenseSession Session(
            RunInventory inventory,
            int enemyLimit = CombinationDefenseSession.DefaultEnemyLimit)
        {
            ContentCatalog catalog = new ContentCatalog();
            return new CombinationDefenseSession(
                catalog,
                catalog.GetStage("stage_starlit_forge"),
                RunMode.Standard,
                inventory,
                12345,
                0,
                150,
                enemyLimit: enemyLimit);
        }

        private static void Advance(CombinationDefenseSession session, float seconds)
        {
            for (int index = 0; index < (int)(seconds / 0.1f) && !session.IsEnded; index++)
            {
                session.Advance(0.1f);
            }
        }
    }
}
