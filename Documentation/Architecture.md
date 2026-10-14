# Technical Architecture

## Layers

### Core

Plain C# definitions and deterministic rules:

- sword, recipe, enemy, wave, and stage data
- seeded random number service
- inventory and forging rules
- combat state and reward calculations
- enemy ability and protagonist skill definitions
- save schema and migration contracts

Core rules must not access `GameObject`, `MonoBehaviour`, scenes, or platform SDKs.

### Runtime

Unity presentation and orchestration:

- runtime bootstrap and screen flow
- battlefield, sword, enemy, and projectile views
- enemy, projectile, and lightweight combat views
- portrait forge-board input
- animation, audio, runtime visuals, and accessibility presentation
- localization and accessibility presentation

### Platform adapters

Replaceable boundaries:

- local/cloud save
- rewarded advertisements
- Google Play Billing
- analytics and crash reporting
- Play Games achievements and leaderboards

The offline development candidate does not require a platform adapter. Production
adapters remain disabled until their console identifiers and privacy declarations are
configured.

Advertising and billing packages are not included in the offline development candidate.
Future platform work must be reintroduced behind adapters rather than mixed into combat
or progression rules.

## Determinism

- Each run records a 64-bit seed.
- Content uses stable string IDs instead of Unity instance IDs.
- Core random decisions use a reproducible seeded generator.
- Runtime presentation advances with Unity delta time while run content choices retain
  the recorded seed.
- A run summary records seed, content version, decisions, and final score.
- A run summary also records damage, summons, forge outcomes, ultimate usage, enemy
  ability activations, rune choices, and peak star level.
- A pure automated simulator replays hundreds of seeds without Unity objects for
  regression and balance checks.

## Save model

- Versioned JSON envelope
- Atomic replace with backup
- Locale-independent numbers and timestamps
- Schema-aware migration and normalization from earlier saves
- No paid entitlement is trusted solely from local storage in production
- Schema 6 adds normalized lifetime combat counters, ultimate/enemy ability records,
  rare appearances, and per-sword mastery.

## Android development target

- Application ID: `com.ymlee.swordstellation`
- Portrait orientation
- APK development build
- IL2CPP ARM64
- Minimum Android API 25 (Unity 6.3 supported floor)
- Target API 36
