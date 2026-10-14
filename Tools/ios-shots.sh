#!/bin/bash
# Takes one simulator screenshot per screen (after Tools/ios-build.sh sim).
#
#   Tools/ios-shots.sh [ko|en]     -> Build/shots/<locale>/<screen>.png
#
# SIM picks the simulator (name or UDID, default: booted).
set -euo pipefail
cd "$(dirname "$0")/.."

SIM="${SIM:-booted}"
LOCALE="${1:-ko}"
BUNDLE=com.ymlee.swordstellation
OUT="Build/shots/$LOCALE"
mkdir -p "$OUT"

shot() {
  local name="$1" wait="$2"; shift 2
  xcrun simctl terminate "$SIM" "$BUNDLE" 2>/dev/null || true
  xcrun simctl launch "$SIM" "$BUNDLE" -locale "$LOCALE" "$@" >/dev/null
  sleep "$wait"
  xcrun simctl io "$SIM" screenshot "$OUT/$name.png" >/dev/null 2>&1
  echo "$OUT/$name.png"
}

shot main 6
for screen in stages settings tutorial codex recipes enemies runes stats upgrades shop; do
  shot "$screen" 5 -shot "$screen"
done
shot battle 12 -shot battle -summon 8
shot battle-step1 12 -shot battle -step 1 -summon 8
xcrun simctl terminate "$SIM" "$BUNDLE" 2>/dev/null || true
