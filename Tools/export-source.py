#!/usr/bin/env python3
"""Archive current source, docs and original assets, excluding ignored caches and credentials."""
from pathlib import Path
import hashlib
import subprocess
import zipfile

root = Path(__file__).resolve().parents[1]
output = root / 'Artifacts/ImpactDrive-source.zip'
output.parent.mkdir(exist_ok=True)
files = subprocess.check_output(
    ['git', 'ls-files', '-z', '--cached', '--others', '--exclude-standard'], cwd=root
).decode().split('\0')
with zipfile.ZipFile(output, 'w', zipfile.ZIP_DEFLATED, compresslevel=6) as archive:
    for name in sorted(set(files)):
        if not name:
            continue
        path = root / name
        if path.is_file():
            archive.write(path, 'ImpactDrive/' + name)
    failure = archive.testzip()
    if failure:
        raise SystemExit(f'Archive integrity failure: {failure}')
digest = hashlib.sha256(output.read_bytes()).hexdigest()
output.with_suffix('.zip.sha256').write_text(f'{digest}  {output.name}\n')
print(f'{output}: {output.stat().st_size} bytes; SHA256 {digest}')
