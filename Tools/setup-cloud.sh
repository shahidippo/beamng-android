#!/usr/bin/env bash
set -euo pipefail
repo_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
tool_dir="${IMPACTDRIVE_TOOLS_DIR:-/workspace/.tools}"
mkdir -p "$tool_dir"
python3 - "$tool_dir" <<'PY'
import hashlib, pathlib, sys, tarfile, urllib.request, subprocess
base = pathlib.Path(sys.argv[1])
version = '8.0.426'
sdk = base / 'dotnet'
if (sdk / 'dotnet').exists() and version in subprocess.check_output([str(sdk / 'dotnet'), '--list-sdks'], text=True):
    print('Pinned .NET SDK is already installed.')
else:
    url = f'https://builds.dotnet.microsoft.com/dotnet/Sdk/{version}/dotnet-sdk-{version}-linux-x64.tar.gz'
    expected = '8141d01bdc53f604aed3bb661b604707a64209f8a947cd81bf95b4f518951593d77864069191f54ea5e84362a409da6b0f69074bf893b283d7e0cf93e2bf20a8'
    archive = base / 'dotnet-sdk.tar.gz'
    with urllib.request.urlopen(url, timeout=60) as response, archive.open('wb') as output:
        while chunk := response.read(1024 * 1024):
            output.write(chunk)
    digest = hashlib.file_digest(archive.open('rb'), 'sha512').hexdigest()
    if digest != expected:
        raise SystemExit('SDK SHA512 verification failed; refusing to install.')
    sdk.mkdir(exist_ok=True)
    with tarfile.open(archive) as package:
        package.extractall(sdk, filter='data')
    print('Installed verified .NET SDK', version)
PY
export DOTNET_CLI_HOME="$tool_dir/dotnet-home"
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export NUGET_PACKAGES="$tool_dir/nuget-packages"
cd "$repo_dir"
"$tool_dir/dotnet/dotnet" --version
python3 Tools/verify-project.py
"$tool_dir/dotnet/dotnet" run --project Tests/Physics/ImpactDrive.Physics.Tests.csproj --configuration Release -- --report Artifacts/Tests/physics-results.json
