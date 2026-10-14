using System;
using System.Collections.Generic;
using System.Linq;

namespace Swordstellation.Core
{
    public enum CombinationDefenseState
    {
        Preparing,
        Active,
        Intermission,
        Victory,
        Defeat,
        Abandoned
    }

    public readonly struct BattlePoint
    {
        public readonly float X;
        public readonly float Y;

        public BattlePoint(float x, float y)
        {
            X = x;
            Y = y;
        }

        public float DistanceSquared(BattlePoint other)
        {
            float x = X - other.X;
            float y = Y - other.Y;
            return x * x + y * y;
        }
    }

    public sealed class BattleEnemyState
    {
        public string RuntimeId { get; }
        public EnemyDefinition Definition { get; }
        public int Wave { get; }
        public float MaxHealth { get; }
        public float Health { get; private set; }
        public float Shield { get; private set; }
        public float Progress { get; private set; }
        public float ProgressSpeed { get; }
        public EnemyAbilityDefinition Ability { get; }
        public float AbilityTimeRemaining { get; private set; }
        public bool IsAlive => Health > 0f;
        public float Age { get; private set; }
        public bool Rampaging { get; private set; }

        public void Rampage()
        {
            if (IsAlive && !Rampaging)
            {
                Rampaging = true;
                BoostSpeed(0.35f, float.MaxValue);
            }
        }
        public bool AbilityReady =>
            Ability != null &&
            AbilityTimeRemaining <= 0f;

        private float _speedBoostRemaining;
        private float _speedBoostMultiplier = 1f;
        private float _burnDamagePerSecond;
        private float _burnRemaining;
        private float _slowMultiplier = 1f;
        private float _slowRemaining;

        public BattleEnemyState(
            string runtimeId,
            EnemyDefinition definition,
            int wave,
            float healthMultiplier,
            float speedMultiplier,
            float initialProgress)
        {
            RuntimeId = runtimeId ??
                throw new ArgumentNullException(nameof(runtimeId));
            Definition = definition ??
                throw new ArgumentNullException(nameof(definition));
            Wave = Math.Max(1, wave);
            MaxHealth = Math.Max(
                1f,
                definition.BaseHealth *
                Math.Max(0.1f, healthMultiplier));
            Health = MaxHealth;
            Progress = Repeat01(initialProgress);
            ProgressSpeed =
                Math.Max(
                    0.004f,
                    definition.MoveSpeed *
                    Math.Max(0.1f, speedMultiplier) *
                    0.025f);
            Ability = EnemyAbilityCatalog.Get(definition.Id);
            AbilityTimeRemaining =
                Ability?.InitialDelay * 1.35f ??
                float.MaxValue;
        }

        public float TakeDamage(float amount)
        {
            if (!IsAlive || amount <= 0f)
            {
                return 0f;
            }

            float remaining = amount;
            float applied = 0f;
            if (Shield > 0f)
            {
                float absorbed =
                    Math.Min(Shield, remaining);
                Shield -= absorbed;
                remaining -= absorbed;
                applied += absorbed;
            }

            float healthDamage =
                Math.Min(Health, remaining);
            Health -= healthDamage;
            applied += healthDamage;
            return applied;
        }

        public float RestoreHealth(float amount)
        {
            if (!IsAlive || amount <= 0f)
            {
                return 0f;
            }

            float restored =
                Math.Min(MaxHealth - Health, amount);
            Health += restored;
            return restored;
        }

        public float AddShield(float amount)
        {
            if (!IsAlive || amount <= 0f)
            {
                return 0f;
            }

            float before = Shield;
            Shield = Math.Min(
                MaxHealth * 0.5f,
                Shield + amount);
            return Shield - before;
        }

        public void ApplyBurn(float damagePerSecond, float seconds)
        {
            if (!IsAlive || damagePerSecond <= 0f || seconds <= 0f)
            {
                return;
            }

            _burnDamagePerSecond = Math.Max(
                _burnDamagePerSecond,
                damagePerSecond);
            _burnRemaining = Math.Max(_burnRemaining, seconds);
        }

        // Returns the damage the burn applied this tick.
        public float TickBurn(float deltaTime)
        {
            if (!IsAlive || _burnRemaining <= 0f || deltaTime <= 0f)
            {
                return 0f;
            }

            float time = Math.Min(deltaTime, _burnRemaining);
            _burnRemaining -= time;
            float applied = TakeDamage(_burnDamagePerSecond * time);
            if (_burnRemaining <= 0f)
            {
                _burnDamagePerSecond = 0f;
            }
            return applied;
        }

        public void ApplySlow(float speedMultiplier, float seconds)
        {
            if (!IsAlive || seconds <= 0f)
            {
                return;
            }

            _slowMultiplier = Math.Min(
                _slowMultiplier,
                Math.Max(0.1f, speedMultiplier));
            _slowRemaining = Math.Max(_slowRemaining, seconds);
        }

        public void Blink(float progressAmount)
        {
            if (IsAlive)
            {
                Progress = Repeat01(
                    Progress + progressAmount);
            }
        }

