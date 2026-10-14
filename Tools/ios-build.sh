#!/bin/bash
# Builds the iPhone Xcode project on the Mac.
#
#   Tools/ios-build.sh        device    -> Build/ios/Unity-iPhone.xcodeproj
#   Tools/ios-build.sh sim    simulator -> Build/ios-sim, then installs on the
#                             simulator in SIM (name or UDID, default: booted)
#
# Fails while a Unity editor has this project open.
set -euo pipefail
cd "$(dirname "$0")/.."

UNITY="/Applications/Unity/Hub/Editor/6000.3.18f1/Unity.app/Contents/MacOS/Unity"
export LANG=en_US.UTF-8 LC_ALL=en_US.UTF-8
mkdir -p Build

if [[ "${1:-}" == "sim" ]]; then METHOD=BuildIosSimulator; OUT=Build/ios-sim; else METHOD=BuildIos; OUT=Build/ios; fi

"$UNITY" -batchmode -quit -nographics -projectPath "$PWD" -buildTarget iOS \
  -executeMethod Swordstellation.Editor.IosBuild.$METHOD -logFile Build/unity-ios.log \
  || { grep -E "error CS|\[IosBuild\]" Build/unity-ios.log | head -20; exit 1; }
grep -E "\[IosBuild\]|\[IosPostBuild\]" Build/unity-ios.log || true

if [[ "${1:-}" == "sim" ]]; then
  xcodebuild -project $OUT/Unity-iPhone.xcodeproj -scheme Unity-iPhone -configuration Release \
    -sdk iphonesimulator -destination 'generic/platform=iOS Simulator' \
    -derivedDataPath Build/ios-sim-dd CODE_SIGNING_ALLOWED=NO build 2>&1 | grep -E "error:|BUILD (SUCCEEDED|FAILED)"
  APP=$(ls -d Build/ios-sim-dd/Build/Products/Release-iphonesimulator/*.app | head -1)
  xcrun simctl install "${SIM:-booted}" "$APP" && echo "installed: $APP"
fi
