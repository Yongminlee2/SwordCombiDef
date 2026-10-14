# Google Play Release Plan

## App identity

- Korean title: **검별공방: 랜덤 디펜스**
- English title: **Swordstellation: Random Forge**
- Developer display: **YMLEE**
- In-game studio credit: **YMLEE Games**
- Application ID: `com.ymlee.swordstellation`
- Initial version: `0.1.0` (`versionCode 1`)
- Orientation: portrait
- Android ABI: ARM64
- Minimum SDK: API 25
- Target SDK: API 36

## Draft store listing — Korean

### Short description

날아다니는 검을 소환하고 비밀 조합을 발견하는 5분 랜덤 디펜스

### Full description

별빛 대장간의 검들이 차원로를 지키기 위해 날아오릅니다.

검을 무작위로 소환하고, 같은 검 두 자루를 합쳐 별 등급을 올리세요.
서로 다른 검을 조합하면 완전히 새로운 외형과 공격 방식을 가진 검이
탄생합니다. 공개된 조합식부터 힌트만 남은 비밀 조합, 완전히 숨겨진
전설 조합까지 직접 발견해 도감을 완성할 수 있습니다.

- 한 손으로 즐기는 세로형 자동 전투
- 60종의 검과 50개의 조합식
- 3개 스테이지, 15종 일반 적, 6종 정예, 5종 보스
- 일반 수호, 오늘의 균열, 무한 균열
- 대장장이·비검술·수호 능력의 영구 성장
- 한국어와 영어 지원

선택형 보상 광고가 포함될 수 있으며, 광고 시청은 강제되지 않습니다.
무작위 유료 상품은 완성된 검이 아니라 설계도 조각을 지급하며, 확률은
구매 화면에서 확인할 수 있습니다.

## Draft store listing — English

### Short description

Summon flying swords and discover secret recipes in a five-minute random defense.

### Full description

The blades of a celestial forge take flight to defend the dimensional rift.

Summon random swords and merge two matching blades to raise their star level.
Combine different swords to forge entirely new appearances and attack styles.
Complete your codex by discovering public recipes, cryptic hints, and hidden
legendary combinations.

- One-handed portrait auto-combat
- 60 swords and 50 forging recipes
- 3 stages, 15 normal enemies, 6 elites, and 5 bosses
- Standard, Daily Rift, and Endless modes
- Permanent Smithing, Sword Control, and Guardianship growth
- Korean and English

Optional rewarded advertising may be included and is never required. Paid randomized
items award blueprint fragments rather than completed swords, with odds displayed on
the purchase screen.

## Content-rating target

The creative direction avoids blood, realistic injury, profanity, gambling simulation,
sexual content, horror, and user-generated communication. Combat uses stylized flying
swords against fantasy creatures with flashes and disappearance effects.

The final IARC questionnaire must reflect the actual shipping advertising, purchase,
and data-collection SDK configuration. Do not guess answers before the final bundle is
scanned.

## Monetization disclosures

- Rewarded video only at launch; no forced interstitials.
- Rewards are granted only after a completed-ad callback.
- Gacha reward: sword-specific blueprint fragments.
- Displayed odds: Rare 70%, Heroic 22%, Legendary 7%, Mythic 1%.
- Odds disclosure appears adjacent to the draw action.
- Growth pack is a fixed-content product, not random.
- Real-money purchase buttons remain disabled until Play Billing is configured and
  tested with license testers.

## Closed-test path

For a newly created personal developer account, plan for the Google Play closed-testing
requirement before production access:

1. Upload the signed AAB to an internal track for installation sanity checks.
2. Create a closed-testing track and enroll at least 12 opted-in testers.
3. Keep at least 12 testers opted in continuously for 14 days.
4. Collect crash, ANR, retention, device, and balance feedback.
5. Apply for production access and answer the testing questionnaire with real evidence.
6. Promote only the exact release candidate that passed final policy and device checks.

## Store assets

- Ready: 1024×500 feature graphic at `StoreAssets/FeatureGraphic.png`
- Ready: 512×512 Play icon at `StoreAssets/PlayIcon512.png`
- Source: 1024×1024 icon master at `Assets/Art/Generated/AppIcon.png`

Still export after the verified device build:

- At least four phone screenshots from the Korean and English runtime
- Optional portrait gameplay video

Store screenshots must be captured from the final device build so they match the
shipping UI and do not misrepresent unavailable purchases or online features.
