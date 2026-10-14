using System;
using System.Collections.Generic;

namespace Swordstellation.Core
{
    public enum UpgradeBranch
    {
        Smithing,
        SwordControl,
        Guardianship
    }

    public sealed class UpgradeResult
    {
        public bool Success;
        public string MessageKey;
        public int NewLevel;
        public int Cost;
        public int CatalystCost;
        public int BossCoreCost;
    }

    public sealed class RunRewardResult
    {
        public int IronPowder;
        public int Experience;
        public int Starsteel;
        public int Catalysts;
        public int BossCores;
        public int LevelsGained;
        public bool DailyBonusAwarded;
        public string NewlyUnlockedStageId;
        public int NewStars;
        public List<string> NewlyUnlockedSwordIds = new List<string>();
    }

    public static class ProgressionService
    {
        private static readonly string[] StageOrder =
        {
            "stage_starlit_forge",
            "stage_frozen_clock",
            "stage_worldroot_void"
        };

        public static UpgradeResult TryUpgrade(PlayerSaveData save, UpgradeBranch branch)
        {
            if (save == null)
            {
                throw new ArgumentNullException(nameof(save));
            }

            int level = GetUpgradeLevel(save, branch);
            if (level >= RunBalanceRules.MaximumUpgradeLevel)
            {
                return new UpgradeResult
                {
                    Success = false,
                    MessageKey = "upgrade.max_level",
                    NewLevel = level
                };
            }

            int cost = RunBalanceRules.UpgradeCost(level);
            int catalystCost =
                RunBalanceRules.UpgradeCatalystCost(level);
            int bossCoreCost =
                RunBalanceRules.UpgradeBossCoreCost(level);
            if (save.IronPowder < cost)
            {
                return new UpgradeResult
                {
                    Success = false,
                    MessageKey = "upgrade.not_enough",
                    NewLevel = level,
                    Cost = cost
                };
            }
            if (save.Catalysts < catalystCost ||
                save.BossCores < bossCoreCost)
            {
                return new UpgradeResult
                {
                    Success = false,
                    MessageKey = "upgrade.not_enough_materials",
                    NewLevel = level,
                    Cost = cost,
                    CatalystCost = catalystCost,
                    BossCoreCost = bossCoreCost
                };
            }

            save.IronPowder -= cost;
            save.Catalysts -= catalystCost;
            save.BossCores -= bossCoreCost;
            SetUpgradeLevel(save, branch, level + 1);
            return new UpgradeResult
            {
                Success = true,
                MessageKey = "upgrade.success",
                NewLevel = level + 1,
                Cost = cost,
                CatalystCost = catalystCost,
                BossCoreCost = bossCoreCost
            };
        }

        public static UpgradeResult TryUpgradeSwordMastery(
            PlayerSaveData save,
            string swordId)
        {
            if (save == null)
            {
                throw new ArgumentNullException(nameof(save));
            }
            if (string.IsNullOrWhiteSpace(swordId) ||
                !save.UnlockedSwordIds.Contains(swordId))
            {
                return new UpgradeResult
                {
                    Success = false,
                    MessageKey = "upgrade.sword_locked"
                };
            }

            int level =
                save.GetSwordMasteryLevel(swordId);
            if (level >=
                RunBalanceRules.MaximumSwordMasteryLevel)
            {
                return new UpgradeResult
                {
                    Success = false,
                    MessageKey = "upgrade.max_level",
                    NewLevel = level
                };
            }

            int cost =
                RunBalanceRules.SwordMasteryCost(level);
            if (save.IronPowder < cost)
            {
                return new UpgradeResult
                {
                    Success = false,
                    MessageKey = "upgrade.not_enough",
                    NewLevel = level,
                    Cost = cost
                };
            }

            save.IronPowder -= cost;
            save.SetSwordMasteryLevel(
                swordId,
                level + 1);
            return new UpgradeResult
            {
                Success = true,
                MessageKey = "upgrade.success",
                NewLevel = level + 1,
                Cost = cost
            };
        }

