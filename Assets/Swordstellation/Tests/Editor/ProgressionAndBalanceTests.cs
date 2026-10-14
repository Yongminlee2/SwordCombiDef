using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Swordstellation.Core;

namespace Swordstellation.Tests
{
    public sealed class ProgressionAndBalanceTests
    {
        [Test]
        public void RunContent_HasThreeUltimatesAndTwentyFourBasicRunes()
        {
            Assert.That(RunContentCatalog.Ultimates.Count, Is.EqualTo(3));
            Assert.That(
                RunContentCatalog.Runes.Count(item => item.Tag == RuneTag.Basic),
                Is.EqualTo(24));
            Assert.That(
                RunContentCatalog.Runes.Select(item => item.Id).Distinct().Count(),
                Is.EqualTo(RunContentCatalog.Runes.Count));
        }

        [Test]
        public void EveryBasicRuneEffect_HasTwoOptions()
        {
            foreach (RuneEffect effect in RunContentCatalog.Runes
                         .Where(item => item.Tag == RuneTag.Basic)
                         .Select(item => item.Effect)
                         .Distinct())
            {
                Assert.That(
                    RunContentCatalog.Runes.Count(item => item.Effect == effect),
                    Is.EqualTo(2),
                    effect.ToString());
            }
        }

        [Test]
        public void StandardWavePlan_ContainsEliteAndBoss()
        {
            Assert.That(RunBalanceRules.CreateWavePlan(3).HasElite, Is.True);
            Assert.That(RunBalanceRules.CreateWavePlan(5).HasBoss, Is.True);
            Assert.That(RunBalanceRules.CreateWavePlan(10).HasBoss, Is.True);
            Assert.That(RunBalanceRules.TotalWaves(RunMode.Standard), Is.EqualTo(15));
            Assert.That(RunBalanceRules.TotalWaves(RunMode.Daily), Is.EqualTo(15));
        }

        [Test]
        public void SummonsStayCheapAndForgeChainsDiscountThem()
        {
            Assert.That(RunBalanceRules.StartingGold(0), Is.EqualTo(150));
            Assert.That(RunBalanceRules.SummonCost(0), Is.EqualTo(30));
            Assert.That(RunBalanceRules.SummonCost(10), Is.EqualTo(50));
            Assert.That(
                RunBalanceRules.SummonCost(10, forgeChain: 2),
                Is.EqualTo(40));
            Assert.That(
                RunBalanceRules.SummonCost(10, forgeChain: 99),
                Is.EqualTo(35));
            Assert.That(
                RunBalanceRules.SummonCost(0, smithingLevel: 30),
                Is.EqualTo(15));
        }

        [Test]
        public void SellingRefundsHalfOfTheSummonsASwordRepresents()
        {
            Assert.That(
                RunBalanceRules.SellValue(SwordRarity.Common, 1, 40),
                Is.EqualTo(20));
            Assert.That(
                RunBalanceRules.SellValue(SwordRarity.Common, 2, 40),
                Is.EqualTo(40));
            Assert.That(
                RunBalanceRules.SellValue(SwordRarity.Rare, 1, 40),
                Is.EqualTo(40));
            Assert.That(
                RunBalanceRules.SellValue(SwordRarity.Heroic, 2, 40),
                Is.EqualTo(160));
        }

        [Test]
        public void LateWavesGrowHealthButEarlyWavesStayGentle()
        {
            Assert.That(
                RunBalanceRules.LateWaveHealthGrowth(5),
                Is.EqualTo(1f));
            Assert.That(
                RunBalanceRules.CreateWavePlan(15).NormalHealthMultiplier,
                Is.GreaterThan(
                    RunBalanceRules.CreateWavePlan(10)
                        .NormalHealthMultiplier * 1.6f));
        }

