using System;
using System.Collections.Generic;

namespace Swordstellation.Core
{
    [Serializable]
    public sealed class SwordMasteryRecord
    {
        public string SwordId;
        public int Level;
    }

    [Serializable]
    public sealed class PlayerSaveData
    {
        public const int CurrentSchemaVersion = 6;
        public const float DefaultMusicVolume = 0.28f;
        public const float DefaultSfxVolume = 0.72f;

        public int SchemaVersion = CurrentSchemaVersion;
        public string Locale = "ko";
        public bool TutorialCompleted;
        public float MusicVolume = DefaultMusicVolume;
        public float SfxVolume = DefaultSfxVolume;
        public bool ReducedMotion;
        public string SelectedUltimateId = "blade_rain";
        public int PlayerLevel = 1;
        public int PlayerExperience;
        public int IronPowder;
        public int Starsteel;
        public int UniversalBlueprintFragments;
        public int Catalysts;
        public int BossCores;
        public int SmithingLevel;
        public int SwordControlLevel;
        public int GuardianshipLevel;
        public int GachaPullsSinceHighRarity;
        public int BestScore;
        public int HighestWave;
        public int DailyRunDate;
        public int RunsPlayed;
        public int Victories;
        public long TotalDamageDealt;
        public int TotalEnemiesDefeated;
        public int TotalSwordsSummoned;
        public int TotalSuccessfulForges;
        public int TotalBossesDefeated;
        public int TotalHeroSkillsUsed;
        public int TotalUltimatesUsed;
        public int TotalEnemyAbilitiesTriggered;
        public List<string> UnlockedSwordIds = new List<string>();
        public List<string> SelectedBaseSwordIds = new List<string>();
        public List<string> DiscoveredRecipeIds = new List<string>();
        public List<string> SeenEnemyIds = new List<string>();
        public List<string> SeenRuneIds = new List<string>();
        public List<string> CompletedStageIds = new List<string>();
        public List<string> OwnedAppearanceIds = new List<string>();
        public List<BlueprintFragmentRecord> BlueprintFragments = new List<BlueprintFragmentRecord>();
        public List<SwordMasteryRecord> SwordMasteries =
            new List<SwordMasteryRecord>();
        public List<PathRecord> PathBestWaves = new List<PathRecord>();

        public static PlayerSaveData CreateNew()
        {
            PlayerSaveData data = new PlayerSaveData
            {
                IronPowder = 250,
                Starsteel = 50
            };

            string[] startingSwords =
            {
                "ember_blade",
                "frost_blade",
                "gale_blade",
                "thunder_blade",
                "stone_blade",
                "sprout_blade"
            };
            data.UnlockedSwordIds.AddRange(startingSwords);
            data.SelectedBaseSwordIds.AddRange(startingSwords);
            return data;
        }

