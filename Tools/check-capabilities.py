#!/usr/bin/env python3
"""Report tool presence without reading credentials or dumping the process environment."""
import json
import os
import shutil
from pathlib import Path

unity = os.environ.get('UNITY_EDITOR') or shutil.which('Unity') or shutil.which('unity-editor')
report = {
    'unity_editor_available': bool(unity and Path(unity).is_file()),
    'unity_license_status': 'not checked; no credential values inspected',
    'android_sdk_configured': bool(os.environ.get('ANDROID_HOME') or os.environ.get('ANDROID_SDK_ROOT')),
    'adb_available': bool(shutil.which('adb')),
    'blender_available': bool(shutil.which('blender')),
    'dotnet_available': bool(shutil.which('dotnet') or Path('/workspace/.tools/dotnet/dotnet').is_file()),
    'first_device_os_target': 'Android 12 / API 31',
    'first_device_model': 'not specified',
    'unity_runtime_validation': 'not established by this check',
    'android_hardware_validation': 'not established by this check',
}
print(json.dumps(report, indent=2))