        public void BoostSpeed(
            float additionalMultiplier,
            float duration)
        {
            if (!IsAlive)
            {
                return;
            }

            _speedBoostMultiplier = Math.Max(
                _speedBoostMultiplier,
                1f + Math.Max(0f, additionalMultiplier));
            _speedBoostRemaining = Math.Max(
                _speedBoostRemaining,
                Math.Max(0f, duration));
        }

        public void TickAbility(float deltaTime)
        {
            if (!IsAlive || deltaTime <= 0f)
            {
                return;
            }

            Age += deltaTime;

            if (Ability != null)
            {
                AbilityTimeRemaining -= deltaTime;
            }
            if (_speedBoostRemaining > 0f)
            {
                _speedBoostRemaining -= deltaTime;
                if (_speedBoostRemaining <= 0f)
                {
                    _speedBoostMultiplier = 1f;
                }
            }
            if (_slowRemaining > 0f)
            {
                _slowRemaining -= deltaTime;
                if (_slowRemaining <= 0f)
                {
                    _slowMultiplier = 1f;
                }
            }
        }

        public void ResetAbilityTimer()
        {
            AbilityTimeRemaining =
                Ability?.Cooldown * 1.45f ??
                float.MaxValue;
        }

        public void Advance(
            float deltaTime,
            float speedMultiplier = 1f)
        {
            if (!IsAlive || deltaTime <= 0f)
            {
                return;
            }

            Progress = Repeat01(
                Progress +
                ProgressSpeed *
                deltaTime *
                Math.Max(0f, speedMultiplier) *
                _speedBoostMultiplier *
                (_slowRemaining > 0f ? _slowMultiplier : 1f));
        }

        private static float Repeat01(float value)
        {
            return value - (float)Math.Floor(value);
        }
    }

    public sealed class BattleAttackEvent
    {
        public int SlotIndex { get; }
        public SwordDefinition Definition { get; }
        public SwordInstance Instance { get; }
        public IReadOnlyList<BattleEnemyState> Targets { get; }
        public float DamagePerTarget { get; }
        public bool IsCritical { get; }

        public BattleAttackEvent(
            int slotIndex,
            SwordDefinition definition,
            SwordInstance instance,
            IReadOnlyList<BattleEnemyState> targets,
            float damagePerTarget,
            bool isCritical = false)
        {
            SlotIndex = slotIndex;
            Definition = definition;
            Instance = instance;
            Targets = targets;
            DamagePerTarget = damagePerTarget;
            IsCritical = isCritical;
        }
    }

    public sealed class BattleUltimateEvent
    {
        public UltimateDefinition Definition { get; }
        public IReadOnlyList<BattleEnemyState> Targets { get; }
        public float Damage { get; }

        public BattleUltimateEvent(
            UltimateDefinition definition,
            IReadOnlyList<BattleEnemyState> targets,
            float damage)
        {
            Definition = definition;
            Targets = targets;
            Damage = damage;
        }
    }

    public sealed class BattleEnemyAbilityEvent
    {
        public BattleEnemyState Caster { get; }
        public EnemyAbilityDefinition Definition { get; }
        public IReadOnlyList<BattleEnemyState> AffectedEnemies { get; }
        public int GoldDrained { get; }
        public float UltimateChargeDrained { get; }

        public BattleEnemyAbilityEvent(
            BattleEnemyState caster,
            EnemyAbilityDefinition definition,
            IReadOnlyList<BattleEnemyState> affectedEnemies,
            int goldDrained,
            float ultimateChargeDrained)
        {
            Caster = caster;
            Definition = definition;
            AffectedEnemies =
                affectedEnemies ??
                Array.Empty<BattleEnemyState>();
            GoldDrained = Math.Max(0, goldDrained);
            UltimateChargeDrained =
                Math.Max(0f, ultimateChargeDrained);
        }
    }

    public sealed class CombinationDefenseSession
    {
        public const int DefaultEnemyLimit = 60;
        public const float InitialPreparationSeconds = 2.0f;
        public const float IntermissionSeconds = 1.5f;

        private readonly ContentCatalog _catalog;
        private readonly StageDefinition _stage;
        private readonly RunMode _mode;
        private readonly RunInventory _inventory;
        private readonly SeededRandom _random;
        private readonly RunModifierState _modifiers;
        private readonly int _swordControlLevel;
        private readonly int _enemyLimit;
        private readonly UltimateDefinition _ultimate;
        private readonly IReadOnlyDictionary<string, int>
            _swordMasteryLevels;
        // A learning-path step limits waves and systems; null is a full run.
        private readonly LearningStep _step;
        private readonly List<BattleEnemyState> _enemies =
            new List<BattleEnemyState>(DefaultEnemyLimit);
        private readonly List<string> _selectedRuneIds =
            new List<string>();

        private WavePlan _wavePlan;
        private float _stateTimer;
        private float _spawnTimer;
        private int _normalSpawnsRemaining;
        private bool _eliteSpawnPending;
        private bool _bossSpawnPending;
        private bool _waveFullySpawned;
        private int _nextEnemyId;
        private float _ultimateCharge;
        private float _enemySlowRemaining;
        private BattleEnemyState _focus;
        private float _focusRemaining;
        private int _clearAwardedWave;

