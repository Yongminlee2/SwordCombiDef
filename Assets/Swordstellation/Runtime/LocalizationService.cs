using System;
using System.Collections.Generic;
using Swordstellation.Core;

namespace Swordstellation.Runtime
{
    public sealed class LocalizationService
    {
        private readonly Dictionary<string, string> _ko = new Dictionary<string, string>
        {
            ["app.title"] = "검별공방",
            ["app.subtitle"] = "랜덤 디펜스",
            ["menu.start"] = "차원로 수호",
            ["menu.stage_select"] = "균열 선택",
            ["menu.standard"] = "일반 수호",
            ["menu.daily"] = "오늘의 균열",
            ["menu.endless"] = "무한 균열",
            ["menu.daily_desc"] = "매일 같은 조건으로 도전",
            ["menu.endless_desc"] = "한계까지 이어지는 방어",
            ["menu.codex"] = "검 도감",
            ["menu.upgrades"] = "대장간 성장",
            ["menu.shop"] = "별빛 상점",
            ["menu.settings"] = "설정",
            ["menu.statistics"] = "기록",
            ["menu.language"] = "English",
            ["menu.preview"] = "PREVIEW BUILD",
            ["menu.level"] = "대장장이",
            ["menu.back"] = "돌아가기",
            ["battle.wave"] = "웨이브",
            ["battle.core"] = "차원로",
            ["battle.ether"] = "단조력",
            ["battle.summon"] = "소환",
            ["battle.forge"] = "조합",
            ["battle.ultimate"] = "검우",
            ["battle.pause"] = "일시정지",
            ["battle.resume"] = "계속",
            ["battle.quit"] = "나가기",
            ["battle.victory"] = "차원로 수호 성공!",
            ["battle.defeat"] = "차원로가 무너졌습니다",
            ["battle.result"] = "결과",
            ["battle.score"] = "점수",
            ["battle.retry"] = "다시 도전",
            ["battle.home"] = "대장간으로",
            ["battle.revive"] = "이번 수호에서 1회 복구",
            ["battle.end_run"] = "이번 수호 종료",
            ["battle.board_full"] = "대장간 슬롯이 가득 찼습니다.",
            ["battle.not_enough_ether"] = "단조력이 부족합니다.",
            ["battle.select_swords"] = "검을 선택한 뒤 조합하세요.",
            ["battle.ultimate_ready"] = "검우가 준비되었습니다!",
            ["battle.ultimate_used"] = "모든 검이 별빛처럼 쏟아집니다!",
            ["battle.discovered"] = "새 조합 발견!",
            ["battle.rare_appearance"] = "희귀 외형 탄생!",
            ["battle.next_wave"] = "다음 균열을 준비합니다",
            ["battle.core_attacked"] = "차원로가 공격받았습니다!",
            ["battle.core_restored"] = "차원로가 복구되었습니다!",
            ["hero.skill_name"] = "천공 명령",
            ["hero.skill_used"] = "대장장이의 천공 단조 명령!",
            ["forge.need_two"] = "검 두 자루를 선택하세요.",
            ["forge.two_identical_required"] = "같은 검·같은 별 두 자루가 필요합니다.",
            ["forge.max_stars"] = "이미 최고 별 등급입니다.",
            ["forge.star_up"] = "별 등급 상승!",
            ["forge.select_recipe_items"] = "조합할 검을 2~3자루 선택하세요.",
            ["forge.unknown_recipe"] = "조합이 반응하지 않습니다.",
            ["forge.invalid_selection"] = "유효하지 않은 검이 선택되었습니다.",
            ["codex.title"] = "검 도감",
            ["codex.swords"] = "검 목록",
            ["codex.recipes"] = "조합식",
            ["codex.recipe_progress"] = "발견한 조합",
            ["codex.hidden_recipe"] = "전설의 불꽃은 아직 답하지 않습니다.",
            ["codex.progress"] = "수집 진행",
            ["codex.appearances"] = "희귀 외형",
            ["codex.locked"] = "미발견",
            ["codex.enemies"] = "적 도감",
            ["codex.enemy_progress"] = "조우한 적",
            ["codex.runes"] = "각인 도감",
            ["codex.rune_progress"] = "새겨 본 각인",
            ["codex.rune_hidden"] = "수호 중 골라 새기면 기록됩니다.",
            ["stats.title"] = "대장장이 기록",
            ["stats.runs"] = "도전",
            ["stats.victories"] = "승리",
            ["stats.best_score"] = "최고 점수",
            ["stats.highest_wave"] = "최고 웨이브",
            ["stats.damage"] = "가한 피해",
            ["stats.summons"] = "검 소환",
            ["stats.forges"] = "조합 성공",
            ["stats.hero_skill"] = "주인공 스킬",
            ["stats.ultimates"] = "궁극기 발동",
            ["stats.enemy_skills"] = "적 특수기",
            ["upgrade.title"] = "대장간 성장",
            ["upgrade.smithing"] = "단조",
            ["upgrade.sword_control"] = "비검술",
            ["upgrade.guardianship"] = "수호",
            ["upgrade.smithing_desc"] = "시작 골드·소환 비용·고등급 소환 확률을 개선합니다.",
            ["upgrade.sword_control_desc"] = "모든 검의 공격력과 공격 속도를 강화합니다.",
            ["upgrade.guardianship_desc"] = "10단계마다 보드가 1칸 확장되어 최대 15칸이 됩니다.",
            ["upgrade.cost"] = "비용",
            ["upgrade.level"] = "레벨",
            ["upgrade.not_enough"] = "철가루가 부족합니다.",
            ["upgrade.not_enough_materials"] = "성장 재료가 부족합니다.",
            ["upgrade.max_level"] = "최대 성장 단계입니다.",
            ["upgrade.success"] = "성장이 완료되었습니다.",
            ["shop.title"] = "별빛 상점",
            ["shop.blueprint"] = "검 설계도 소환",
            ["shop.blueprint_desc"] = "검이 아닌 설계도 조각을 획득합니다.",
            ["shop.pull"] = "10 별강철로 소환",
            ["shop.odds"] = "등급 확률  희귀 70% · 영웅 22% · 전설 7% · 신화 1%",
            ["shop.pull_success"] = "설계도 조각 획득!",
            ["shop.pull_duplicate"] = "보유 검 조각이 범용 조각으로 변환되었습니다.",
            ["shop.unlocked"] = "새로운 검 해금!",
            ["shop.not_enough_starsteel"] = "별강철이 부족합니다.",
            ["shop.growth_pack"] = "초보 대장장이 성장 묶음",
            ["shop.preview_only"] = "결제 연결 전 미리보기 상품",
            ["shop.rewarded"] = "보상 광고로 철가루 30개",
            ["shop.rewarded_done"] = "철가루 30개를 받았습니다.",
            ["shop.materials"] = "보유 성장 재료",
            ["shop.universal_unlock"] = "범용 30으로 검 해금",
            ["shop.universal_unlocked"] = "범용 조각으로 검 해금!",
            ["shop.not_enough_universal"] = "범용 설계도 조각이 부족합니다.",
            ["shop.all_unlocked"] = "모든 조합 검을 해금했습니다.",
            ["rune.choose"] = "각인을 하나 고르세요",
            ["rune.acquired"] = "각인 새김",
            ["loadout.title"] = "출전 준비",
            ["loadout.ultimate"] = "궁극기 선택",
            ["loadout.pool"] = "소환 검 목록",
            ["loadout.pool_edit"] = "소환 검 편집",
            ["loadout.pool_rule"] = "해금된 기본 검 중 4~6자루를 선택하세요.",
            ["loadout.start"] = "수호 시작",
            ["stage.locked"] = "이전 균열을 먼저 수호하세요",
            ["settings.title"] = "설정",
            ["settings.music"] = "음악",
            ["settings.sfx"] = "효과음",
            ["settings.reduced_motion"] = "화면 움직임 줄이기",
            ["settings.on"] = "켜짐",
            ["settings.off"] = "꺼짐",
            ["settings.tutorial"] = "도움말 다시 보기",
            ["tutorial.title"] = "대장장이 안내",
            ["tutorial.page1"] = "소환과 판매\n소환한 검은 알아서 싸우고 처치할 때마다 골드를 벌어 옵니다.\n필요 없는 검은 소환 버튼으로 끌어다 놓으면 팔립니다.",
            ["tutorial.page2"] = "강화와 조합\n같은 검·같은 등급끼리 끌어 겹치면 등급이 오릅니다.\n보드 위 안내에 \"눌러서 제작\"이 뜨면 눌러서 새 검을 만드세요.",
            ["tutorial.page3"] = "각인과 궁극기\n웨이브가 끝날 때마다 각인 카드 1장을 골라 이번 판의 빌드를 만듭니다.\n궁극기는 보드 아래 둥근 버튼을 눌러 원할 때 씁니다.",
            ["tutorial.page5"] = "별자리와 집중\n검을 끌어 다른 칸에 놓으면 자리를 바꿉니다. 같은 원소끼리 붙이면 별자리가 되어 강해집니다.\n외곽의 적을 누르면 모든 검이 그 적을 먼저 노립니다.",
            ["tutorial.page4"] = "발견과 성장\n15웨이브 동안 적이 60마리 쌓이기 전에 조합하세요.\n패배해도 발견한 조합과 성장은 남고, 레벨이 오르면 새 각인이 열립니다.",
            ["tutorial.next"] = "다음",
            ["tutorial.begin"] = "공방으로",
            ["common.locked"] = "잠김"
        };

