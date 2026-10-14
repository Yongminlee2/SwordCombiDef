# 한 판 구조 개편 1단계 구현 계획

> 설계: `2026-10-11-run-redesign-stage1.md`

**목표:** 매 웨이브 각인 3택1, 궁극기 직접 발동, 집중 공격 탭, 전투력 표시, 새 색·폰트.

**구조:** 각인 = 기존 `RuneDefinition` 확장(등급 `RuneRarity`, 꼬리표 `RuneTag`, 새 `RuneEffect` 값).
전투 효과(피해 보너스·화상·둔화·연쇄·집중·궁극기)는 Core `CombinationDefenseSession`,
경제 효과(판매·소환·연쇄·공명)는 `BattleController`가 `RunModifierState`를 읽어 적용.

## 전역 제약
- 검·레시피·적 데이터와 그림 유지, 세로 한 손, 하단 버튼 3개, 60마리 패배, 전체이용가.
- Core는 UnityEngine 참조 금지. 모든 새 규칙은 EditMode 테스트.

## 검토 초점 (테스트로 고정)
1. 뽑을 각인이 3장 미만/0장 — 남은 만큼만, 0장이면 화면 없이 진행.
2. 최종 웨이브(30)는 휴식이 없으므로 카드도 없음, 무한 모드는 매 웨이브.
3. 집중 대상이 죽거나 사라짐 — 표식 해제, 다음 공격은 평소 규칙.
4. 궁극기 100% 미만에서 버튼 탭 — 아무 일 없음(게이지 유지).
5. 화상·연쇄로 적이 죽음 — 골드·점수·처치 이벤트가 일반 처치와 동일하게 1번만.

## 작업

### 1. 각인 데이터와 뽑기 (Core)
- `RunContent.cs`: `RuneRarity {Common, Rare, Legendary}`, `RuneTag {Basic, Forge, Sell, Recipe, Element, Hand, Rule}`,
  `RuneDefinition`에 `Rarity`, `Tag` 추가(기존 24개는 Common/Basic). 새 `RuneEffect` 18종과 새 각인 18개.
- `RunContentCatalog.DrawRunes(SeededRandom, IReadOnlyCollection<string> owned, bool bossWave)`:
  등급 가중치 70/25/5, 보스 웨이브는 희귀 이상 1장 보장, 중복·보유 제외, 최대 3장.
- `RunModifierState`: 새 효과 플래그·스택(`ForgeStacks`, `DiscoveryCount`, `ChainDamageBonus` 등).
- 테스트: 뽑기 규칙, 기존 룬 테스트 갱신.

### 2. 전투 규칙 (Core)
- 세션: `IntermissionStarted` 이벤트, `UltimateReady`, `TryUseUltimate()`, 자동 발동 제거,
  `AddUltimateCharge(float)`, `SetFocus(BattleEnemyState, float)`, `FocusedEnemy`, `BoardPower()`.
- 피해 보너스(합성 스택·발견·연쇄·원소 공명·빈칸·집중), ★3 대상 +1, 화상/둔화(적 상태), 번개 연쇄.
- 테스트: 효과별 1개 이상 + 검토 초점 2~5.

### 3. 화면·경제 (Runtime)
- 휴식마다 각인 카드 화면(이름 함렛 + 효과, 등급색, 같은 꼬리표 보유 수), 보스 처치 룬 트리거 제거.
- 판매 2배/판매 할인/판매 충전, 레시피 충전, 연쇄 무한, 공명 확률, 궁극기 후 무료 소환 3회, 이른 별.
- 궁극기 원형 버튼(방사형 게이지), 적 탭 집중 표식, 전투력 숫자.
- PlayMode 테스트: 카드 화면 뜨고 적용, 궁극기 버튼, 적 탭 집중.

### 4. 디자인 기준
- 폰트 교체(프리텐다드 기본, 함렛 제목), Noto 제거, 라이선스 문서.
- `GameTheme` 7색, 금색은 숫자 전용, 제목류 함렛.

### 5. 밸런스·검증
- CoreSmoke 봇: 각인 선택·궁극기 사용, 체력 곡선 재조정(빠른 봇 55~70%).
- run_tests, 화면 캡처, APK 0.13.0 (17), 실기기 플레이.
