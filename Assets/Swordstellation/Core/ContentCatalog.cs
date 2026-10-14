using System;
using System.Collections.Generic;
using System.Linq;

namespace Swordstellation.Core
{
    public sealed class ContentCatalog
    {
        public const string Version = "1.3.0";

        private readonly Dictionary<string, SwordDefinition> _swords =
            new Dictionary<string, SwordDefinition>(StringComparer.Ordinal);
        private readonly Dictionary<string, EnemyDefinition> _enemies =
            new Dictionary<string, EnemyDefinition>(StringComparer.Ordinal);
        private readonly Dictionary<string, StageDefinition> _stages =
            new Dictionary<string, StageDefinition>(StringComparer.Ordinal);
        private readonly List<RecipeDefinition> _recipes = new List<RecipeDefinition>();
        private readonly Dictionary<string, RecipeDefinition> _recipeByIngredients =
            new Dictionary<string, RecipeDefinition>(StringComparer.Ordinal);

        public IReadOnlyDictionary<string, SwordDefinition> Swords => _swords;
        public IReadOnlyDictionary<string, EnemyDefinition> Enemies => _enemies;
        public IReadOnlyDictionary<string, StageDefinition> Stages => _stages;
        public IReadOnlyList<RecipeDefinition> Recipes => _recipes;

        public IReadOnlyList<string> BaseSwordIds { get; private set; }
        public IReadOnlyList<string> NormalEnemyIds { get; private set; }
        public IReadOnlyList<string> EliteEnemyIds { get; private set; }
        public IReadOnlyList<string> BossEnemyIds { get; private set; }

        public ContentCatalog()
        {
            BuildSwords();
            BuildRecipes();
            BuildEnemies();
            BuildStages();

            BaseSwordIds = _swords.Values
                .Where(sword => sword.Rarity == SwordRarity.Common)
                .Select(sword => sword.Id)
                .ToArray();
            NormalEnemyIds = _enemies.Values
                .Where(enemy => !enemy.IsElite && !enemy.IsBoss)
                .Select(enemy => enemy.Id)
                .ToArray();
            EliteEnemyIds = _enemies.Values
                .Where(enemy => enemy.IsElite && !enemy.IsBoss)
                .Select(enemy => enemy.Id)
                .ToArray();
            BossEnemyIds = _enemies.Values
                .Where(enemy => enemy.IsBoss)
                .Select(enemy => enemy.Id)
                .ToArray();
        }

        public SwordDefinition GetSword(string id)
        {
            return _swords.TryGetValue(id, out SwordDefinition sword) ? sword : null;
        }

        public EnemyDefinition GetEnemy(string id)
        {
            return _enemies.TryGetValue(id, out EnemyDefinition enemy) ? enemy : null;
        }

        public StageDefinition GetStage(string id)
        {
            return _stages.TryGetValue(id, out StageDefinition stage) ? stage : null;
        }

        public RecipeDefinition GetRecipe(string id)
        {
            return _recipes.FirstOrDefault(recipe => recipe.Id == id);
        }

        public RecipeDefinition FindRecipe(IEnumerable<string> ingredientSwordIds)
        {
            string key = RecipeKey(ingredientSwordIds);
            return _recipeByIngredients.TryGetValue(key, out RecipeDefinition recipe) ? recipe : null;
        }

