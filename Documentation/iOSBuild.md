# iPhone build

The iPhone version is built from the same Unity project and the same game code
as Android. Only player settings, the home-screen name, and one small native
helper differ.

## Identity

- Bundle ID: `com.ymlee.swordstellation` (same as the Android package)
- Team: `3276G27R59`, automatic signing
- iPhone only, portrait, iOS 15.0 or later
- Version follows Android (`0.18.0`); the iOS build number is kept in
  `ProjectSettings` (`buildNumber: iPhone`) and raised for every upload
- Home-screen name: `검별공방` on Korean phones, `Swordstellation` elsewhere
- `ITSAppUsesNonExemptEncryption = NO`; no ads, billing, tracking, or network
  SDK, so there are no permission prompts

## What differs from Android

| Place | Android | iPhone |
| --- | --- | --- |
| `Editor/ProjectSetup.cs` | Gradle path fix, SDK levels, adaptive icon | compiled only on Windows or with Android active (`UnityEditor.Android` is missing from the Mac editor that has only the iOS module) |
| `Editor/IosBuild.cs` | — | iOS player settings, every icon slot filled each build, device and simulator builds |
| `Editor/IosPostBuild.cs` | — | Info.plist: English default name, `CFBundleLocalizations` (en, ko), `ko.lproj` / `en.lproj` names, export compliance |
| `Plugins/iOS/SwordNative.mm`, `Runtime/IosNative.cs` | returns nothing | reads launch arguments (iOS keeps `-key value` in NSUserDefaults, not in the command line) |
| `GameBootstrap.ApplyLaunchShot` | no effect | `-shot <screen>`, `-locale en`, `-summon N` pick a screen for simulator screenshots |

Everything else (rules, art, audio, bundled Pretendard/Hahmlet fonts, safe area,
save file in `Application.persistentDataPath`) is shared unchanged.

## Requirements on the Mac

- Unity `6000.3.18f1` with **iOS Build Support** (Unity Hub)
- Xcode 26 or later

## Build

```bash
Tools/ios-build.sh sim     # simulator build, installs on SIM (default: booted)
Tools/ios-build.sh         # device project in Build/ios/Unity-iPhone.xcodeproj
```

The Unity editor must not have the project open while the script runs.
The first run imports the whole project and takes about 15 minutes.

## Simulator screenshots

Taps cannot be injected into the simulator on the build Mac, so a launch
argument chooses the screen:

```bash
xcrun simctl launch <udid> com.ymlee.swordstellation -shot battle -summon 8
xcrun simctl io <udid> screenshot shot.png
```

Screens: `stages`, `settings`, `tutorial`, `codex`, `recipes`, `enemies`,
`runes`, `stats`, `upgrades`, `shop`, `battle` (`-step N` plays learning step N,
`-summon N` summons N swords after a second). `-locale en` or `-locale ko`
switches the game language first (it is saved, like the in-game button).

`Tools/ios-shots.sh` takes all of them in one run.

## Store release

Not started. As on Android, the store release waits until the game is judged
finished. When it starts, follow the iPhone checklist used for the other apps
(App Store Connect app, screenshots 1206×2622, privacy answers "not collected",
China mainland excluded).
