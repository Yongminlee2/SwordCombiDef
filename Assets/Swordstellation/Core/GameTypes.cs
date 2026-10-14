using System;
using System.Collections.Generic;

namespace Swordstellation.Core
{
    public enum SwordRarity
    {
        Common,
        Rare,
        Heroic,
        Legendary,
        Mythic
    }

    public enum SwordElement
    {
        Neutral,
        Flame,
        Frost,
        Gale,
        Thunder,
        Earth,
        Nature,
        Light,
        Shadow
    }

    public enum AttackStyle
    {
        Dash,
        Projectile,
        Chain,
        Area
    }

    public enum RecipeVisibility
    {
        Public,
        Silhouette,
        Riddle,
        Hidden
    }

    public enum HiddenRecipeCondition
    {
        None,
        AllIngredientsTogether,
        AnyIngredientMaximumRank
    }

    public enum RunMode
    {
        Standard,
        Daily,
        Endless
    }

    [Serializable]
    public sealed class SwordDefinition
    {
        public string Id;
        public string NameKo;
        public string NameEn;
        public SwordRarity Rarity;
        public SwordElement Element;
        public AttackStyle AttackStyle;
        public float BaseDamage;
        public float AttackInterval;
        public int TargetCount;
        public int ColorRgb;

        public SwordDefinition(
            string id,
            string nameKo,
            string nameEn,
            SwordRarity rarity,
            SwordElement element,
            AttackStyle attackStyle,
            float baseDamage,
            float attackInterval,
            int targetCount,
            int colorRgb)
        {
            Id = id;
            NameKo = nameKo;
            NameEn = nameEn;
            Rarity = rarity;
            Element = element;
            AttackStyle = attackStyle;
            BaseDamage = baseDamage;
            AttackInterval = attackInterval;
            TargetCount = targetCount;
            ColorRgb = colorRgb;
        }
    }

    [Serializable]
    public sealed class RecipeDefinition
    {
        public string Id;
        public string ResultSwordId;
        public string[] IngredientSwordIds;
        public RecipeVisibility Visibility;
        public HiddenRecipeCondition HiddenCondition;
        public string HintKo;
        public string HintEn;

        public RecipeDefinition(
            string id,
            string resultSwordId,
            string[] ingredientSwordIds,
            RecipeVisibility visibility,
            string hintKo,
            string hintEn,
            HiddenRecipeCondition hiddenCondition =
                HiddenRecipeCondition.None)
        {
            Id = id;
            ResultSwordId = resultSwordId;
            IngredientSwordIds = ingredientSwordIds;
            Visibility = visibility;
            HiddenCondition = hiddenCondition;
            HintKo = hintKo;
            HintEn = hintEn;
        }
    }

    [Serializable]
    public sealed class EnemyDefinition
    {
        public string Id;
        public string NameKo;
        public string NameEn;
        public float BaseHealth;
        public float MoveSpeed;
        public int CoreDamage;
        public int Reward;
        public bool IsElite;
        public bool IsBoss;
        public int ColorRgb;

        public EnemyDefinition(
            string id,
            string nameKo,
            string nameEn,
            float baseHealth,
            float moveSpeed,
            int coreDamage,
            int reward,
            bool isElite,
            bool isBoss,
            int colorRgb)
        {
            Id = id;
            NameKo = nameKo;
            NameEn = nameEn;
            BaseHealth = baseHealth;
            MoveSpeed = moveSpeed;
            CoreDamage = coreDamage;
            Reward = reward;
            IsElite = isElite;
            IsBoss = isBoss;
            ColorRgb = colorRgb;
        }
    }

    [Serializable]
    public sealed class StageDefinition
    {
        public string Id;
        public string NameKo;
        public string NameEn;
        public string DescriptionKo;
        public string DescriptionEn;
        public string[] NormalEnemyIds;
        public string[] EliteEnemyIds;
        public string BossEnemyId;
        public int BackgroundRgb;

        public StageDefinition(
            string id,
            string nameKo,
            string nameEn,
            string descriptionKo,
            string descriptionEn,
            string[] normalEnemyIds,
            string[] eliteEnemyIds,
            string bossEnemyId,
            int backgroundRgb)
        {
            Id = id;
            NameKo = nameKo;
            NameEn = nameEn;
            DescriptionKo = descriptionKo;
            DescriptionEn = descriptionEn;
            NormalEnemyIds = normalEnemyIds;
            EliteEnemyIds = eliteEnemyIds;
            BossEnemyId = bossEnemyId;
            BackgroundRgb = backgroundRgb;
        }
    }

    [Serializable]
    public sealed class SwordInstance
    {
        public string InstanceId;
        public string DefinitionId;
        public int Stars;
        public ulong AppearanceSeed;
        public float Cooldown;

        public SwordInstance(string definitionId, int stars, ulong appearanceSeed, string instanceId)
        {
            DefinitionId = definitionId;
            Stars = stars;
            AppearanceSeed = appearanceSeed;
            InstanceId = instanceId;
        }
    }

    public sealed class ForgeResult
    {
        public bool Success { get; }
        public string MessageKey { get; }
        public SwordInstance Result { get; }

        private ForgeResult(bool success, string messageKey, SwordInstance result)
        {
            Success = success;
            MessageKey = messageKey;
            Result = result;
        }

        public static ForgeResult Failed(string messageKey)
        {
            return new ForgeResult(false, messageKey, null);
        }

        public static ForgeResult Succeeded(string messageKey, SwordInstance result)
        {
            return new ForgeResult(true, messageKey, result);
        }
    }

    [Serializable]
    public sealed class RunSummary
    {
        public ulong Seed;
        public string ContentVersion;
        public string StageId;
        public RunMode Mode;
        // 1–7 for a learning-path step, 0 for a full rift run.
        public int LearningStep;
        // Highest enemy count (rampaging bosses weigh more) during the run.
        public int PeakPressure;
        public int FinalWave;
        public int Score;
        public int DefeatedEnemies;
        public int DefeatedElites;
        public int DefeatedBosses;
        public float DurationSeconds;
        public bool Victory;
        public string SelectedUltimateId;
        public float DamageDealt;
        public int DamageTaken;
        public int SwordsSummoned;
        public int ForgeAttempts;
        public int SuccessfulForges;
        public int StarUpgrades;
        public int RecipeForges;
        public int UltimatesUsed;
        public int HeroSkillsUsed;
        public int EnemyAbilitiesTriggered;
        public int PeakSwordStars;
        public List<string> DiscoveredRecipeIds = new List<string>();
        public List<string> SelectedRuneIds = new List<string>();
    }
}