        private void BuildSwords()
        {
            // Twelve base families
            AddSword("ember_blade", "불씨검", "Ember Blade", SwordRarity.Common, SwordElement.Flame, AttackStyle.Dash, 16, 1.15f, 1, 0xF0643A);
            AddSword("frost_blade", "서리검", "Frost Blade", SwordRarity.Common, SwordElement.Frost, AttackStyle.Projectile, 13, 1.00f, 1, 0x73C8F2);
            AddSword("gale_blade", "질풍검", "Gale Blade", SwordRarity.Common, SwordElement.Gale, AttackStyle.Dash, 10, 0.72f, 1, 0x79D9B4);
            AddSword("thunder_blade", "뇌광검", "Thunder Blade", SwordRarity.Common, SwordElement.Thunder, AttackStyle.Chain, 12, 1.05f, 2, 0xE8D84A);
            AddSword("stone_blade", "암석검", "Stone Blade", SwordRarity.Common, SwordElement.Earth, AttackStyle.Dash, 24, 1.55f, 1, 0xA9815E);
            AddSword("sprout_blade", "새싹검", "Sprout Blade", SwordRarity.Common, SwordElement.Nature, AttackStyle.Projectile, 11, 1.08f, 1, 0x6BCB63);
            AddSword("radiant_blade", "광휘검", "Radiant Blade", SwordRarity.Common, SwordElement.Light, AttackStyle.Projectile, 15, 1.20f, 1, 0xFFE8A3);
            AddSword("twilight_blade", "황혼검", "Twilight Blade", SwordRarity.Common, SwordElement.Shadow, AttackStyle.Dash, 18, 1.38f, 1, 0x8E69C7);
            AddSword("mist_blade", "안개검", "Mist Blade", SwordRarity.Common, SwordElement.Nature, AttackStyle.Area, 8, 1.45f, 3, 0x77BCA7);
            AddSword("resonance_blade", "공명검", "Resonance Blade", SwordRarity.Common, SwordElement.Neutral, AttackStyle.Chain, 10, 1.10f, 2, 0xB58BDB);
            AddSword("iron_shard_blade", "철편검", "Iron Shard Blade", SwordRarity.Common, SwordElement.Neutral, AttackStyle.Projectile, 14, 0.95f, 1, 0x9DA7B3);
            AddSword("glass_blade", "유리검", "Glass Blade", SwordRarity.Common, SwordElement.Light, AttackStyle.Projectile, 22, 1.48f, 1, 0xBDEBEE);

            // Twenty rare recipes
            AddSword("scarlet_cyclone", "홍련회전검", "Scarlet Cyclone", SwordRarity.Rare, SwordElement.Flame, AttackStyle.Area, 26, 1.00f, 3, 0xF24B36);
            AddSword("frost_thunder_blade", "빙뢰파검", "Frost Thunder Blade", SwordRarity.Rare, SwordElement.Thunder, AttackStyle.Chain, 25, 1.05f, 3, 0x79BDEF);
            AddSword("magma_greatsword", "용암대검", "Magma Greatsword", SwordRarity.Rare, SwordElement.Earth, AttackStyle.Area, 38, 1.65f, 3, 0xD65735);
            AddSword("storm_flying_blade", "폭풍비검", "Storm Flying Blade", SwordRarity.Rare, SwordElement.Gale, AttackStyle.Chain, 22, 0.70f, 3, 0x63D2CB);
            AddSword("snowbloom_woodblade", "설화목검", "Snowbloom Woodblade", SwordRarity.Rare, SwordElement.Frost, AttackStyle.Area, 22, 1.15f, 3, 0xB7E5D2);
            AddSword("life_saint_blade", "생명의 성검", "Life Saint Blade", SwordRarity.Rare, SwordElement.Light, AttackStyle.Projectile, 25, 1.10f, 2, 0xD7F28A);
            AddSword("mountain_cleaver", "산맥참검", "Mountain Cleaver", SwordRarity.Rare, SwordElement.Earth, AttackStyle.Dash, 44, 1.70f, 1, 0x8A735E);
            AddSword("prismatic_blade", "칠채광검", "Prismatic Blade", SwordRarity.Rare, SwordElement.Light, AttackStyle.Chain, 27, 1.05f, 3, 0xE8B7F2);
            AddSword("moonless_mirror", "무월경검", "Moonless Mirror Blade", SwordRarity.Rare, SwordElement.Shadow, AttackStyle.Projectile, 30, 1.12f, 2, 0x665084);
            AddSword("azure_mist_blade", "청연무검", "Azure Mist Blade", SwordRarity.Rare, SwordElement.Nature, AttackStyle.Area, 20, 0.98f, 4, 0x4FACA2);
            AddSword("white_mist_blade", "백무설검", "White Mist Blade", SwordRarity.Rare, SwordElement.Frost, AttackStyle.Area, 20, 1.00f, 4, 0xC6EDF0);
            AddSword("thunder_resonance", "천둥공명검", "Thunder Resonance", SwordRarity.Rare, SwordElement.Thunder, AttackStyle.Chain, 28, 0.90f, 3, 0xE3C858);
            AddSword("soundless_gale", "무형음속검", "Soundless Gale", SwordRarity.Rare, SwordElement.Gale, AttackStyle.Dash, 25, 0.55f, 1, 0xA3E9DA);
            AddSword("shard_sky_blade", "파편천검", "Shard Sky Blade", SwordRarity.Rare, SwordElement.Neutral, AttackStyle.Projectile, 20, 0.80f, 3, 0xD2DAE4);
            AddSword("steam_echo_blade", "증기명검", "Steam Echo Blade", SwordRarity.Rare, SwordElement.Neutral, AttackStyle.Area, 30, 1.18f, 4, 0xA8D6D3);
            AddSword("dawnroot_blade", "새벽뿌리검", "Dawnroot Blade", SwordRarity.Rare, SwordElement.Nature, AttackStyle.Projectile, 29, 1.05f, 2, 0xAEDB70);
            AddSword("dusk_gale_blade", "황혼질풍검", "Dusk Gale Blade", SwordRarity.Rare, SwordElement.Shadow, AttackStyle.Dash, 33, 0.82f, 2, 0x816BB4);
            AddSword("stormstone_blade", "폭풍암검", "Stormstone Blade", SwordRarity.Rare, SwordElement.Earth, AttackStyle.Area, 36, 1.30f, 3, 0x738D83);
            AddSword("sunshard_blade", "태양파편검", "Sunshard Blade", SwordRarity.Rare, SwordElement.Light, AttackStyle.Projectile, 35, 1.20f, 3, 0xFFD66E);
            AddSword("ember_echo_blade", "불꽃공명검", "Ember Echo Blade", SwordRarity.Rare, SwordElement.Flame, AttackStyle.Chain, 31, 0.92f, 3, 0xEE7653);

            // Sixteen heroic recipes
            AddSword("starfall_blade", "낙성검", "Starfall Blade", SwordRarity.Heroic, SwordElement.Light, AttackStyle.Area, 54, 1.10f, 4, 0xF8D37B);
            AddSword("aurora_edge", "극광검", "Aurora Edge", SwordRarity.Heroic, SwordElement.Frost, AttackStyle.Chain, 48, 0.90f, 4, 0x8ED8F6);
            AddSword("verdant_tempest", "녹풍신검", "Verdant Tempest", SwordRarity.Heroic, SwordElement.Nature, AttackStyle.Area, 45, 0.85f, 5, 0x63D482);
            AddSword("twilight_thunder", "황혼뇌검", "Twilight Thunder", SwordRarity.Heroic, SwordElement.Shadow, AttackStyle.Chain, 58, 0.95f, 4, 0x8D68C6);
            AddSword("crystal_winter", "수정동검", "Crystal Winter", SwordRarity.Heroic, SwordElement.Frost, AttackStyle.Projectile, 65, 1.20f, 4, 0xB9E9F8);
            AddSword("solar_guard", "태양수호검", "Solar Guard", SwordRarity.Heroic, SwordElement.Light, AttackStyle.Area, 52, 1.00f, 5, 0xFFD46B);
            AddSword("lunar_echo", "월광공명검", "Lunar Echo", SwordRarity.Heroic, SwordElement.Shadow, AttackStyle.Chain, 55, 0.92f, 5, 0x9583CD);
            AddSword("dragonbreath_blade", "용숨결검", "Dragonbreath Blade", SwordRarity.Heroic, SwordElement.Flame, AttackStyle.Area, 70, 1.28f, 4, 0xEE6746);
            AddSword("terra_bloom", "대지개화검", "Terra Bloom", SwordRarity.Heroic, SwordElement.Earth, AttackStyle.Area, 62, 1.25f, 5, 0x9AB56E);
            AddSword("phantom_gale", "환영질풍검", "Phantom Gale", SwordRarity.Heroic, SwordElement.Gale, AttackStyle.Dash, 49, 0.48f, 3, 0x9BDCCE);
            AddSword("celestial_shard", "천상파편검", "Celestial Shard", SwordRarity.Heroic, SwordElement.Light, AttackStyle.Projectile, 60, 0.72f, 5, 0xE7EAF7);
            AddSword("storm_prism", "폭풍프리즘검", "Storm Prism", SwordRarity.Heroic, SwordElement.Thunder, AttackStyle.Chain, 57, 0.68f, 5, 0xC9E66B);
            AddSword("frozen_mountain", "빙산대검", "Frozen Mountain", SwordRarity.Heroic, SwordElement.Earth, AttackStyle.Dash, 86, 1.55f, 2, 0x85A9B7);
            AddSword("forest_chorus", "숲의합창검", "Forest Chorus", SwordRarity.Heroic, SwordElement.Nature, AttackStyle.Chain, 50, 0.84f, 5, 0x72C995);
            AddSword("sunset_mirage", "노을신기루검", "Sunset Mirage", SwordRarity.Heroic, SwordElement.Flame, AttackStyle.Projectile, 68, 0.98f, 4, 0xED8A69);
            AddSword("magnetic_anvil", "자력모루검", "Magnetic Anvil", SwordRarity.Heroic, SwordElement.Neutral, AttackStyle.Area, 74, 1.20f, 5, 0x8395AC);

            // Eight legendary recipes
            AddSword("eclipse_twins", "일식쌍검", "Eclipse Twins", SwordRarity.Legendary, SwordElement.Shadow, AttackStyle.Chain, 105, 0.60f, 7, 0x6D5AA8);
            AddSword("genesis_forgeblade", "태초의 대장검", "Genesis Forgeblade", SwordRarity.Legendary, SwordElement.Earth, AttackStyle.Area, 135, 1.10f, 7, 0xD9A85C);
            AddSword("astral_chorus", "성진합창검", "Astral Chorus", SwordRarity.Legendary, SwordElement.Light, AttackStyle.Projectile, 112, 0.55f, 7, 0xFFE3A1);
            AddSword("void_blossom", "공허개화검", "Void Blossom", SwordRarity.Legendary, SwordElement.Shadow, AttackStyle.Area, 120, 0.82f, 8, 0x7453A7);
            AddSword("heavenfall_blade", "천락검", "Heavenfall Blade", SwordRarity.Legendary, SwordElement.Thunder, AttackStyle.Area, 145, 1.05f, 8, 0xEBD963);
            AddSword("worldroot_blade", "세계수근검", "Worldroot Blade", SwordRarity.Legendary, SwordElement.Nature, AttackStyle.Chain, 118, 0.72f, 8, 0x78D080);
            AddSword("aurora_sovereign", "극광군주검", "Aurora Sovereign", SwordRarity.Legendary, SwordElement.Frost, AttackStyle.Chain, 128, 0.66f, 8, 0x9DDCF7);
            AddSword("dimensional_edge", "차원절단검", "Dimensional Edge", SwordRarity.Legendary, SwordElement.Neutral, AttackStyle.Dash, 175, 0.80f, 4, 0xD8C6F3);

            // Four mythic recipes
            AddSword("infinite_orbit", "무한궤도검", "Infinite Orbit", SwordRarity.Mythic, SwordElement.Light, AttackStyle.Chain, 220, 0.42f, 9, 0xFFE7A6);
            AddSword("first_anvil", "첫모루의 검", "Blade of the First Anvil", SwordRarity.Mythic, SwordElement.Earth, AttackStyle.Area, 280, 0.92f, 9, 0xDF9B55);
            AddSword("sword_constellation", "검의 별자리", "Sword Constellation", SwordRarity.Mythic, SwordElement.Neutral, AttackStyle.Projectile, 235, 0.38f, 9, 0xE9D7FF);
            AddSword("forge_dream", "대장간의 꿈", "Dream of the Forge", SwordRarity.Mythic, SwordElement.Nature, AttackStyle.Area, 250, 0.60f, 9, 0x9BE8B0);
        }