        [Test]
        public void CommonOneStarSwordsHitCombinationWallAfterWaveFive()
        {
            Assert.That(
                RunBalanceRules
                    .CombinationPressureMultiplier(
                        1,
                        SwordRarity.Common,
                        5),
                Is.EqualTo(1f));
            Assert.That(
                RunBalanceRules
                    .CombinationPressureMultiplier(
                        1,
                        SwordRarity.Common,
                        7),
                Is.LessThan(0.7f));
            Assert.That(
                RunBalanceRules
                    .CombinationPressureMultiplier(
                        2,
                        SwordRarity.Common,
                        10),
                Is.EqualTo(1f));
            Assert.That(
                RunBalanceRules
                    .CombinationPressureMultiplier(
                        1,
                        SwordRarity.Rare,
                        10),
                Is.EqualTo(1f));
        }

        [Test]
        public void RuneModifiers_StackAndPressureBurstIsConsumedOnce()
        {
            RunModifierState modifiers = new RunModifierState();
            modifiers.Apply(RunContentCatalog.GetRune("keen_edge"));
            modifiers.Apply(RunContentCatalog.GetRune("repair_moss"));
            modifiers.Apply(RunContentCatalog.GetRune("echo_fork"));

            Assert.That(modifiers.DamageMultiplier, Is.EqualTo(1.12f).Within(0.001f));
            Assert.That(modifiers.ExtraTargets, Is.EqualTo(1));
            Assert.That(
                modifiers.ConsumePressureBurst(),
                Is.EqualTo(0.22f).Within(0.001f));
            Assert.That(
                modifiers.ConsumePressureBurst(),
                Is.Zero);
        }

        [Test]
        public void CombatMath_StarsAndGrowthIncreaseDamage()
        {
            ContentCatalog catalog = new ContentCatalog();
            SwordDefinition definition = catalog.GetSword("ember_blade");
            SwordInstance oneStar = new SwordInstance("ember_blade", 1, 1, "a");
            SwordInstance threeStar = new SwordInstance("ember_blade", 3, 2, "b");

            float baseline = CombatMath.SwordDamage(
                definition,
                oneStar,
                0,
                null,
                false,
                false);
            float upgraded = CombatMath.SwordDamage(
                definition,
                threeStar,
                10,
                null,
                false,
                false);

            Assert.That(upgraded, Is.GreaterThan(baseline * 3f));
        }

        [Test]
        public void SwordMasteryPersistsAndIncreasesDamage()
        {
            ContentCatalog catalog = new ContentCatalog();
            SwordDefinition definition =
                catalog.GetSword("ember_blade");
            SwordInstance sword =
                new SwordInstance(
                    "ember_blade",
                    1,
                    1,
                    "mastery");
            PlayerSaveData save =
                PlayerSaveData.CreateNew();
            save.IronPowder = 1000;

            UpgradeResult upgrade =
                ProgressionService
                    .TryUpgradeSwordMastery(
                        save,
                        "ember_blade");
            float baseline = CombatMath.SwordDamage(
                definition,
                sword,
                0,
                null,
                false,
                false,
                0);
            float mastered = CombatMath.SwordDamage(
                definition,
                sword,
                0,
                null,
                false,
                false,
                save.GetSwordMasteryLevel(
                    "ember_blade"));

            Assert.That(upgrade.Success, Is.True);
            Assert.That(
                save.GetSwordMasteryLevel(
                    "ember_blade"),
                Is.EqualTo(1));
            Assert.That(mastered, Is.GreaterThan(baseline));
        }

        [Test]
        public void CombatMath_RunesAffectTargetsAndCooldown()
        {
            ContentCatalog catalog = new ContentCatalog();
            SwordDefinition definition = catalog.GetSword("ember_blade");
            RunModifierState modifiers = new RunModifierState();
            modifiers.Apply(RunContentCatalog.GetRune("echo_fork"));
            modifiers.Apply(RunContentCatalog.GetRune("swift_sheath"));

            Assert.That(
                CombatMath.TargetCount(definition, modifiers, 5),
                Is.EqualTo(2));
            Assert.That(
                CombatMath.AttackCooldown(definition, 0, modifiers),
                Is.LessThan(definition.AttackInterval));
        }

        [Test]
        public void RunInventory_RejectsNullAndDuplicateSelections()
        {
            RunInventory inventory = new RunInventory();
            Assert.Throws<ArgumentNullException>(
                () => inventory.TryAdd(null, out _));
            SwordInstance sword =
                new SwordInstance("ember_blade", 1, 1, "a");
            inventory.TryAdd(sword, out _);
            Assert.Throws<ArgumentException>(
                () => inventory.GetSelection(new[] { 0, 0 }));
        }

