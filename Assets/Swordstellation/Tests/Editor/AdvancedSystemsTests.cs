using System.Linq;
using NUnit.Framework;
using Swordstellation.Core;

namespace Swordstellation.Tests
{
    public sealed class AdvancedSystemsTests
    {
        [Test]
        public void EveryEliteAndBossHasUniqueAbilityData()
        {
            ContentCatalog catalog = new ContentCatalog();
            EnemyDefinition[] specialEnemies = catalog.Enemies.Values
                .Where(enemy => enemy.IsElite || enemy.IsBoss)
                .ToArray();

            Assert.That(specialEnemies, Has.Length.EqualTo(11));
            Assert.That(EnemyAbilityCatalog.Count, Is.EqualTo(11));
            foreach (EnemyDefinition enemy in specialEnemies)
            {
                EnemyAbilityDefinition ability =
                    EnemyAbilityCatalog.Get(enemy.Id);
                Assert.That(ability, Is.Not.Null, enemy.Id);
                Assert.That(ability.Cooldown, Is.GreaterThan(0f));
                Assert.That(ability.NameKo, Is.Not.Empty);
                Assert.That(ability.NameEn, Is.Not.Empty);
            }

            Assert.That(
                specialEnemies.Select(enemy =>
                        EnemyAbilityCatalog.Get(enemy.Id).Type)
                    .Distinct()
                    .Count(),
                Is.EqualTo(specialEnemies.Length));
        }

        [Test]
        public void HeroGrowthImprovesForgeCommand()
        {
            HeroSkillStats beginner = HeroSkillRules.Calculate(1, 0, 0, 0);
            HeroSkillStats veteran = HeroSkillRules.Calculate(40, 12, 12, 12);

            Assert.That(veteran.CooldownSeconds,
                Is.LessThan(beginner.CooldownSeconds));
            Assert.That(veteran.DamageBoost,
                Is.GreaterThan(beginner.DamageBoost));
            Assert.That(veteran.ForgeEnergy,
                Is.GreaterThan(beginner.ForgeEnergy));
            Assert.That(veteran.CoreRepair,
                Is.GreaterThan(beginner.CoreRepair));
        }

        [Test]
        public void RunStatisticsAccumulateAndNormalize()
        {
            PlayerSaveData save = PlayerSaveData.CreateNew();
            RunSummary summary = new RunSummary
            {
                StageId = "stage_starlit_forge",
                Mode = RunMode.Standard,
                Score = 1200,
                FinalWave = 12,
                DefeatedEnemies = 34,
                DefeatedBosses = 1,
                DamageDealt = 12345.6f,
                SwordsSummoned = 18,
                SuccessfulForges = 7,
                HeroSkillsUsed = 2,
                UltimatesUsed = 3,
                EnemyAbilitiesTriggered = 4,
                Victory = true
            };

            ProgressionService.ApplyRun(save, summary, 20260730);

            Assert.That(save.TotalDamageDealt, Is.EqualTo(12346));
            Assert.That(save.TotalEnemiesDefeated, Is.EqualTo(34));
            Assert.That(save.TotalSwordsSummoned, Is.EqualTo(18));
            Assert.That(save.TotalSuccessfulForges, Is.EqualTo(7));
            Assert.That(save.TotalBossesDefeated, Is.EqualTo(1));
            Assert.That(save.TotalHeroSkillsUsed, Is.EqualTo(2));
            Assert.That(save.TotalUltimatesUsed, Is.EqualTo(3));
            Assert.That(
                save.TotalEnemyAbilitiesTriggered,
                Is.EqualTo(4));

            save.TotalDamageDealt = -1;
            save.Normalize();
            Assert.That(save.TotalDamageDealt, Is.Zero);
            Assert.That(
                save.SchemaVersion,
                Is.EqualTo(PlayerSaveData.CurrentSchemaVersion));
        }
    }
}