        private void BuildRecipes()
        {
            AddRecipe("recipe_scarlet_cyclone", "scarlet_cyclone", RecipeVisibility.Public, "불꽃은 바람을 타고 회전한다.", "Flame turns with the wind.", "ember_blade", "gale_blade");
            AddRecipe("recipe_frost_thunder", "frost_thunder_blade", RecipeVisibility.Public, "얼어붙은 번개가 갈라진다.", "Frozen thunder breaks.", "frost_blade", "thunder_blade");
            AddRecipe("recipe_magma_greatsword", "magma_greatsword", RecipeVisibility.Public, "불씨를 바위 속에 가둔다.", "Seal an ember inside stone.", "ember_blade", "stone_blade");
            AddRecipe("recipe_storm_flying", "storm_flying_blade", RecipeVisibility.Public, "바람과 번개는 가장 빠른 길을 찾는다.", "Wind and thunder find the fastest path.", "gale_blade", "thunder_blade");
            AddRecipe("recipe_snowbloom", "snowbloom_woodblade", RecipeVisibility.Silhouette, "눈 속에서도 꽃은 피어난다.", "A flower blooms even in snow.", "sprout_blade", "frost_blade");
            AddRecipe("recipe_life_saint", "life_saint_blade", RecipeVisibility.Silhouette, "새싹이 빛을 품는다.", "A sprout holds the light.", "sprout_blade", "radiant_blade");
            AddRecipe("recipe_mountain_cleaver", "mountain_cleaver", RecipeVisibility.Public, "철은 산을 가른다.", "Iron parts the mountain.", "iron_shard_blade", "stone_blade");
            AddRecipe("recipe_prismatic", "prismatic_blade", RecipeVisibility.Silhouette, "유리에 일곱 빛을 비춘다.", "Shine seven lights through glass.", "glass_blade", "radiant_blade");
            AddRecipe("recipe_moonless_mirror", "moonless_mirror", RecipeVisibility.Riddle, "달이 없는 밤, 거울은 무엇을 비추는가?", "What does a mirror show on a moonless night?", "glass_blade", "twilight_blade");
            AddRecipe("recipe_azure_mist", "azure_mist_blade", RecipeVisibility.Public, "안개 속 불씨는 푸르게 남는다.", "An ember in mist burns azure.", "mist_blade", "ember_blade");
            AddRecipe("recipe_white_mist", "white_mist_blade", RecipeVisibility.Silhouette, "차가운 안개가 길을 지운다.", "Cold mist erases the path.", "mist_blade", "frost_blade");
            AddRecipe("recipe_thunder_resonance", "thunder_resonance", RecipeVisibility.Public, "울림은 번개를 부른다.", "Resonance calls lightning.", "resonance_blade", "thunder_blade");
            AddRecipe("recipe_soundless_gale", "soundless_gale", RecipeVisibility.Riddle, "소리보다 먼저 도착하는 바람.", "The wind that arrives before sound.", "resonance_blade", "gale_blade");
            AddRecipe("recipe_shard_sky", "shard_sky_blade", RecipeVisibility.Silhouette, "깨진 하늘은 수많은 칼날이 된다.", "A broken sky becomes countless blades.", "iron_shard_blade", "glass_blade");
            AddRecipe("recipe_steam_echo", "steam_echo_blade", RecipeVisibility.Riddle, "불과 얼음 사이에서 울리는 숨.", "A breath echoing between flame and frost.", "ember_blade", "frost_blade", "resonance_blade");
            AddRecipe("recipe_dawnroot", "dawnroot_blade", RecipeVisibility.Silhouette, "빛을 향해 자라는 뿌리.", "A root growing toward light.", "sprout_blade", "radiant_blade", "stone_blade");
            AddRecipe("recipe_dusk_gale", "dusk_gale_blade", RecipeVisibility.Silhouette, "해가 진 뒤에도 바람은 달린다.", "The wind still runs after sunset.", "twilight_blade", "gale_blade");
            AddRecipe("recipe_stormstone", "stormstone_blade", RecipeVisibility.Public, "폭풍이 바위를 깎는다.", "A storm carves stone.", "stone_blade", "thunder_blade");
            AddRecipe("recipe_sunshard", "sunshard_blade", RecipeVisibility.Silhouette, "햇빛이 철 조각마다 머문다.", "Sunlight rests on every iron shard.", "iron_shard_blade", "radiant_blade");
            AddRecipe("recipe_ember_echo", "ember_echo_blade", RecipeVisibility.Public, "불꽃의 박동을 들어라.", "Hear the pulse of the flame.", "ember_blade", "resonance_blade");

            AddRecipe("recipe_starfall", "starfall_blade", RecipeVisibility.Riddle, "하늘의 빛을 파편에 담아 떨어뜨려라.", "Cast the sky's light into falling shards.", "sunshard_blade", "shard_sky_blade");
            AddRecipe("recipe_aurora", "aurora_edge", RecipeVisibility.Riddle, "두 색의 추위가 북쪽에서 만난다.", "Two colors of cold meet in the north.", "frost_thunder_blade", "prismatic_blade");
            AddRecipe("recipe_verdant_tempest", "verdant_tempest", RecipeVisibility.Silhouette, "폭풍 속 숲은 더욱 깊어진다.", "The forest grows deeper in a storm.", "snowbloom_woodblade", "storm_flying_blade");
            AddRecipe("recipe_twilight_thunder", "twilight_thunder", RecipeVisibility.Silhouette, "황혼에 세 번 울리는 천둥.", "Thunder rings three times at dusk.", "dusk_gale_blade", "thunder_resonance");
            AddRecipe("recipe_crystal_winter", "crystal_winter", RecipeVisibility.Riddle, "겨울을 유리 안에 가두어라.", "Trap winter inside glass.", "white_mist_blade", "prismatic_blade");
            AddRecipe("recipe_solar_guard", "solar_guard", RecipeVisibility.Silhouette, "산과 태양이 수호를 맹세한다.", "Mountain and sun swear to guard.", "mountain_cleaver", "sunshard_blade");
            AddRecipe("recipe_lunar_echo", "lunar_echo", RecipeVisibility.Riddle, "달 없는 거울에 메아리를 새긴다.", "Engrave an echo on a moonless mirror.", "moonless_mirror", "thunder_resonance");
            AddRecipe("recipe_dragonbreath", "dragonbreath_blade", RecipeVisibility.Silhouette, "용암에 불꽃의 울림을 더한다.", "Add a flame's echo to magma.", "magma_greatsword", "ember_echo_blade");
            AddRecipe("recipe_terra_bloom", "terra_bloom", RecipeVisibility.Silhouette, "대지는 생명을 기억한다.", "The earth remembers life.", "dawnroot_blade", "stormstone_blade");
            AddRecipe("recipe_phantom_gale", "phantom_gale", RecipeVisibility.Riddle, "보이지 않는 바람이 거울을 지난다.", "An unseen wind passes through the mirror.", "soundless_gale", "moonless_mirror");
            AddRecipe("recipe_celestial_shard", "celestial_shard", RecipeVisibility.Silhouette, "하늘의 파편에 일곱 빛을 섞는다.", "Mix seven lights into a shard of sky.", "shard_sky_blade", "prismatic_blade");
            AddRecipe("recipe_storm_prism", "storm_prism", RecipeVisibility.Silhouette, "폭풍을 투명한 빛에 가둔다.", "Trap a storm in transparent light.", "storm_flying_blade", "prismatic_blade");
            AddRecipe("recipe_frozen_mountain", "frozen_mountain", RecipeVisibility.Riddle, "산맥 위로 영원한 겨울이 내린다.", "Eternal winter falls upon the mountain.", "mountain_cleaver", "white_mist_blade");
            AddRecipe("recipe_forest_chorus", "forest_chorus", RecipeVisibility.Silhouette, "살아 있는 모든 뿌리가 함께 노래한다.", "Every living root sings together.", "life_saint_blade", "dawnroot_blade");
            AddRecipe("recipe_sunset_mirage", "sunset_mirage", RecipeVisibility.Riddle, "노을과 안개 사이의 거짓 불꽃.", "A false flame between sunset and mist.", "azure_mist_blade", "dusk_gale_blade");
            AddRecipe("recipe_magnetic_anvil", "magnetic_anvil", RecipeVisibility.Silhouette, "철과 천둥을 돌의 중심에 묶는다.", "Bind iron and thunder to the heart of stone.", "stormstone_blade", "iron_shard_blade", "thunder_blade");

            AddRecipe("recipe_eclipse_twins", "eclipse_twins", RecipeVisibility.Hidden, "빛과 그림자가 같은 칼집을 나눈다.", "Light and shadow share one sheath.", "solar_guard", "lunar_echo");
            AddRecipe("recipe_genesis_forgeblade", "genesis_forgeblade", RecipeVisibility.Hidden, "산맥과 생명이 최초의 모루 앞에 선다.", "Mountain and life stand before the first anvil.", "frozen_mountain", "terra_bloom", "magnetic_anvil");
            AddRecipe("recipe_astral_chorus", "astral_chorus", RecipeVisibility.Hidden, "별이 떨어질 때 숲은 합창한다.", "The forest sings when stars fall.", "starfall_blade", "forest_chorus");
            AddRecipe("recipe_void_blossom", "void_blossom", RecipeVisibility.Hidden, "아무것도 없는 곳에서 꽃을 피운다.", "Bloom where nothing exists.", "twilight_thunder", "sunset_mirage", "terra_bloom");
            AddRecipe("recipe_heavenfall", "heavenfall_blade", RecipeVisibility.Hidden, "하늘의 파편과 폭풍의 빛을 하나로.", "Join a celestial shard with stormlight.", "celestial_shard", "storm_prism");
            AddRecipe("recipe_worldroot", "worldroot_blade", RecipeVisibility.Hidden, "숲의 노래가 얼어붙은 산을 깨운다.", "The forest's song wakes the frozen mountain.", "forest_chorus", "frozen_mountain");
            AddRecipe("recipe_aurora_sovereign", "aurora_sovereign", RecipeVisibility.Hidden, "극광과 환영이 겨울 왕관을 만든다.", "Aurora and phantom shape a winter crown.", "aurora_edge", "phantom_gale", "crystal_winter");
            AddRecipe("recipe_dimensional_edge", "dimensional_edge", RecipeVisibility.Hidden, "서로 다른 세 울림이 공간을 가른다.", "Three different echoes divide space.", "magnetic_anvil", "lunar_echo", "storm_prism");

            AddRecipe("recipe_infinite_orbit", "infinite_orbit", RecipeVisibility.Hidden, "세 전설이 별의 궤도를 완성한다.", "Three legends complete a stellar orbit.", "eclipse_twins", "astral_chorus", "heavenfall_blade");
            AddRecipe("recipe_first_anvil", "first_anvil", RecipeVisibility.Hidden, "태초의 대장검을 세계의 뿌리에 되돌린다.", "Return the genesis blade to the root of the world.", "genesis_forgeblade", "worldroot_blade", "dimensional_edge");
            AddRecipe("recipe_sword_constellation", "sword_constellation", RecipeVisibility.Hidden, "별과 공허와 차원이 하나의 형상을 이룬다.", "Star, void, and dimension form one figure.", "astral_chorus", "void_blossom", "dimensional_edge");
            AddRecipe("recipe_forge_dream", "forge_dream", RecipeVisibility.Hidden, "첫 모루가 별자리의 꿈을 기억한다.", "The first anvil remembers the constellation's dream.", "first_anvil", "sword_constellation");

            // Alternate discoveries bring the catalog to fifty recipes.
            AddRecipe("recipe_starfall_alt", "starfall_blade", RecipeVisibility.Riddle, "빛나는 유리와 천둥의 공명.", "Radiant glass and thunderous resonance.", "prismatic_blade", "thunder_resonance", "radiant_blade");
            AddRecipe("recipe_worldroot_alt", "worldroot_blade", RecipeVisibility.Hidden, "생명의 성검을 대지의 개화에 심는다.", "Plant the life blade in the earth's bloom.", "life_saint_blade", "terra_bloom", "forest_chorus");
        }