        [Test]
        public void RunInventory_MergesOnlyMatchingSwordAndRank()
        {
            RunInventory inventory = new RunInventory();
            inventory.TryAdd(
                new SwordInstance(
                    "ember_blade",
                    1,
                    10,
                    "source"),
                out int source);
            inventory.TryAdd(
                new SwordInstance(
                    "ember_blade",
                    1,
                    20,
                    "destination"),
                out int destination);

            Assert.That(
                inventory.TryMergeSame(
                    source,
                    destination,
                    out SwordInstance result),
                Is.True);
            Assert.That(result.Stars, Is.EqualTo(2));
            Assert.That(
                inventory.Get(source),
                Is.Null);
            Assert.That(
                inventory.Get(destination),
                Is.SameAs(result));

            inventory.TryAdd(
                new SwordInstance(
                    "frost_blade",
                    1,
                    30,
                    "mismatch"),
                out int mismatch);
            Assert.That(
                inventory.TryMergeSame(
                    mismatch,
                    destination,
                    out _),
                Is.False);
        }

        [Test]
        public void PlayerSave_NormalizeRepairsInvalidValuesAndDuplicates()
        {
            PlayerSaveData save = PlayerSaveData.CreateNew();
            save.PlayerLevel = -4;
            save.IronPowder = -5;
            save.MusicVolume = 2f;
            save.UnlockedSwordIds.Add("ember_blade");
            save.CompletedStageIds = null;

            save.Normalize();

            Assert.That(save.PlayerLevel, Is.EqualTo(1));
            Assert.That(save.IronPowder, Is.Zero);
            Assert.That(save.MusicVolume, Is.EqualTo(1f));
            Assert.That(
                save.UnlockedSwordIds.Count(id => id == "ember_blade"),
                Is.EqualTo(1));
            Assert.That(save.CompletedStageIds, Is.Not.Null);
        }

        [Test]
        public void Upgrade_UsesIncreasingCostAndHonorsMaximumLevel()
        {
            PlayerSaveData save = PlayerSaveData.CreateNew();
            save.IronPowder = 10000;
            UpgradeResult first = ProgressionService.TryUpgrade(
                save,
                UpgradeBranch.Smithing);
            Assert.That(first.Success, Is.True);
            Assert.That(first.Cost, Is.EqualTo(100));

            save.SmithingLevel = RunBalanceRules.MaximumUpgradeLevel;
            UpgradeResult maximum = ProgressionService.TryUpgrade(
                save,
                UpgradeBranch.Smithing);
            Assert.That(maximum.Success, Is.False);
            Assert.That(maximum.MessageKey, Is.EqualTo("upgrade.max_level"));
        }

        [Test]
        public void DailyVictoryBonus_IsOnlyAwardedOncePerDate()
        {
            PlayerSaveData save = PlayerSaveData.CreateNew();
            RunSummary summary = WinningSummary(
                "stage_starlit_forge",
                RunMode.Daily);

            RunRewardResult first =
                ProgressionService.ApplyRun(save, summary, 20260729);
            RunRewardResult second =
                ProgressionService.ApplyRun(save, summary, 20260729);

            Assert.That(first.DailyBonusAwarded, Is.True);
            Assert.That(second.DailyBonusAwarded, Is.False);
            Assert.That(first.Starsteel, Is.GreaterThanOrEqualTo(10));
        }

        [Test]
        public void StageVictory_UnlocksNextStageAndBaseSwords()
        {
            PlayerSaveData save = PlayerSaveData.CreateNew();
            RunRewardResult reward = ProgressionService.ApplyRun(
                save,
                WinningSummary("stage_starlit_forge", RunMode.Standard),
                20260729);

            Assert.That(
                ProgressionService.IsStageUnlocked(
                    save,
                    "stage_frozen_clock"),
                Is.True);
            Assert.That(
                reward.NewlyUnlockedSwordIds,
                Does.Contain("radiant_blade"));
        }

