using System.Linq;
using NUnit.Framework;
using Swordstellation.Core;

namespace Swordstellation.Tests
{
    public sealed class LearningPathTests
    {
        [Test]
        public void SevenStepsOpenSystemsOneAtATime()
        {
            Assert.That(LearningPath.Steps.Count, Is.EqualTo(7));
            for (int index = 1; index < LearningPath.Steps.Count; index++)
            {
                LearningStep previous = LearningPath.Steps[index - 1];
                LearningStep step = LearningPath.Steps[index];
                Assert.That(step.Features & previous.Features, Is.EqualTo(previous.Features));
                Assert.That(step.Waves, Is.GreaterThanOrEqualTo(previous.Waves));
            }
            for (int number = 2; number <= 6; number++)
            {
                Assert.That(LearningPath.Get(number).NewFeatures, Is.Not.EqualTo(GameFeature.None));
            }
            Assert.That(LearningPath.Get(1).Features, Is.EqualTo(GameFeature.None));
            Assert.That(LearningPath.Get(7).Features, Is.EqualTo(GameFeature.All));
            Assert.That(LearningPath.Get(7).Waves, Is.EqualTo(15));
            Assert.That(LearningPath.Get(1).HealthScale, Is.LessThan(1f));
            Assert.That(LearningPath.Get(7).HealthScale, Is.EqualTo(1f));
        }

        [Test]
        public void StepsOpenInOrderAndClearsAreRecorded()
        {
            PlayerSaveData save = PlayerSaveData.CreateNew();
            Assert.That(LearningPath.IsUnlocked(save, 1), Is.True);
            Assert.That(LearningPath.IsUnlocked(save, 2), Is.False);
            Assert.That(LearningPath.NextStep(save), Is.EqualTo(1));

            ProgressionService.ApplyRun(save, Summary(1, true, 5), 20261012);

            Assert.That(LearningPath.IsCleared(save, 1), Is.True);
            Assert.That(LearningPath.IsUnlocked(save, 2), Is.True);
            Assert.That(LearningPath.NextStep(save), Is.EqualTo(2));
            Assert.That(ProgressionService.IsStageUnlocked(save, "stage_frozen_clock"), Is.False);
        }

        [Test]
        public void FailedAttemptsKeepTheBestWave()
        {
            PlayerSaveData save = PlayerSaveData.CreateNew();
            ProgressionService.ApplyRun(save, Summary(3, false, 4), 20261012);
            ProgressionService.ApplyRun(save, Summary(3, false, 2), 20261012);

            Assert.That(LearningPath.BestWave(save, 3), Is.EqualTo(4));
            Assert.That(LearningPath.BestWave(save, 4), Is.EqualTo(0));
            Assert.That(LearningPath.IsCleared(save, 3), Is.False);
        }

        [Test]
        public void ClearingStepSevenOpensTheNextRift()
        {
            PlayerSaveData save = PlayerSaveData.CreateNew();
            for (int number = 1; number <= 7; number++)
            {
                ProgressionService.ApplyRun(save, Summary(number, true, LearningPath.Get(number).Waves), 20261012);
            }

            Assert.That(LearningPath.IsComplete(save), Is.True);
            Assert.That(LearningPath.NextStep(save), Is.EqualTo(0));
            Assert.That(ProgressionService.IsStageUnlocked(save, "stage_frozen_clock"), Is.True);
        }

        [Test]
        public void SessionFollowsTheStep()
        {
            CombinationDefenseSession session = Session(LearningPath.Get(1), StrongBoard());
            Assert.That(session.TotalWaves, Is.EqualTo(5));
            bool bossSeen = false;
            session.EnemySpawned += enemy => bossSeen |= enemy.Definition.IsBoss;
            for (int step = 0; step < 6000 && !session.IsEnded; step++)
            {
                session.Advance(0.1f);
            }

            Assert.That(session.State, Is.EqualTo(CombinationDefenseState.Victory));
            Assert.That(bossSeen, Is.False);
        }

        [Test]
        public void EarlyStepsHaveWeakerEnemies()
        {
            float easy = FirstEnemyHealth(LearningPath.Get(1));
            float full = FirstEnemyHealth(null);
            Assert.That(easy, Is.LessThan(full));
        }