        public event Action<BattleEnemyState> EnemySpawned;
        public event Action<BattleAttackEvent> SwordAttacked;
        public event Action<BattleEnemyState, int> EnemyDefeated;
        public event Action<int> WaveStarted;
        public event Action<CombinationDefenseState> StateChanged;
        public event Action<BattleUltimateEvent> UltimateUsed;
        public event Action<BattleEnemyAbilityEvent>
            EnemyAbilityUsed;

        public CombinationDefenseState State { get; private set; }
        public IReadOnlyList<BattleEnemyState> Enemies => _enemies;
        public int AliveEnemyCount => _enemies.Count;
        public int Wave { get; private set; }
        public int TotalWaves { get; }
        public int Gold { get; private set; }
        public int Score { get; private set; }
        public int DefeatedEnemies { get; private set; }
        public int DefeatedElites { get; private set; }
        public int DefeatedBosses { get; private set; }
        public float DamageDealt { get; private set; }
        public float DurationSeconds { get; private set; }
        public float StateTimeRemaining => Math.Max(0f, _stateTimer);
        public float UltimateCharge => _ultimateCharge;
        public UltimateDefinition Ultimate => _ultimate;
        public int UltimatesUsed { get; private set; }
        public int EnemyAbilitiesTriggered { get; private set; }
        public IReadOnlyList<string> SelectedRuneIds =>
            _selectedRuneIds;
        public int SummonDiscount =>
            _modifiers.SummonDiscount;
        public bool IsEnded =>
            State == CombinationDefenseState.Victory ||
            State == CombinationDefenseState.Defeat ||
            State == CombinationDefenseState.Abandoned;

        public CombinationDefenseSession(
            ContentCatalog catalog,
            StageDefinition stage,
            RunMode mode,
            RunInventory inventory,
            ulong seed,
            int swordControlLevel,
            int startingGold,
            RunModifierState modifiers = null,
            int enemyLimit = DefaultEnemyLimit,
            string ultimateId = "blade_rain",
            IReadOnlyDictionary<string, int>
                swordMasteryLevels = null,
            LearningStep learningStep = null)
        {
            _catalog = catalog ??
                throw new ArgumentNullException(nameof(catalog));
            _stage = stage ??
                throw new ArgumentNullException(nameof(stage));
            _inventory = inventory ??
                throw new ArgumentNullException(nameof(inventory));
            _mode = mode;
            _random = new SeededRandom(seed);
            _swordControlLevel =
                Math.Max(0, swordControlLevel);
            _modifiers = modifiers ?? new RunModifierState();
            _enemyLimit = Math.Max(1, enemyLimit);
            _ultimate =
                RunContentCatalog.GetUltimate(ultimateId) ??
                RunContentCatalog.GetUltimate("blade_rain");
            _swordMasteryLevels =
                swordMasteryLevels ??
                new Dictionary<string, int>();
            Gold = Math.Max(0, startingGold);
            _step = learningStep;
            TotalWaves = learningStep?.Waves ?? RunBalanceRules.TotalWaves(mode);
            State = CombinationDefenseState.Preparing;
            _stateTimer = InitialPreparationSeconds;
        }

        public LearningStep LearningStep => _step;
        public int PeakPressure { get; private set; }

        public bool HasFeature(GameFeature feature)
        {
            return _step == null || _step.Has(feature);
        }

        public bool TrySpendGold(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }
            if (Gold < amount || IsEnded)
            {
                return false;
            }

            Gold -= amount;
            return true;
        }

        public event Action<int> IntermissionStarted;
        public event Action<BattleEnemyState> BossRampaged;
        public event Action<int, int> WaveCleared;
        // Enemies toward the 60 limit; a rampaging boss weighs as ten.
        public int PressureCount =>
            _enemies.Sum(enemy =>
                enemy.Rampaging ? RunBalanceRules.RampagePressure : 1);

        public float BossTimeRemaining(BattleEnemyState boss)
        {
            return boss == null || !boss.Definition.IsBoss
                ? 0f
                : Math.Max(0f, RunBalanceRules.BossTimeLimit - boss.Age);
        }

        public RunModifierState Modifiers => _modifiers;
        public bool UltimateReady =>
            !IsEnded && _ultimateCharge >= 100f;
        public BattleEnemyState FocusedEnemy =>
            _focus != null &&
            _focus.IsAlive &&
            _focusRemaining > 0f &&
            _enemies.Contains(_focus)
                ? _focus
                : null;

        public void AddUltimateCharge(float percent)
        {
            _ultimateCharge = Math.Max(
                0f,
                Math.Min(100f, _ultimateCharge + percent));
        }

        // Swords in range hit the marked enemy first for a few seconds.
        public void SetFocus(
            BattleEnemyState enemy,
            float seconds = 5f)
        {
            if (enemy == null ||
                !enemy.IsAlive ||
                !_enemies.Contains(enemy))
            {
                return;
            }

            _focus = enemy;
            _focusRemaining = Math.Max(0f, seconds);
        }