        private void BuildEnemies()
        {
            AddEnemy("slime_mint", "민트 슬라임", "Mint Slime", 55, 0.75f, 1, 8, false, false, 0x78D8A0);
            AddEnemy("slime_berry", "베리 슬라임", "Berry Slime", 70, 0.70f, 1, 9, false, false, 0xD77BAA);
            AddEnemy("paper_golem", "종이 골렘", "Paper Golem", 90, 0.62f, 1, 10, false, false, 0xE2D7B9);
            AddEnemy("clockwork_bug", "태엽벌레", "Clockwork Bug", 50, 1.05f, 1, 8, false, false, 0xC99858);
            AddEnemy("cloud_puff", "구름몽실이", "Cloud Puff", 80, 0.86f, 1, 9, false, false, 0xDDEBFA);
            AddEnemy("moss_turtle", "이끼거북", "Moss Turtle", 145, 0.45f, 2, 12, false, false, 0x709C65);
            AddEnemy("lantern_imp", "등불도깨비", "Lantern Imp", 88, 0.90f, 1, 10, false, false, 0xEAA35F);
            AddEnemy("crystal_moth", "수정나방", "Crystal Moth", 72, 1.10f, 1, 9, false, false, 0xA9D7E8);
            AddEnemy("acorn_knight", "도토리기사", "Acorn Knight", 120, 0.66f, 2, 12, false, false, 0xA87B4F);
            AddEnemy("bubble_wisp", "방울정령", "Bubble Wisp", 65, 1.18f, 1, 9, false, false, 0x83D4DE);
            AddEnemy("shadow_plush", "그림자인형", "Shadow Plush", 105, 0.82f, 1, 11, false, false, 0x76649B);
            AddEnemy("stone_sprout", "돌새싹", "Stone Sprout", 155, 0.52f, 2, 13, false, false, 0x879B6F);
            AddEnemy("star_mouse", "별쥐", "Star Mouse", 75, 1.22f, 1, 9, false, false, 0xE8CF71);
            AddEnemy("rune_box", "룬상자", "Rune Box", 180, 0.50f, 2, 15, false, false, 0x8C76AD);
            AddEnemy("wind_sock", "바람양말", "Wind Sock", 85, 1.15f, 1, 10, false, false, 0x74BFAE);

            AddEnemy("elite_iron_golem", "철갑 골렘", "Iron Golem", 700, 0.42f, 4, 55, true, false, 0x7A8490);
            AddEnemy("elite_storm_ram", "폭풍산양", "Storm Ram", 520, 0.86f, 4, 52, true, false, 0x7BC5D5);
            AddEnemy("elite_mirror_mage", "거울마도사", "Mirror Mage", 580, 0.64f, 4, 54, true, false, 0xBBA3D8);
            AddEnemy("elite_root_guard", "뿌리수호자", "Root Guardian", 820, 0.38f, 5, 60, true, false, 0x66885E);
            AddEnemy("elite_sun_beetle", "태양풍뎅이", "Sun Beetle", 640, 0.60f, 4, 58, true, false, 0xE1B44F);
            AddEnemy("elite_moon_owl", "달빛부엉이", "Moon Owl", 560, 0.88f, 4, 56, true, false, 0x7B75A4);

            AddEnemy("boss_forge_colossus", "용광로 거신", "Forge Colossus", 5200, 0.28f, 12, 240, true, true, 0xD06B43);
            AddEnemy("boss_glacier_whale", "빙하하늘고래", "Glacier Skywhale", 6500, 0.24f, 14, 280, true, true, 0x86C9E1);
            AddEnemy("boss_clockwork_dragon", "태엽용", "Clockwork Dragon", 7600, 0.32f, 16, 320, true, true, 0xB78955);
            AddEnemy("boss_worldroot_guardian", "세계수문지기", "Worldroot Guardian", 9000, 0.22f, 18, 360, true, true, 0x6EA46D);
            AddEnemy("boss_constellation_eater", "별자리포식자", "Constellation Eater", 12000, 0.27f, 20, 450, true, true, 0x72538F);
        }