        [Test]
        public void ConstellationBonusWaitsForItsStep()
        {
            RunInventory Board()
            {
                RunInventory inventory = new RunInventory();
                inventory.TryAdd(new SwordInstance("ember_blade", 1, 1, "a"), out _);
                inventory.TryAdd(new SwordInstance("ember_blade", 1, 2, "b"), out _);
                return inventory;
            }

            float early = Session(LearningPath.Get(5), Board()).BoardPower();
            float open = Session(LearningPath.Get(6), Board()).BoardPower();
            Assert.That(early, Is.LessThan(open));
        }

        [Test]
        public void StarsFollowHowCloseTheEnemiesGot()
        {
            Assert.That(LearningPath.Stars(false, 5), Is.EqualTo(0));
            Assert.That(LearningPath.Stars(true, 10), Is.EqualTo(3));
            Assert.That(LearningPath.Stars(true, 30), Is.EqualTo(2));
            Assert.That(LearningPath.Stars(true, 50), Is.EqualTo(1));
        }

        [Test]
        public void BetterClearsRaiseStarsAndPayOnlyForNewOnes()
        {
            PlayerSaveData save = PlayerSaveData.CreateNew();
            RunSummary close = Summary(1, true, 5);
            close.PeakPressure = 50;
            RunRewardResult first = ProgressionService.ApplyRun(save, close, 20261012);
            Assert.That(first.NewStars, Is.EqualTo(1));
            Assert.That(LearningPath.BestStars(save, 1), Is.EqualTo(1));

            RunSummary clean = Summary(1, true, 5);
            clean.PeakPressure = 10;
            RunRewardResult better = ProgressionService.ApplyRun(save, clean, 20261012);
            Assert.That(better.NewStars, Is.EqualTo(2));
            Assert.That(LearningPath.BestStars(save, 1), Is.EqualTo(3));

            RunRewardResult again = ProgressionService.ApplyRun(save, clean, 20261012);
            Assert.That(again.NewStars, Is.EqualTo(0));
            Assert.That(
                better.IronPowder - again.IronPowder,
                Is.EqualTo(2 * LearningPath.IronPerStar));
        }

        [Test]
        public void SessionRemembersThePeakEnemyCount()
        {
            CombinationDefenseSession session = Session(LearningPath.Get(1), new RunInventory());
            int peak = 0;
            for (int tick = 0; tick < 300 && !session.IsEnded; tick++)
            {
                session.Advance(0.1f);
                peak = System.Math.Max(peak, session.PressureCount);
            }
            Assert.That(session.PeakPressure, Is.GreaterThanOrEqualTo(peak));
            Assert.That(session.PeakPressure, Is.GreaterThan(0));
        }

        private static float FirstEnemyHealth(LearningStep step)
        {
            CombinationDefenseSession session = Session(step, new RunInventory());
            float health = 0f;
            session.EnemySpawned += enemy =>
            {
                if (health == 0f && !enemy.Definition.IsElite && !enemy.Definition.IsBoss)
                {
                    health = enemy.MaxHealth;
                }
            };
            for (int tick = 0; tick < 200 && health == 0f; tick++)
            {
                session.Advance(0.1f);
            }
            return health;
        }

        private static RunInventory StrongBoard()
        {
            RunInventory inventory = new RunInventory();
            for (int index = 0; index < RunInventory.SlotCount; index++)
            {
                inventory.TryAdd(new SwordInstance("sword_constellation", 5, (ulong)(index + 1), $"s{index}"), out _);
            }
            return inventory;
        }

        private static CombinationDefenseSession Session(LearningStep step, RunInventory inventory)
        {
            ContentCatalog catalog = new ContentCatalog();
            return new CombinationDefenseSession(
                catalog,
                catalog.GetStage(LearningPath.StageId),
                RunMode.Standard,
                inventory,
                4242,
                0,
                150,
                enemyLimit: 1000,
                learningStep: step);
        }

        private static RunSummary Summary(int step, bool victory, int finalWave)
        {
            return new RunSummary
            {
                StageId = LearningPath.StageId,
                Mode = RunMode.Standard,
                LearningStep = step,
                Victory = victory,
                FinalWave = finalWave,
                Score = 100,
                DefeatedEnemies = 10
            };
        }
    }
}