        public static RunRewardResult ApplyRun(
            PlayerSaveData save,
            RunSummary summary,
            int localDateKey)
        {
            if (save == null)
            {
                throw new ArgumentNullException(nameof(save));
            }
            if (summary == null)
            {
                throw new ArgumentNullException(nameof(summary));
            }

            RunRewardResult reward = new RunRewardResult
            {
                IronPowder = Math.Max(20, summary.Score / 20),
                Experience = Math.Max(15, summary.DefeatedEnemies * 2),
                Catalysts = Math.Max(0, summary.DefeatedElites),
                BossCores = Math.Max(0, summary.DefeatedBosses)
            };

            save.IronPowder += reward.IronPowder;
            save.PlayerExperience += reward.Experience;
            save.Catalysts += reward.Catalysts;
            save.BossCores += reward.BossCores;
            save.BestScore = Math.Max(save.BestScore, summary.Score);
            save.HighestWave = Math.Max(save.HighestWave, summary.FinalWave);
            save.RunsPlayed++;
            save.TotalDamageDealt += Math.Max(
                0L,
                (long)Math.Round(summary.DamageDealt));
            save.TotalEnemiesDefeated += Math.Max(0, summary.DefeatedEnemies);
            save.TotalSwordsSummoned += Math.Max(0, summary.SwordsSummoned);
            save.TotalSuccessfulForges += Math.Max(0, summary.SuccessfulForges);
            save.TotalBossesDefeated += Math.Max(0, summary.DefeatedBosses);
            save.TotalHeroSkillsUsed += Math.Max(0, summary.HeroSkillsUsed);
            save.TotalUltimatesUsed += Math.Max(0, summary.UltimatesUsed);
            save.TotalEnemyAbilitiesTriggered +=
                Math.Max(0, summary.EnemyAbilitiesTriggered);

            if (summary.LearningStep > 0)
            {
                reward.NewStars = LearningPath.Record(
                    save,
                    summary.LearningStep,
                    summary.FinalWave,
                    summary.Victory,
                    LearningPath.Stars(summary.Victory, summary.PeakPressure));
                reward.IronPowder += reward.NewStars * LearningPath.IronPerStar;
                save.IronPowder += reward.NewStars * LearningPath.IronPerStar;
            }

            // Steps 1–6 only open the next step; step 7 or a full run clears the rift.
            bool clearsRift =
                summary.LearningStep == 0 ||
                summary.LearningStep == LearningPath.Steps.Count;
            if (summary.Victory)
            {
                save.Victories++;
            }
            if (summary.Victory && clearsRift)
            {
                reward.NewlyUnlockedStageId = CompleteStage(save, summary.StageId);
                UnlockStageBaseSwords(save, summary.StageId, reward);
            }

            while (save.PlayerExperience >=
                   RunBalanceRules.ExperienceForLevel(save.PlayerLevel))
            {
                save.PlayerExperience -=
                    RunBalanceRules.ExperienceForLevel(save.PlayerLevel);
                save.PlayerLevel++;
                save.Starsteel += 5;
                reward.Starsteel += 5;
                reward.LevelsGained++;
            }

            if (summary.Mode == RunMode.Daily &&
                summary.Victory &&
                save.DailyRunDate != localDateKey)
            {
                save.DailyRunDate = localDateKey;
                save.Starsteel += 10;
                reward.Starsteel += 10;
                reward.DailyBonusAwarded = true;
            }

            foreach (string recipeId in summary.DiscoveredRecipeIds ??
                                               new List<string>())
            {
                save.DiscoverRecipe(recipeId);
            }

            foreach (string runeId in summary.SelectedRuneIds ??
                                             new List<string>())
            {
                if (!save.SeenRuneIds.Contains(runeId))
                {
                    save.SeenRuneIds.Add(runeId);
                }
            }

            return reward;
        }

        private static void UnlockStageBaseSwords(
            PlayerSaveData save,
            string stageId,
            RunRewardResult reward)
        {
            string[] swordIds = stageId switch
            {
                "stage_starlit_forge" =>
                    new[] { "radiant_blade", "twilight_blade" },
                "stage_frozen_clock" =>
                    new[] { "mist_blade", "resonance_blade" },
                "stage_worldroot_void" =>
                    new[] { "iron_shard_blade", "glass_blade" },
                _ => Array.Empty<string>()
            };

            foreach (string swordId in swordIds)
            {
                if (save.UnlockSword(swordId))
                {
                    reward.NewlyUnlockedSwordIds.Add(swordId);
                    if (save.SelectedBaseSwordIds.Count < 6)
                    {
                        save.SelectedBaseSwordIds.Add(swordId);
                    }
                }
            }
        }

        public static bool IsStageUnlocked(PlayerSaveData save, string stageId)
        {
            if (save == null || string.IsNullOrWhiteSpace(stageId))
            {
                return false;
            }

            int index = Array.IndexOf(StageOrder, stageId);
            if (index <= 0)
            {
                return index == 0;
            }

            return save.CompletedStageIds.Contains(StageOrder[index - 1]);
        }

        private static string CompleteStage(PlayerSaveData save, string stageId)
        {
            bool newlyCompleted = false;
            if (!string.IsNullOrWhiteSpace(stageId) &&
                !save.CompletedStageIds.Contains(stageId))
            {
                save.CompletedStageIds.Add(stageId);
                newlyCompleted = true;
            }

            int index = Array.IndexOf(StageOrder, stageId);
            if (newlyCompleted &&
                index >= 0 &&
                index + 1 < StageOrder.Length &&
                !save.CompletedStageIds.Contains(StageOrder[index + 1]))
            {
                return StageOrder[index + 1];
            }

            return null;
        }

        private static int GetUpgradeLevel(PlayerSaveData save, UpgradeBranch branch)
        {
            return branch switch
            {
                UpgradeBranch.Smithing => save.SmithingLevel,
                UpgradeBranch.SwordControl => save.SwordControlLevel,
                UpgradeBranch.Guardianship => save.GuardianshipLevel,
                _ => throw new ArgumentOutOfRangeException(nameof(branch))
            };
        }

        private static void SetUpgradeLevel(
            PlayerSaveData save,
            UpgradeBranch branch,
            int value)
        {
            switch (branch)
            {
                case UpgradeBranch.Smithing:
                    save.SmithingLevel = value;
                    break;
                case UpgradeBranch.SwordControl:
                    save.SwordControlLevel = value;
                    break;
                case UpgradeBranch.Guardianship:
                    save.GuardianshipLevel = value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(branch));
            }
        }
    }
}