        public void RegisterForge()
        {
            _modifiers.RegisterForge();
        }

        // Board damage per second with every current bonus, for the HUD.
        public float BoardPower()
        {
            float total = 0f;
            for (int slotIndex = 0;
                 slotIndex < _inventory.Slots.Count;
                 slotIndex++)
            {
                SwordInstance instance = _inventory.Slots[slotIndex];
                SwordDefinition definition = instance == null
                    ? null
                    : _catalog.GetSword(instance.DefinitionId);
                if (definition == null)
                {
                    continue;
                }

                float damage =
                    BaseSwordDamage(definition, instance, false, false) *
                    EngravingDamageMultiplier(definition, slotIndex);
                int targets = Math.Max(
                    1,
                    definition.TargetCount +
                    _modifiers.ExtraTargets +
                    StarredExtraTargets(instance));
                total += damage * targets /
                         CombatMath.AttackCooldown(
                             definition,
                             _swordControlLevel,
                             _modifiers);
            }

            return total;
        }

        public bool TryUseUltimate()
        {
            if (_ultimate == null ||
                !UltimateReady ||
                _enemies.Count == 0)
            {
                return false;
            }

            FireUltimate();
            return true;
        }

        public void AddGold(int amount)
        {
            if (amount > 0 && !IsEnded)
            {
                Gold += amount;
            }
        }

        public void Abandon()
        {
            if (!IsEnded)
            {
                End(CombinationDefenseState.Abandoned);
            }
        }

        public void ApplyRune(RuneDefinition rune)
        {
            if (rune == null)
            {
                throw new ArgumentNullException(nameof(rune));
            }
            if (_selectedRuneIds.Contains(rune.Id))
            {
                return;
            }

            _modifiers.Apply(rune);
            _selectedRuneIds.Add(rune.Id);

            float pressureBurst =
                _modifiers.ConsumePressureBurst();
            if (pressureBurst > 0f)
            {
                DamageCurrentEnemiesByMaxHealth(
                    pressureBurst);
            }
        }

        public void Advance(float deltaTime)
        {
            if (deltaTime <= 0f || IsEnded)
            {
                return;
            }

            float remaining = Math.Min(deltaTime, 2f);
            while (remaining > 0f && !IsEnded)
            {
                float step = Math.Min(0.05f, remaining);
                AdvanceStep(step);
                PeakPressure = Math.Max(PeakPressure, PressureCount);
                remaining -= step;
            }
        }

        public static BattlePoint BoardSlotPosition(
            int slotIndex,
            int slotCount = RunInventory.SlotCount)
        {
            if (slotIndex < 0 || slotIndex >= slotCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(slotIndex));
            }