        public void Normalize()
        {
            SchemaVersion = CurrentSchemaVersion;
            Locale = Locale == "en" ? "en" : "ko";
            MusicVolume = Clamp01(MusicVolume);
            SfxVolume = Clamp01(SfxVolume);
            bool validUltimate = false;
            foreach (UltimateDefinition ultimate in
                     RunContentCatalog.Ultimates)
            {
                if (ultimate.Id == SelectedUltimateId)
                {
                    validUltimate = true;
                    break;
                }
            }
            if (!validUltimate)
            {
                SelectedUltimateId = "blade_rain";
            }

            PlayerLevel = Math.Max(1, PlayerLevel);
            PlayerExperience = Math.Max(0, PlayerExperience);
            IronPowder = Math.Max(0, IronPowder);
            Starsteel = Math.Max(0, Starsteel);
            UniversalBlueprintFragments = Math.Max(0, UniversalBlueprintFragments);
            Catalysts = Math.Max(0, Catalysts);
            BossCores = Math.Max(0, BossCores);
            SmithingLevel = ClampUpgrade(SmithingLevel);
            SwordControlLevel = ClampUpgrade(SwordControlLevel);
            GuardianshipLevel = ClampUpgrade(GuardianshipLevel);
            GachaPullsSinceHighRarity = Math.Max(0, GachaPullsSinceHighRarity);
            BestScore = Math.Max(0, BestScore);
            HighestWave = Math.Max(0, HighestWave);
            RunsPlayed = Math.Max(0, RunsPlayed);
            Victories = Math.Max(0, Math.Min(Victories, RunsPlayed));
            TotalDamageDealt = Math.Max(0L, TotalDamageDealt);
            TotalEnemiesDefeated = Math.Max(0, TotalEnemiesDefeated);
            TotalSwordsSummoned = Math.Max(0, TotalSwordsSummoned);
            TotalSuccessfulForges = Math.Max(0, TotalSuccessfulForges);
            TotalBossesDefeated = Math.Max(0, TotalBossesDefeated);
            TotalHeroSkillsUsed = Math.Max(0, TotalHeroSkillsUsed);
            TotalUltimatesUsed = Math.Max(0, TotalUltimatesUsed);
            TotalEnemyAbilitiesTriggered =
                Math.Max(0, TotalEnemyAbilitiesTriggered);

            UnlockedSwordIds = Unique(UnlockedSwordIds);
            if (UnlockedSwordIds.Count == 0)
            {
                UnlockedSwordIds.AddRange(new[]
                {
                    "ember_blade",
                    "frost_blade",
                    "gale_blade",
                    "thunder_blade",
                    "stone_blade",
                    "sprout_blade"
                });
            }
            SelectedBaseSwordIds = Unique(SelectedBaseSwordIds);
            SelectedBaseSwordIds.RemoveAll(
                id => !UnlockedSwordIds.Contains(id));
            if (SelectedBaseSwordIds.Count == 0)
            {
                SelectedBaseSwordIds.AddRange(
                    UnlockedSwordIds.GetRange(
                        0,
                        Math.Min(6, UnlockedSwordIds.Count)));
            }
            if (SelectedBaseSwordIds.Count > 6)
            {
                SelectedBaseSwordIds.RemoveRange(
                    6,
                    SelectedBaseSwordIds.Count - 6);
            }
            DiscoveredRecipeIds = Unique(DiscoveredRecipeIds);
            SeenEnemyIds = Unique(SeenEnemyIds);
            SeenRuneIds = Unique(SeenRuneIds);
            CompletedStageIds = Unique(CompletedStageIds);
            OwnedAppearanceIds = Unique(OwnedAppearanceIds);
            BlueprintFragments ??= new List<BlueprintFragmentRecord>();
            BlueprintFragments.RemoveAll(record =>
                record == null || string.IsNullOrWhiteSpace(record.SwordId));
            List<BlueprintFragmentRecord> mergedFragments =
                new List<BlueprintFragmentRecord>();
            foreach (BlueprintFragmentRecord record in BlueprintFragments)
            {
                BlueprintFragmentRecord existing = mergedFragments.Find(
                    item => item.SwordId == record.SwordId);
                if (existing == null)
                {
                    mergedFragments.Add(new BlueprintFragmentRecord
                    {
                        SwordId = record.SwordId,
                        Fragments = Math.Max(0, record.Fragments)
                    });
                }
                else
                {
                    existing.Fragments += Math.Max(0, record.Fragments);
                }
            }
            BlueprintFragments = mergedFragments;
            PathBestWaves ??= new List<PathRecord>();
            PathBestWaves.RemoveAll(record => record == null || string.IsNullOrWhiteSpace(record.Id));

            SwordMasteries ??=
                new List<SwordMasteryRecord>();
            SwordMasteries.RemoveAll(record =>
                record == null ||
                string.IsNullOrWhiteSpace(record.SwordId));
            List<SwordMasteryRecord> mergedMasteries =
                new List<SwordMasteryRecord>();
            foreach (SwordMasteryRecord record in
                     SwordMasteries)
            {
                SwordMasteryRecord existing =
                    mergedMasteries.Find(item =>
                        item.SwordId == record.SwordId);
                int level = Math.Max(
                    0,
                    Math.Min(
                        RunBalanceRules
                            .MaximumSwordMasteryLevel,
                        record.Level));
                if (existing == null)
                {
                    mergedMasteries.Add(
                        new SwordMasteryRecord
                        {
                            SwordId = record.SwordId,
                            Level = level
                        });
                }
                else
                {
                    existing.Level = Math.Max(
                        existing.Level,
                        level);
                }
            }
            SwordMasteries = mergedMasteries;
        }

        public int GetSwordMasteryLevel(string swordId)
        {
            SwordMasteryRecord record =
                SwordMasteries?.Find(item =>
                    item != null &&
                    item.SwordId == swordId);
            return record?.Level ?? 0;
        }

        public void SetSwordMasteryLevel(
            string swordId,
            int level)
        {
            if (string.IsNullOrWhiteSpace(swordId))
            {
                throw new ArgumentException(
                    "A sword id is required.",
                    nameof(swordId));
            }

            SwordMasteries ??=
                new List<SwordMasteryRecord>();
            SwordMasteryRecord record =
                SwordMasteries.Find(item =>
                    item.SwordId == swordId);
            if (record == null)
            {
                record = new SwordMasteryRecord
                {
                    SwordId = swordId
                };
                SwordMasteries.Add(record);
            }
            record.Level = Math.Max(
                0,
                Math.Min(
                    RunBalanceRules
                        .MaximumSwordMasteryLevel,
                    level));
        }

        public bool UnlockSword(string swordId)
        {
            if (UnlockedSwordIds.Contains(swordId))
            {
                return false;
            }

            UnlockedSwordIds.Add(swordId);
            return true;
        }

        public bool DiscoverRecipe(string recipeId)
        {
            if (string.IsNullOrWhiteSpace(recipeId))
            {
                return false;
            }
            if (DiscoveredRecipeIds.Contains(recipeId))
            {
                return false;
            }

            DiscoveredRecipeIds.Add(recipeId);
            return true;
        }

        private static int ClampUpgrade(int value)
        {
            return Math.Max(0, Math.Min(RunBalanceRules.MaximumUpgradeLevel, value));
        }

        private static float Clamp01(float value)
        {
            return Math.Max(0f, Math.Min(1f, value));
        }

        private static List<string> Unique(List<string> source)
        {
            List<string> result = new List<string>();
            if (source == null)
            {
                return result;
            }

            foreach (string value in source)
            {
                if (!string.IsNullOrWhiteSpace(value) && !result.Contains(value))
                {
                    result.Add(value);
                }
            }
            return result;
        }
    }
}
