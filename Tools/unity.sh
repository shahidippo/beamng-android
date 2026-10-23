#!/usr/bin/env bash
set -euo pipefail
repo_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
mode="${1:-test}"
unity_editor="${UNITY_EDITOR:-}"
if [[ -z "$unity_editor" || ! -x "$unity_editor" ]]; then
  printf '%s\n' 'Set UNITY_EDITOR to the licensed Unity 6000.0.62f1 executable. Install its Android Build Support, SDK/NDK and OpenJDK for APK builds.' >&2
  exit 2
fi
mkdir -p "$repo_dir/Artifacts/Unity"
common=(-batchmode -nographics -projectPath "$repo_dir")
case "$mode" in
  configure)
    "$unity_editor" "${common[@]}" -quit -executeMethod ImpactDrive.Editor.ProjectSetup.Configure -logFile "$repo_dir/Artifacts/Unity/configure.log"
    ;;
  test)
    "$unity_editor" "${common[@]}" -quit -executeMethod ImpactDrive.Editor.ProjectSetup.Configure -logFile "$repo_dir/Artifacts/Unity/configure.log"
    for platform in EditMode PlayMode; do
      result="$repo_dir/Artifacts/Unity/$platform.xml"
      rm -f -- "$result"
      "$unity_editor" "${common[@]}" -runTests -testPlatform "$platform" -testResults "$result" -logFile "$repo_dir/Artifacts/Unity/$platform.log"
      python3 "$repo_dir/Tools/check-unity-results.py" "$result"
    done
    ;;
  android)
    "$unity_editor" "${common[@]}" -buildTarget Android -quit -executeMethod ImpactDrive.Editor.BuildAndroid.Development -logFile "$repo_dir/Artifacts/Unity/android.log"
    ;;
  *) printf '%s\n' 'Usage: Tools/unity.sh configure|test|android' >&2; exit 2 ;;
esac
