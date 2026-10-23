#!/usr/bin/env bash
# Run deliberately against one connected Android device after a successful APK build.
set -euo pipefail
repo_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
apk="${1:-$repo_dir/Artifacts/Android/ImpactDrive-development.apk}"
: "${ANDROID_SERIAL:?Set ANDROID_SERIAL to the device listed by adb devices.}"
command -v adb >/dev/null || { printf '%s\n' 'Install Android SDK platform-tools (adb).' >&2; exit 2; }
[[ -f "$apk" ]] || { printf 'APK does not exist: %s\n' "$apk" >&2; exit 2; }
adb -s "$ANDROID_SERIAL" get-state
api="$(adb -s "$ANDROID_SERIAL" shell getprop ro.build.version.sdk | tr -d '\r')"
abis="$(adb -s "$ANDROID_SERIAL" shell getprop ro.product.cpu.abilist | tr -d '\r')"
[[ "$api" =~ ^[0-9]+$ && "$api" -ge 26 ]] || { printf '%s\n' 'Android API26 or newer required.' >&2; exit 2; }
[[ "$abis" == *arm64-v8a* ]] || { printf '%s\n' 'This APK requires ARM64.' >&2; exit 2; }
mkdir -p "$repo_dir/Artifacts/Android"
adb -s "$ANDROID_SERIAL" shell getprop ro.product.model > "$repo_dir/Artifacts/Android/device-model.txt"
printf 'API=%s\nABI=%s\n' "$api" "$abis" > "$repo_dir/Artifacts/Android/device-platform.txt"
adb -s "$ANDROID_SERIAL" install -r "$apk"
adb -s "$ANDROID_SERIAL" shell monkey -p com.impactdrive.mobile -c android.intent.category.LAUNCHER 1
printf '%s\n' 'Installation and launch were requested. Complete Docs/AndroidQA.md on the device; this command does not certify gameplay or performance.'