            int columns = 3;
            int rows = (int)Math.Ceiling(slotCount / 3f);
            int column = slotIndex % columns;
            int row = slotIndex / columns;
            float x = 0.29f + column * 0.21f;
            float top = rows <= 4 ? 0.74f : 0.744f;
            float bottom = rows <= 4 ? 0.29f : 0.172f;
            float y = rows == 1
                ? 0.5f
                : top -
                  row * ((top - bottom) / (rows - 1));
            return new BattlePoint(x, y);
        }

        public static BattlePoint LoopPosition(float progress)
        {
            float repeated =
                progress - (float)Math.Floor(progress);
            const float left = 0.075f;
            const float right = 0.925f;
            const float bottom = 0.067f;
            const float top = 0.933f;
            float segment = repeated * 4f;

            if (segment < 1f)
            {
                return new BattlePoint(
                    Lerp(left, right, segment),
                    top);
            }
            if (segment < 2f)
            {
                return new BattlePoint(
                    right,
                    Lerp(top, bottom, segment - 1f));
            }
            if (segment < 3f)
            {
                return new BattlePoint(
                    Lerp(right, left, segment - 2f),
                    bottom);
            }

            return new BattlePoint(
                left,
                Lerp(bottom, top, segment - 3f));
        }

        public static float EffectiveAttackRange(
            int slotIndex,
            int slotCount,
            SwordDefinition definition,
            float rangeMultiplier = 1f)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(
                    nameof(definition));
            }

            BattlePoint source =
                BoardSlotPosition(
                    slotIndex,
                    slotCount);
            const float left = 0.075f;
            const float right = 0.925f;
            const float bottom = 0.067f;
            const float top = 0.933f;
            float nearestLoop =
                Math.Min(
                    Math.Min(
                        Math.Abs(source.X - left),
                        Math.Abs(right - source.X)),
                    Math.Min(
                        Math.Abs(source.Y - bottom),
                        Math.Abs(top - source.Y)));
            float baseRange = Math.Max(
                SwordFamilyRules.Range(definition),
                nearestLoop + 0.018f);
            return baseRange *
                   Math.Max(0.1f, rangeMultiplier);
        }

        private void AdvanceStep(float deltaTime)
        {
            DurationSeconds += deltaTime;

            if (State == CombinationDefenseState.Preparing)
            {
                _stateTimer -= deltaTime;
                if (_stateTimer <= 0f)
                {
                    StartWave(1);
                }
            }
            else if (State ==
                     CombinationDefenseState.Intermission)
            {
                _stateTimer -= deltaTime;
                if (_stateTimer <= 0f)
                {
                    StartWave(Wave + 1);
                }
            }
            else if (State == CombinationDefenseState.Active)
            {
                AdvanceSpawning(deltaTime);
            }

            if (_enemySlowRemaining > 0f)
            {
                _enemySlowRemaining -= deltaTime;
            }
            float moveMultiplier =
                _enemySlowRemaining > 0f
                    ? 0.38f
                    : 1f;
            ProcessEnemyAbilities(deltaTime);
            foreach (BattleEnemyState enemy in _enemies)
            {
                enemy.TickAbility(deltaTime);
                enemy.Advance(
                    deltaTime,
                    moveMultiplier);
                if (enemy.Definition.IsBoss &&
                    !enemy.Rampaging &&
                    enemy.Age >= RunBalanceRules.BossTimeLimit)
                {
                    enemy.Rampage();
                    BossRampaged?.Invoke(enemy);
                }
            }

            TickBurns(deltaTime);
            if (_focusRemaining > 0f)
            {
                _focusRemaining -= deltaTime;
            }
            if (FocusedEnemy == null)
            {
                _focus = null;
            }
            ProcessSwordAttacks(deltaTime);
            ChargeUltimate(deltaTime);

            if (_waveFullySpawned &&
                _enemies.Count == 0 &&
                _clearAwardedWave < Wave &&
                Wave < TotalWaves)
            {
                int bonus = 10 + Wave * 4;
                Gold += bonus;
                _clearAwardedWave = Wave;
                WaveCleared?.Invoke(Wave, bonus);
            }

            if (PressureCount >= _enemyLimit)
            {
                End(CombinationDefenseState.Defeat);
                return;
            }

            if (Wave >= TotalWaves &&
                _waveFullySpawned &&
                _enemies.Count == 0)
            {
                End(CombinationDefenseState.Victory);
            }
        }

        private void StartWave(int wave)
        {
            Wave = Math.Max(1, wave);
            _wavePlan =
                RunBalanceRules.CreateWavePlan(Wave);
            _normalSpawnsRemaining =
                _wavePlan.SpawnCount;
            _eliteSpawnPending = _wavePlan.HasElite;
            _bossSpawnPending = _wavePlan.HasBoss && HasFeature(GameFeature.Bosses);
            _waveFullySpawned = false;
            _spawnTimer = 0.15f;
            SetState(CombinationDefenseState.Active);
            WaveStarted?.Invoke(Wave);
        }

        private void AdvanceSpawning(float deltaTime)
        {
            if (_waveFullySpawned)
            {
                return;
            }

            _spawnTimer -= deltaTime;
            if (_spawnTimer > 0f)
            {
                return;
            }

            if (_normalSpawnsRemaining > 0)
            {
                SpawnNormal();
                _normalSpawnsRemaining--;
                _spawnTimer += _wavePlan.SpawnInterval;
                return;
            }

            if (_eliteSpawnPending)
            {
                SpawnElite();
                _eliteSpawnPending = false;
                _spawnTimer += _wavePlan.SpawnInterval;
                return;
            }

            if (_bossSpawnPending)
            {
                SpawnBoss();
                _bossSpawnPending = false;
                _spawnTimer += _wavePlan.SpawnInterval;
                return;
            }

            _waveFullySpawned = true;
            if (Wave < TotalWaves)
            {
                _stateTimer = IntermissionSeconds;
                SetState(
                    CombinationDefenseState.Intermission);
                IntermissionStarted?.Invoke(Wave);
            }
        }

        private void SpawnNormal()
        {
            string[] ids = _stage.NormalEnemyIds;
            Spawn(
                _catalog.GetEnemy(
                    ids[_random.NextInt(ids.Length)]),
                _wavePlan.NormalHealthMultiplier);
        }

        private void SpawnElite()
        {
            if (_stage.EliteEnemyIds == null ||
                _stage.EliteEnemyIds.Length == 0)
            {
                return;
            }

            string id = _stage.EliteEnemyIds[
                _random.NextInt(
                    _stage.EliteEnemyIds.Length)];
            Spawn(
                _catalog.GetEnemy(id),
                _wavePlan.EliteHealthMultiplier);
        }

        private void SpawnBoss()
        {
            Spawn(
                _catalog.GetEnemy(_stage.BossEnemyId),
                _wavePlan.BossHealthMultiplier);
        }

        private void Spawn(
            EnemyDefinition definition,
            float healthMultiplier)
        {
            if (definition == null)
            {
                return;
            }

            BattleEnemyState enemy =
                new BattleEnemyState(
                    $"enemy-{++_nextEnemyId}",
                    definition,
                    Wave,
                    healthMultiplier * (_step?.HealthScale ?? 1f),
                    _wavePlan.SpeedMultiplier,
                    _random.NextFloat());
            _enemies.Add(enemy);
            EnemySpawned?.Invoke(enemy);
        }

        private void ProcessSwordAttacks(float deltaTime)
        {
            if (_enemies.Count == 0)
            {
                return;
            }

            for (int slotIndex = 0;
                 slotIndex < _inventory.Slots.Count;
                 slotIndex++)
            {
                SwordInstance instance =
                    _inventory.Slots[slotIndex];
                if (instance == null)
                {
                    continue;
                }

                instance.Cooldown -= deltaTime;
                if (instance.Cooldown > 0f)
                {
                    continue;
                }

                SwordDefinition definition =
                    _catalog.GetSword(
                        instance.DefinitionId);
                if (definition == null)
                {
                    continue;
                }

                List<BattleEnemyState> targets =
                    FindTargets(slotIndex, definition, instance);
                if (targets.Count == 0)
                {
                    instance.Cooldown = 0.08f;
                    continue;
                }

                float damage =
                    BaseSwordDamage(
                        definition,
                        instance,
                        targets[0].Definition.IsElite,
                        targets[0].Definition.IsBoss) *
                    EngravingDamageMultiplier(definition, slotIndex);
                bool critical =
                    _random.NextFloat() <
                    RunBalanceRules.CriticalChance(instance.Stars);
                if (critical)
                {
                    damage *= RunBalanceRules.CriticalMultiplier;
                }
                List<BattleEnemyState> defeated =
                    new List<BattleEnemyState>();
                foreach (BattleEnemyState target in targets)
                {
                    float hit = damage;
                    if (_modifiers.Has(RuneEffect.FocusDamage) &&
                        target == FocusedEnemy)
                    {
                        hit *= 1.5f;
                    }

                    float applied =
                        target.TakeDamage(hit);
                    DamageDealt += applied;
                    if (!target.IsAlive)
                    {
                        defeated.Add(target);
                        continue;
                    }

                    if (_modifiers.Has(RuneEffect.Burn) &&
                        definition.Element == SwordElement.Flame)
                    {
                        target.ApplyBurn(hit * 0.20f, 3f);
                    }
                    if (_modifiers.Has(RuneEffect.FrostSlow) &&
                        definition.Element == SwordElement.Frost)
                    {
                        target.ApplySlow(0.70f, 2f);
                    }
                }

                if (_modifiers.Has(RuneEffect.ChainLightning) &&
                    definition.Element == SwordElement.Thunder)
                {
                    ArcFromKills(defeated, damage * 0.5f);
                }

                SwordAttacked?.Invoke(
                    new BattleAttackEvent(
                        slotIndex,
                        definition,
                        instance,
                        targets,
                        damage,
                        critical));

                foreach (BattleEnemyState target in defeated)
                {
                    DefeatEnemy(target);
                }

                instance.Cooldown =
                    CombatMath.AttackCooldown(
                        definition,
                        _swordControlLevel,
                        _modifiers);
            }
        }

        private void ChargeUltimate(float deltaTime)
        {
            if (_ultimate == null ||
                _enemies.Count == 0)
            {
                return;
            }

            _ultimateCharge = Math.Min(
                100f,
                _ultimateCharge +
                deltaTime *
                2.2f *
                Math.Max(0.1f, _modifiers.UltimateChargeMultiplier));
        }

        private void FireUltimate()
        {
            List<BattleEnemyState> targets =
                _enemies
                    .Where(enemy => enemy.IsAlive)
                    .ToList();
            float damage =
                _ultimate.BasePower *
                0.55f *
                _modifiers.UltimatePowerMultiplier;
            List<BattleEnemyState> defeated =
                new List<BattleEnemyState>();
            foreach (BattleEnemyState target in targets)
            {
                float applied =
                    target.TakeDamage(damage);
                DamageDealt += applied;
                if (!target.IsAlive)
                {
                    defeated.Add(target);
                }
            }

            if (_ultimate.Effect ==
                UltimateEffect.ChronoSeal)
            {
                _enemySlowRemaining = 7f;
            }
            else if (_ultimate.Effect ==
                     UltimateEffect.ForgeNova)
            {
                Gold += 75;
            }

            _ultimateCharge = 0f;
            UltimatesUsed++;
            UltimateUsed?.Invoke(
                new BattleUltimateEvent(
                    _ultimate,
                    targets,
                    damage));
            foreach (BattleEnemyState enemy in defeated)
            {
                DefeatEnemy(enemy);
            }
        }

        private void ProcessEnemyAbilities(
            float deltaTime)
        {
            if (_enemies.Count == 0 ||
                deltaTime <= 0f)
            {
                return;
            }

            BattleEnemyState[] casters =
                _enemies
                    .Where(enemy =>
                        enemy.IsAlive &&
                        enemy.AbilityReady)
                    .ToArray();
            foreach (BattleEnemyState caster in casters)
            {
                if (!_enemies.Contains(caster) ||
                    !caster.IsAlive)
                {
                    continue;
                }

                ActivateEnemyAbility(caster);
                caster.ResetAbilityTimer();
            }
        }

        private void ActivateEnemyAbility(
            BattleEnemyState caster)
        {
            EnemyAbilityDefinition ability =
                caster.Ability;
            if (ability == null)
            {
                return;
            }

            List<BattleEnemyState> affected =
                new List<BattleEnemyState>();
            int goldDrained = 0;
            float ultimateDrained = 0f;
            switch (ability.Type)
            {
                case EnemyAbilityType.ShieldPulse:
                case EnemyAbilityType.MirrorWard:
                    caster.AddShield(
                        caster.MaxHealth *
                        ability.Strength);
                    affected.Add(caster);
                    break;

                case EnemyAbilityType.StormCharge:
                case EnemyAbilityType.ClockworkRush:
                    caster.BoostSpeed(
                        ability.Strength,
                        ability.Duration);
                    affected.Add(caster);
                    break;

                case EnemyAbilityType.RootRegeneration:
                    caster.RestoreHealth(
                        (caster.MaxHealth -
                         caster.Health) *
                        ability.Strength);
                    affected.Add(caster);
                    break;

                case EnemyAbilityType.SolarDrain:
                    goldDrained = Math.Min(
                        Gold,
                        Math.Max(
                            0,
                            (int)Math.Round(
                                ability.Strength)));
                    Gold -= goldDrained;
                    caster.AddShield(
                        caster.MaxHealth * 0.10f);
                    affected.Add(caster);
                    break;

                case EnemyAbilityType.MoonBlink:
                    caster.Blink(
                        Math.Min(
                            0.16f,
                            ability.Strength * 0.18f));
                    affected.Add(caster);
                    break;

                case EnemyAbilityType.ForgeQuake:
                    caster.AddShield(
                        caster.MaxHealth *
                        ability.Strength);
                    DelaySwordAttacks(0.35f);
                    affected.Add(caster);
                    break;

                case EnemyAbilityType.GlacierStorm:
                    DelaySwordAttacks(
                        ability.Strength);
                    affected.AddRange(
                        _enemies.Where(enemy =>
                            enemy.IsAlive));
                    break;

                case EnemyAbilityType.WorldrootBloom:
                    foreach (BattleEnemyState enemy in
                             _enemies.Where(enemy =>
                                 enemy.IsAlive))
                    {
                        enemy.RestoreHealth(
                            enemy.MaxHealth *
                            ability.Strength);
                        affected.Add(enemy);
                    }
                    break;

                case EnemyAbilityType.ConstellationDevour:
                    goldDrained = Math.Min(
                        Gold,
                        Math.Max(
                            0,
                            (int)Math.Round(
                                ability.Strength)));
                    Gold -= goldDrained;
                    ultimateDrained =
                        Math.Min(
                            _ultimateCharge,
                            ability.Strength * 1.8f);
                    _ultimateCharge -= ultimateDrained;
                    affected.Add(caster);
                    break;
            }

            EnemyAbilitiesTriggered++;
            EnemyAbilityUsed?.Invoke(
                new BattleEnemyAbilityEvent(
                    caster,
                    ability,
                    affected,
                    goldDrained,
                    ultimateDrained));
        }

        private void DelaySwordAttacks(float seconds)
        {
            if (seconds <= 0f)
            {
                return;
            }

            foreach (SwordInstance sword in
                     _inventory.Slots)
            {
                if (sword != null)
                {
                    sword.Cooldown =
                        Math.Min(
                            5f,
                            Math.Max(0f, sword.Cooldown) +
                            seconds);
                }
            }
        }

        private void DamageCurrentEnemiesByMaxHealth(
            float fraction)
        {
            float safeFraction =
                Math.Max(0f, Math.Min(1f, fraction));
            if (safeFraction <= 0f)
            {
                return;
            }

            BattleEnemyState[] targets =
                _enemies
                    .Where(enemy => enemy.IsAlive)
                    .ToArray();
            List<BattleEnemyState> defeated =
                new List<BattleEnemyState>();
            foreach (BattleEnemyState enemy in targets)
            {
                float applied = enemy.TakeDamage(
                    enemy.MaxHealth *
                    safeFraction);
                DamageDealt += applied;
                if (!enemy.IsAlive)
                {
                    defeated.Add(enemy);
                }
            }

            foreach (BattleEnemyState enemy in defeated)
            {
                DefeatEnemy(enemy);
            }
        }

        private List<BattleEnemyState> FindTargets(
            int slotIndex,
            SwordDefinition definition,
            SwordInstance instance)
        {
            BattlePoint source =
                BoardSlotPosition(
                    slotIndex,
                    _inventory.Slots.Count);
            float range =
                EffectiveAttackRange(
                    slotIndex,
                    _inventory.Slots.Count,
                    definition,
                    _modifiers.RangeMultiplier);
            float rangeSquared = range * range;
            int targetCount = Math.Min(
                _enemies.Count,
                CombatMath.TargetCount(
                    definition,
                    _modifiers,
                    _enemies.Count) +
                StarredExtraTargets(instance));
            BattleEnemyState focus = FocusedEnemy;

            return _enemies
                .Where(enemy =>
                    enemy.IsAlive &&
                    source.DistanceSquared(
                        LoopPosition(enemy.Progress)) <=
                    rangeSquared)
                .OrderBy(enemy => enemy == focus ? 0 : 1)
                .ThenBy(enemy =>
                    source.DistanceSquared(
                        LoopPosition(enemy.Progress)))
                .ThenBy(enemy => enemy.Health)
                .Take(targetCount)
                .ToList();
        }

        private float BaseSwordDamage(
            SwordDefinition definition,
            SwordInstance instance,
            bool isElite,
            bool isBoss)
        {
            return CombatMath.SwordDamage(
                       definition,
                       instance,
                       _swordControlLevel,
                       _modifiers,
                       isElite,
                       isBoss,
                       _swordMasteryLevels.TryGetValue(
                           definition.Id,
                           out int masteryLevel)
                           ? masteryLevel
                           : 0) *
                   RunBalanceRules.CombinationPressureMultiplier(
                       instance.Stars,
                       definition.Rarity,
                       Math.Max(1, Wave));
        }

        private float EngravingDamageMultiplier(
            SwordDefinition definition,
            int slotIndex)
        {
            float bonus =
                _modifiers.ForgeDamageBonus +
                _modifiers.DiscoveryDamageBonus +
                _modifiers.ChainDamageBonus +
                (HasFeature(GameFeature.Constellations) ? 1f : 0f) *
                ConstellationRules.GroupBonus(
                    ConstellationRules.GroupSize(
                        _inventory,
                        _catalog,
                        slotIndex)) *
                (_modifiers.Has(RuneEffect.ConstellationMastery) ? 2f : 1f);
            if (_modifiers.Has(RuneEffect.EmptySlotPower))
            {
                bonus += _inventory.EmptyCount * 0.05f;
            }
            if (_modifiers.Has(RuneEffect.ElementResonance) &&
                _inventory.Slots.Count(sword =>
                    sword != null &&
                    _catalog.GetSword(sword.DefinitionId)?.Element ==
                    definition.Element) >= 3)
            {
                bonus += 0.30f;
            }

            return 1f + bonus;
        }

        private int StarredExtraTargets(SwordInstance instance)
        {
            return _modifiers.Has(RuneEffect.StarredExtraTarget) &&
                   instance != null &&
                   instance.Stars >= 3
                ? 1
                : 0;
        }

        private void TickBurns(float deltaTime)
        {
            for (int index = _enemies.Count - 1; index >= 0; index--)
            {
                if (index >= _enemies.Count)
                {
                    continue;
                }

                BattleEnemyState enemy = _enemies[index];
                DamageDealt += enemy.TickBurn(deltaTime);
                if (!enemy.IsAlive)
                {
                    DefeatEnemy(enemy);
                }
            }
        }

        // Each thunder kill arcs once to the nearest survivor; arcs do not chain.
        private void ArcFromKills(
            List<BattleEnemyState> kills,
            float damage)
        {
            foreach (BattleEnemyState killed in kills.ToArray())
            {
                BattlePoint origin = LoopPosition(killed.Progress);
                BattleEnemyState next = _enemies
                    .Where(enemy =>
                        enemy.IsAlive &&
                        !kills.Contains(enemy))
                    .OrderBy(enemy =>
                        origin.DistanceSquared(
                            LoopPosition(enemy.Progress)))
                    .FirstOrDefault();
                if (next == null)
                {
                    continue;
                }

                DamageDealt += next.TakeDamage(damage);
                if (!next.IsAlive)
                {
                    kills.Add(next);
                }
            }
        }

        private void DefeatEnemy(BattleEnemyState enemy)
        {
            if (!_enemies.Remove(enemy))
            {
                return;
            }
            if (enemy == _focus)
            {
                _focus = null;
            }

            int reward = Math.Max(
                1,
                (int)Math.Round(
                    (enemy.Definition.Reward +
                     enemy.Wave / 2f) *
                    RunBalanceRules.KillGoldScale *
                    _modifiers.KillGoldMultiplier));
            Gold += reward;
            DefeatedEnemies++;
            if (enemy.Definition.IsElite)
            {
                DefeatedElites++;
            }
            if (enemy.Definition.IsBoss)
            {
                DefeatedBosses++;
            }

            Score += (int)Math.Round(
                reward *
                (enemy.Definition.IsBoss
                    ? 20f
                    : enemy.Definition.IsElite
                        ? 6f
                        : 2f) *
                _modifiers.ScoreMultiplier);
            EnemyDefeated?.Invoke(enemy, reward);
        }

        private void End(CombinationDefenseState result)
        {
            SetState(result);
        }

        private void SetState(
            CombinationDefenseState state)
        {
            if (State == state)
            {
                return;
            }

            State = state;
            StateChanged?.Invoke(state);
        }

        private static float Lerp(
            float start,
            float end,
            float amount)
        {
            return start + (end - start) * amount;
        }
    }
}
