#!/usr/bin/env python3
"""Create stable GUIDs for new original assets without overwriting existing Unity metadata."""
from pathlib import Path
import uuid

root = Path(__file__).resolve().parents[1]
namespace = uuid.UUID('09580539-fef0-4ca1-ac1a-320d741e1f8f')
created = 0
for path in sorted((root / 'Assets').rglob('*')):
    if path.name.endswith('.meta') or 'Generated' in path.parts:
        continue
    meta = path.with_name(path.name + '.meta')
    if meta.exists():
        continue
    guid = uuid.uuid5(namespace, path.relative_to(root).as_posix()).hex
    content = f'fileFormatVersion: 2\nguid: {guid}\n'
    if path.is_dir():
        content += 'folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n'
    meta.write_text(content)
    created += 1
print(f'Created {created} missing Unity asset metadata files; existing GUIDs preserved.')