        private void BuildStages()
        {
            AddStage(
                "stage_starlit_forge",
                "별빛 초원",
                "Starlit Meadow",
                "작은 균열에서 검별공방의 첫 수호가 시작됩니다.",
                "Begin the forge's first defense at a gentle rift.",
                new[] { "slime_mint", "slime_berry", "paper_golem", "cloud_puff", "acorn_knight" },
                new[] { "elite_iron_golem", "elite_sun_beetle" },
                "boss_forge_colossus",
                0x1A2D4C);
            AddStage(
                "stage_frozen_clock",
                "빙결 시계탑",
                "Frozen Clocktower",
                "빠른 태엽 생물과 얼음 거신이 시간을 뒤틀고 있습니다.",
                "Clockwork creatures and a skywhale twist frozen time.",
                new[] { "clockwork_bug", "crystal_moth", "bubble_wisp", "rune_box", "star_mouse" },
                new[] { "elite_storm_ram", "elite_mirror_mage" },
                "boss_glacier_whale",
                0x18324B);
            AddStage(
                "stage_worldroot_void",
                "세계수 공허",
                "Worldroot Void",
                "그림자와 뿌리가 뒤엉킨 가장 깊은 균열입니다.",
                "Face the deepest rift where shadow and roots intertwine.",
                new[] { "moss_turtle", "lantern_imp", "shadow_plush", "stone_sprout", "wind_sock" },
                new[] { "elite_root_guard", "elite_moon_owl" },
                "boss_worldroot_guardian",
                0x261C3F);
        }

