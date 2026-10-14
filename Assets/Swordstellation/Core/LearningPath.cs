using System;
using System.Collections.Generic;
using System.Linq;

namespace Swordstellation.Core
{
    [Flags]
    public enum GameFeature
    {
        None = 0,
        Recipes = 1,
        Sell = 2,
        Missions = 4,
        Engravings = 8,
        Bosses = 16,
        Ultimate = 32,
        Treasure = 64,
        Constellations = 128,
        Focus = 256,
        All = Recipes | Sell | Missions | Engravings | Bosses | Ultimate | Treasure | Constellations | Focus
    }

    public sealed class LearningStep
    {
        public int Number { get; }
        public string Id => $"path_{Number}";
        public string NameKo { get; }
        public string NameEn { get; }
        public string IntroKo { get; }
        public string IntroEn { get; }
        public int Waves { get; }
        public float HealthScale { get; }
        public GameFeature NewFeatures { get; }
        public GameFeature Features { get; internal set; }

        public LearningStep(
            int number,
            string nameKo,
            string nameEn,
            string introKo,
            string introEn,
            int waves,
            float healthScale,
            GameFeature newFeatures)
        {
            Number = number;
            NameKo = nameKo;
            NameEn = nameEn;
            IntroKo = introKo;
            IntroEn = introEn;
            Waves = waves;
            HealthScale = healthScale;
            NewFeatures = newFeatures;
        }

        public bool Has(GameFeature feature)
        {
            return (Features & feature) == feature;
        }
    }

    // The first rift taught one system at a time; step 7 is the full rift.
    public static class LearningPath
    {
        public const string StageId = "stage_starlit_forge";
        // A clear earns ★1; holding the enemies lower earns ★2 / ★3.
        public const int TwoStarsBelow = 40;
        public const int ThreeStarsBelow = 25;
        public const int IronPerStar = 40;

        public static int Stars(bool victory, int peakPressure)
        {
            return !victory ? 0
                : peakPressure < ThreeStarsBelow ? 3
                : peakPressure < TwoStarsBelow ? 2
                : 1;
        }

        public static string StarText(int stars)
        {
            int filled = Math.Max(0, Math.Min(3, stars));
            return new string('★', filled) + new string('☆', 3 - filled);
        }

        public static int BestStars(PlayerSaveData save, int number)
        {
            LearningStep step = Get(number);
            return save?.PathBestWaves?
                .FirstOrDefault(record => record.Id == step?.Id)?.Stars ?? 0;
        }

        public static IReadOnlyList<LearningStep> Steps { get; } = Build();