        [Test]
        public void RepeatedVictory_DoesNotRepeatStageUnlockNotification()
        {
            PlayerSaveData save = PlayerSaveData.CreateNew();
            RunSummary summary = WinningSummary(
                "stage_starlit_forge",
                RunMode.Standard);
            ProgressionService.ApplyRun(save, summary, 20260729);
            RunRewardResult repeated =
                ProgressionService.ApplyRun(save, summary, 20260729);

            Assert.That(repeated.NewlyUnlockedStageId, Is.Null);
            Assert.That(repeated.NewlyUnlockedSwordIds, Is.Empty);
        }

        [Test]
        public void GachaPity_ProducesLegendaryOrMythicOnThirtiethPull()
        {
            ContentCatalog catalog = new ContentCatalog();
            PlayerSaveData save = PlayerSaveData.CreateNew();
            save.Starsteel = GachaService.PullCost;
            save.GachaPullsSinceHighRarity =
                GachaService.HighRarityPityThreshold - 1;
            GachaService gacha =
                new GachaService(catalog, new SeededRandom(9));

            GachaPullResult result = gacha.TryPull(save);

            Assert.That(result.Success, Is.True);
            Assert.That(result.Rarity, Is.GreaterThanOrEqualTo(SwordRarity.Legendary));
            Assert.That(save.GachaPullsSinceHighRarity, Is.Zero);
        }

        [Test]
        public void GachaDuplicate_ConvertsDirectlyToUniversalFragments()
        {
            ContentCatalog catalog = new ContentCatalog();
            PlayerSaveData save = PlayerSaveData.CreateNew();
            save.UnlockedSwordIds = catalog.Swords.Keys.ToList();
            save.Starsteel = GachaService.PullCost;
            GachaService gacha =
                new GachaService(catalog, new SeededRandom(22));

            GachaPullResult result = gacha.TryPull(save);

            Assert.That(result.Success, Is.True);
            Assert.That(save.UniversalBlueprintFragments, Is.EqualTo(result.Fragments));
            Assert.That(result.MessageKey, Is.EqualTo("shop.pull_duplicate"));
        }

        [Test]
        public void UniversalFragments_UnlockAnUnownedForgedSword()
        {
            ContentCatalog catalog = new ContentCatalog();
            PlayerSaveData save = PlayerSaveData.CreateNew();
            save.UniversalBlueprintFragments =
                GachaService.UnlockRequirement;
            int before = save.UnlockedSwordIds.Count;
            GachaService gacha =
                new GachaService(catalog, new SeededRandom(55));

            GachaPullResult result =
                gacha.TryUnlockWithUniversalFragments(save);

            Assert.That(result.Success, Is.True);
            Assert.That(result.Unlocked, Is.True);
            Assert.That(save.UnlockedSwordIds.Count, Is.EqualTo(before + 1));
            Assert.That(save.UniversalBlueprintFragments, Is.Zero);
        }

        [Test]
        public void HighLevelUpgrade_ConsumesGrowthMaterials()
        {
            PlayerSaveData save = PlayerSaveData.CreateNew();
            save.SmithingLevel = 20;
            save.IronPowder = 100000;
            save.Catalysts = 20;
            save.BossCores = 3;

            UpgradeResult result = ProgressionService.TryUpgrade(
                save,
                UpgradeBranch.Smithing);

            Assert.That(result.Success, Is.True);
            Assert.That(result.CatalystCost, Is.GreaterThan(0));
            Assert.That(result.BossCoreCost, Is.EqualTo(1));
            Assert.That(save.BossCores, Is.EqualTo(2));
        }

        private static RunSummary WinningSummary(
            string stageId,
            RunMode mode)
        {
            return new RunSummary
            {
                StageId = stageId,
                Mode = mode,
                FinalWave = 30,
                Score = 3000,
                DefeatedEnemies = 100,
                DefeatedElites = 3,
                DefeatedBosses = 1,
                Victory = true,
                DiscoveredRecipeIds =
                    new List<string> { "recipe_scarlet_cyclone" },
                SelectedRuneIds =
                    new List<string> { "keen_edge" }
            };
        }
    }
}