        private void AddSword(
            string id,
            string nameKo,
            string nameEn,
            SwordRarity rarity,
            SwordElement element,
            AttackStyle attackStyle,
            float damage,
            float interval,
            int targetCount,
            int colorRgb)
        {
            _swords.Add(id, new SwordDefinition(
                id,
                nameKo,
                nameEn,
                rarity,
                element,
                attackStyle,
                damage,
                interval,
                targetCount,
                colorRgb));
        }

        private void AddRecipe(
            string id,
            string resultSwordId,
            RecipeVisibility visibility,
            string hintKo,
            string hintEn,
            params string[] ingredientSwordIds)
        {
            RecipeDefinition recipe = new RecipeDefinition(
                id,
                resultSwordId,
                ingredientSwordIds,
                visibility,
                hintKo,
                hintEn,
                ResolveHiddenCondition(
                    visibility,
                    resultSwordId));
            _recipes.Add(recipe);
            _recipeByIngredients.Add(RecipeKey(ingredientSwordIds), recipe);
        }

        private void AddEnemy(
            string id,
            string nameKo,
            string nameEn,
            float health,
            float speed,
            int coreDamage,
            int reward,
            bool elite,
            bool boss,
            int colorRgb)
        {
            _enemies.Add(id, new EnemyDefinition(
                id,
                nameKo,
                nameEn,
                health,
                speed,
                coreDamage,
                reward,
                elite,
                boss,
                colorRgb));
        }

        private HiddenRecipeCondition ResolveHiddenCondition(
            RecipeVisibility visibility,
            string resultSwordId)
        {
            if (visibility != RecipeVisibility.Hidden)
            {
                return HiddenRecipeCondition.None;
            }

            SwordDefinition result = GetSword(resultSwordId);
            return result != null &&
                   result.Rarity == SwordRarity.Mythic
                ? HiddenRecipeCondition.AnyIngredientMaximumRank
                : HiddenRecipeCondition.AllIngredientsTogether;
        }

        private void AddStage(
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
            _stages.Add(id, new StageDefinition(
                id,
                nameKo,
                nameEn,
                descriptionKo,
                descriptionEn,
                normalEnemyIds,
                eliteEnemyIds,
                bossEnemyId,
                backgroundRgb));
        }

        private static string RecipeKey(IEnumerable<string> ingredientSwordIds)
        {
            return string.Join("|", ingredientSwordIds.OrderBy(id => id, StringComparer.Ordinal));
        }
    }
}