        private static IReadOnlyList<LearningStep> Build()
        {
            List<LearningStep> steps = new List<LearningStep>
            {
                new LearningStep(1, "소환과 합성", "Summon and merge",
                    "소환 버튼으로 검을 불러요. 같은 검 두 개를 끌어 겹치면 별이 올라가요.\n적이 60마리 쌓이기 전에 5웨이브를 버티세요.",
                    "Tap Summon for swords. Drag two identical swords together to add a star.\nSurvive 5 waves before 60 enemies pile up.",
                    5, 0.7f, GameFeature.None),
                new LearningStep(2, "조합서로 제작", "Forge from the recipe book",
                    "서로 다른 검을 모으면 새 검을 만들 수 있어요.\n위쪽 줄에 \"눌러서 제작\"이 뜨면 누르세요. 조합서에서 조합법을 볼 수 있어요.",
                    "Different swords combine into new ones.\nWhen the strip above the board says \"Tap to forge\", tap it. The recipe book lists every recipe.",
                    6, 0.75f, GameFeature.Recipes),
                new LearningStep(3, "판매와 미션", "Selling and missions",
                    "필요 없는 검은 소환 버튼으로 끌어다 놓으면 골드로 바뀌어요.\n오른쪽 위 미션을 채우면 보너스 골드를 받아요.",
                    "Drag a sword you don't need onto Summon to sell it.\nFinish the mission at the top right for bonus gold.",
                    6, 0.8f, GameFeature.Sell | GameFeature.Missions),
                new LearningStep(4, "웨이브마다 각인", "An engraving every wave",
                    "웨이브가 끝날 때마다 각인 카드 3장 중 하나를 골라요.\n각인은 이번 판 내내 검을 강하게 해요. 전투력을 누르면 고른 각인이 보여요.",
                    "After each wave, pick 1 of 3 engraving cards.\nEngravings power up your swords for the whole run. Tap the power number to see them.",
                    8, 0.85f, GameFeature.Engravings),
                new LearningStep(5, "보스와 궁극기", "Bosses and the ultimate",
                    "5웨이브마다 보스가 나와요. 40초 안에 못 잡으면 폭주해요.\n아래 동그란 궁극기 버튼이 차면 눌러서 쓰세요. 보스를 잡으면 보물 상자가 열려요.",
                    "A boss arrives every 5 waves. Leave it 40 seconds and it rampages.\nTap the round ultimate button once it fills. Beating a boss opens a treasure chest.",
                    10, 1f, GameFeature.Bosses | GameFeature.Ultimate | GameFeature.Treasure),
                new LearningStep(6, "별자리와 집중", "Constellations and focus",
                    "같은 원소 검을 위아래·옆으로 붙이면 별자리가 되어 더 세져요.\n적을 누르면 모든 검이 그 적부터 노려요.",
                    "Same-element swords side by side form a constellation and hit harder.\nTap an enemy to make every sword target it first.",
                    10, 1f, GameFeature.Constellations | GameFeature.Focus),
                new LearningStep(7, "별빛 초원 완주", "Full Starlit Meadow",
                    "이제 모든 기능을 써서 15웨이브를 끝까지 지켜내세요.\n깨면 다음 균열이 열려요.",
                    "Use everything you've learned and hold all 15 waves.\nClear it to open the next rift.",
                    15, 1f, GameFeature.None)
            };

            GameFeature open = GameFeature.None;
            foreach (LearningStep step in steps)
            {
                open |= step.NewFeatures;
                step.Features = step.Number == steps.Count ? GameFeature.All : open;
            }
            return steps;
        }

        public static LearningStep Get(int number)
        {
            return number >= 1 && number <= Steps.Count ? Steps[number - 1] : null;
        }

        public static bool IsCleared(PlayerSaveData save, int number)
        {
            LearningStep step = Get(number);
            return save?.CompletedStageIds != null &&
                   step != null &&
                   (save.CompletedStageIds.Contains(step.Id) ||
                    // Saves that cleared the whole rift before steps existed.
                    save.CompletedStageIds.Contains(StageId));
        }

        public static bool IsUnlocked(PlayerSaveData save, int number)
        {
            return number == 1 || (Get(number) != null && IsCleared(save, number - 1));
        }

        public static bool IsComplete(PlayerSaveData save)
        {
            return IsCleared(save, Steps.Count);
        }

        // The first uncleared step, or 0 when the path is done.
        public static int NextStep(PlayerSaveData save)
        {
            LearningStep next = Steps.FirstOrDefault(step => !IsCleared(save, step.Number));
            return next?.Number ?? 0;
        }

        public static int BestWave(PlayerSaveData save, int number)
        {
            LearningStep step = Get(number);
            return save?.PathBestWaves?
                .FirstOrDefault(record => record.Id == step?.Id)?.Wave ?? 0;
        }

        // Returns how many stars this run added to the step's best.
        internal static int Record(PlayerSaveData save, int number, int wave, bool victory, int stars)
        {
            LearningStep step = Get(number);
            if (step == null)
            {
                return 0;
            }

            save.PathBestWaves ??= new List<PathRecord>();
            PathRecord record = save.PathBestWaves.FirstOrDefault(item => item.Id == step.Id);
            if (record == null)
            {
                record = new PathRecord { Id = step.Id };
                save.PathBestWaves.Add(record);
            }
            record.Wave = Math.Max(record.Wave, wave);
            int gained = Math.Max(0, stars - record.Stars);
            record.Stars += gained;

            if (victory && !save.CompletedStageIds.Contains(step.Id))
            {
                save.CompletedStageIds.Add(step.Id);
            }
            return gained;
        }
    }

    [Serializable]
    public sealed class PathRecord
    {
        public string Id;
        public int Wave;
        public int Stars;
    }
}
