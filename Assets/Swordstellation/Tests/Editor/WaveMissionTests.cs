using System;
using System.Linq;
using NUnit.Framework;
using Swordstellation.Core;

namespace Swordstellation.Tests
{
    public sealed class WaveMissionTests
    {
        [Test]
        public void EveryWaveGetsAReachableMissionWithAReward()
        {
            SeededRandom random = new SeededRandom(7);
            for (int wave = 1; wave <= 15; wave++)
            {
                WaveMission mission = WaveMissionRules.Create(wave, random);
                Assert.That(mission.Target, Is.InRange(1, 6));
                Assert.That(mission.Reward, Is.GreaterThan(0));
                Assert.That(mission.Progress, Is.EqualTo(0));
            }
        }

        [Test]
        public void MissionsVaryAcrossARun()
        {
            SeededRandom random = new SeededRandom(11);
            int kinds = Enumerable.Range(1, 15)
                .Select(wave => WaveMissionRules.Create(wave, random).Kind)
                .Distinct()
                .Count();
            Assert.That(kinds, Is.GreaterThanOrEqualTo(3));
        }

        [Test]
        public void OnlyMatchingActionsCountAndCompletionFiresOnce()
        {
            WaveMission mission = new WaveMission(WaveMissionKind.Summon, 2, 40);

            Assert.That(mission.Record(WaveMissionKind.Sell), Is.False);
            Assert.That(mission.Progress, Is.EqualTo(0));
            Assert.That(mission.Record(WaveMissionKind.Summon), Is.False);
            Assert.That(mission.Record(WaveMissionKind.Summon), Is.True);
            Assert.That(mission.IsComplete, Is.True);
            Assert.That(mission.Record(WaveMissionKind.Summon), Is.False);
            Assert.That(mission.Progress, Is.EqualTo(2));
        }

        [Test]
        public void LaterWavesPayMore()
        {
            Assert.That(
                WaveMissionRules.Reward(12),
                Is.GreaterThan(WaveMissionRules.Reward(2)));
        }
    }
}