        private readonly Dictionary<string, string> _en = new Dictionary<string, string>
        {
            ["app.title"] = "SWORDSTELLATION",
            ["app.subtitle"] = "RANDOM FORGE",
            ["menu.start"] = "DEFEND THE RIFT",
            ["menu.stage_select"] = "CHOOSE A RIFT",
            ["menu.standard"] = "STANDARD",
            ["menu.daily"] = "DAILY RIFT",
            ["menu.endless"] = "ENDLESS RIFT",
            ["menu.daily_desc"] = "A new shared challenge every day",
            ["menu.endless_desc"] = "Hold the rift as long as you can",
            ["menu.codex"] = "SWORD CODEX",
            ["menu.upgrades"] = "FORGE GROWTH",
            ["menu.shop"] = "STARLIGHT SHOP",
            ["menu.settings"] = "SETTINGS",
            ["menu.statistics"] = "RECORDS",
            ["menu.language"] = "한국어",
            ["menu.preview"] = "PREVIEW BUILD",
            ["menu.level"] = "SMITH",
            ["menu.back"] = "BACK",
            ["battle.wave"] = "WAVE",
            ["battle.core"] = "RIFT",
            ["battle.ether"] = "FORGE",
            ["battle.summon"] = "SUMMON",
            ["battle.forge"] = "FORGE",
            ["battle.ultimate"] = "BLADE RAIN",
            ["battle.pause"] = "PAUSE",
            ["battle.resume"] = "RESUME",
            ["battle.quit"] = "QUIT",
            ["battle.victory"] = "RIFT DEFENDED!",
            ["battle.defeat"] = "THE RIFT HAS FALLEN",
            ["battle.result"] = "RESULT",
            ["battle.score"] = "SCORE",
            ["battle.retry"] = "RETRY",
            ["battle.home"] = "RETURN TO FORGE",
            ["battle.revive"] = "RESTORE RIFT ONCE THIS RUN",
            ["battle.end_run"] = "END THIS RUN",
            ["battle.board_full"] = "The forge board is full.",
            ["battle.not_enough_ether"] = "Not enough forge energy.",
            ["battle.select_swords"] = "Select swords, then forge.",
            ["battle.ultimate_ready"] = "Blade Rain is ready!",
            ["battle.ultimate_used"] = "Every blade falls like starlight!",
            ["battle.discovered"] = "NEW RECIPE DISCOVERED!",
            ["battle.rare_appearance"] = "RARE APPEARANCE FORGED!",
            ["battle.next_wave"] = "PREPARE FOR THE NEXT RIFT",
            ["battle.core_attacked"] = "THE RIFT IS UNDER ATTACK!",
            ["battle.core_restored"] = "THE RIFT HAS BEEN RESTORED!",
            ["hero.skill_name"] = "FORGE COMMAND",
            ["hero.skill_used"] = "CELESTIAL FORGE COMMAND!",
            ["forge.need_two"] = "Select two swords.",
            ["forge.two_identical_required"] = "Two matching swords with equal stars are required.",
            ["forge.max_stars"] = "This sword is already at maximum stars.",
            ["forge.star_up"] = "STAR LEVEL UP!",
            ["forge.select_recipe_items"] = "Select 2–3 swords for a recipe.",
            ["forge.unknown_recipe"] = "The forge does not react.",
            ["forge.invalid_selection"] = "The selected sword is invalid.",
            ["codex.title"] = "SWORD CODEX",
            ["codex.swords"] = "SWORDS",
            ["codex.recipes"] = "RECIPES",
            ["codex.recipe_progress"] = "DISCOVERED RECIPES",
            ["codex.hidden_recipe"] = "The legendary flame has not answered yet.",
            ["codex.progress"] = "COLLECTION",
            ["codex.appearances"] = "RARE APPEARANCES",
            ["codex.locked"] = "UNDISCOVERED",
            ["codex.enemies"] = "ENEMY CODEX",
            ["codex.enemy_progress"] = "ENEMIES ENCOUNTERED",
            ["codex.runes"] = "Engravings",
            ["codex.rune_progress"] = "Engravings used",
            ["codex.rune_hidden"] = "Pick it during a run to record it.",
            ["stats.title"] = "SMITH RECORDS",
            ["stats.runs"] = "RUNS",
            ["stats.victories"] = "VICTORIES",
            ["stats.best_score"] = "BEST SCORE",
            ["stats.highest_wave"] = "HIGHEST WAVE",
            ["stats.damage"] = "DAMAGE DEALT",
            ["stats.summons"] = "SWORDS SUMMONED",
            ["stats.forges"] = "SUCCESSFUL FORGES",
            ["stats.hero_skill"] = "HERO SKILLS",
            ["stats.ultimates"] = "ULTIMATES",
            ["stats.enemy_skills"] = "ENEMY ABILITIES",
            ["upgrade.title"] = "FORGE GROWTH",
            ["upgrade.smithing"] = "SMITHING",
            ["upgrade.sword_control"] = "SWORD CONTROL",
            ["upgrade.guardianship"] = "GUARDIANSHIP",
            ["upgrade.smithing_desc"] = "Improves starting gold, summon cost, and advanced summon chance.",
            ["upgrade.sword_control_desc"] = "Improves every sword's damage and attack speed.",
            ["upgrade.guardianship_desc"] = "Adds one board slot every 10 levels, up to 15 slots.",
            ["upgrade.cost"] = "COST",
            ["upgrade.level"] = "LEVEL",
            ["upgrade.not_enough"] = "Not enough iron powder.",
            ["upgrade.not_enough_materials"] = "Not enough growth materials.",
            ["upgrade.max_level"] = "This branch is fully upgraded.",
            ["upgrade.success"] = "Upgrade complete.",
            ["shop.title"] = "STARLIGHT SHOP",
            ["shop.blueprint"] = "SWORD BLUEPRINT DRAW",
            ["shop.blueprint_desc"] = "Awards blueprint fragments, not a completed sword.",
            ["shop.pull"] = "DRAW FOR 10 STARSTEEL",
            ["shop.odds"] = "Rarity odds  Rare 70% · Heroic 22% · Legendary 7% · Mythic 1%",
            ["shop.pull_success"] = "BLUEPRINT FRAGMENTS ACQUIRED!",
            ["shop.pull_duplicate"] = "Owned sword fragments converted to universal fragments.",
            ["shop.unlocked"] = "NEW SWORD UNLOCKED!",
            ["shop.not_enough_starsteel"] = "Not enough Starsteel.",
            ["shop.growth_pack"] = "APPRENTICE GROWTH PACK",
            ["shop.preview_only"] = "Preview product — billing not connected",
            ["shop.rewarded"] = "WATCH A REWARDED AD FOR 30 IRON",
            ["shop.rewarded_done"] = "Received 30 Iron Powder.",
            ["shop.materials"] = "GROWTH MATERIALS",
            ["shop.universal_unlock"] = "UNLOCK SWORD FOR 30 UNIVERSAL",
            ["shop.universal_unlocked"] = "SWORD UNLOCKED WITH UNIVERSAL FRAGMENTS!",
            ["shop.not_enough_universal"] = "Not enough universal blueprint fragments.",
            ["shop.all_unlocked"] = "All forged swords are unlocked.",
            ["rune.choose"] = "Choose an engraving",
            ["rune.acquired"] = "Engraved",
            ["loadout.title"] = "BATTLE LOADOUT",
            ["loadout.ultimate"] = "CHOOSE AN ULTIMATE",
            ["loadout.pool"] = "SUMMON POOL",
            ["loadout.pool_edit"] = "EDIT SUMMON POOL",
            ["loadout.pool_rule"] = "Choose 4–6 unlocked base swords.",
            ["loadout.start"] = "DEFEND THE RIFT",
            ["stage.locked"] = "Defend the previous rift first",
            ["settings.title"] = "SETTINGS",
            ["settings.music"] = "MUSIC",
            ["settings.sfx"] = "SFX",
            ["settings.reduced_motion"] = "REDUCE MOTION",
            ["settings.on"] = "ON",
            ["settings.off"] = "OFF",
            ["settings.tutorial"] = "VIEW TUTORIAL",
            ["tutorial.title"] = "SMITH'S GUIDE",
            ["tutorial.page1"] = "SUMMON & SELL\nSummoned swords fight on their own and every kill earns gold.\nDrag a sword you no longer need onto Summon to sell it.",
            ["tutorial.page2"] = "RANK UP & FORGE\nDrop a sword on its equal-rank twin to rank it up.\nWhen the guide above the board says TAP TO FORGE, tap it.",
            ["tutorial.page3"] = "ENGRAVINGS & ULTIMATE\nAfter every wave, pick one of three engravings to shape this run's build.\nTap the round button under the board to fire your ultimate when you choose.",
            ["tutorial.page5"] = "CONSTELLATIONS & FOCUS\nDrag a sword onto another slot to move or swap it. Same-element neighbours form a stronger constellation.\nTap an enemy on the loop and every sword hits it first.",
            ["tutorial.page4"] = "DISCOVER & GROW\nCombine through 15 waves before enemies reach 60.\nDiscoveries and growth stay after defeat, and new levels unlock engravings.",
            ["tutorial.next"] = "NEXT",
            ["tutorial.begin"] = "ENTER THE FORGE",
            ["common.locked"] = "Locked"
        };

        public string Locale { get; private set; }
        public bool IsKorean => Locale == "ko";

        public event Action LocaleChanged;

        public LocalizationService(string locale)
        {
            Locale = locale == "en" ? "en" : "ko";
        }

        public string Get(string key)
        {
            Dictionary<string, string> table = IsKorean ? _ko : _en;
            return table.TryGetValue(key, out string value) ? value : key;
        }

        public string RarityName(SwordRarity rarity)
        {
            return rarity switch
            {
                SwordRarity.Mythic => IsKorean ? "신화" : "Mythic",
                SwordRarity.Legendary => IsKorean ? "전설" : "Legendary",
                SwordRarity.Heroic => IsKorean ? "영웅" : "Heroic",
                SwordRarity.Rare => IsKorean ? "희귀" : "Rare",
                _ => IsKorean ? "일반" : "Common"
            };
        }

        public string SwordName(SwordDefinition definition)
        {
            return IsKorean ? definition.NameKo : definition.NameEn;
        }

        public string EnemyName(EnemyDefinition definition)
        {
            return IsKorean ? definition.NameKo : definition.NameEn;
        }

        public string RecipeHint(RecipeDefinition definition)
        {
            return IsKorean ? definition.HintKo : definition.HintEn;
        }

        public string StageName(StageDefinition definition)
        {
            return IsKorean ? definition.NameKo : definition.NameEn;
        }

        public string StageDescription(StageDefinition definition)
        {
            return IsKorean ? definition.DescriptionKo : definition.DescriptionEn;
        }

        public void Toggle()
        {
            Locale = IsKorean ? "en" : "ko";
            LocaleChanged?.Invoke();
        }
    }
}
