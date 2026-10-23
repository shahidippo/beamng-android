#!/usr/bin/env python3
"""Reject absent, empty, failed or wholly skipped Unity Test Framework runs."""
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

path = Path(sys.argv[1])
if not path.is_file():
    raise SystemExit(f'Missing current-run Unity test results: {path}')
root = ET.parse(path).getroot()
cases = list(root.iter('test-case'))
passed = sum(t.get('result') == 'Passed' for t in cases)
failed = sum(t.get('result') == 'Failed' for t in cases)
skipped = len(cases) - passed - failed
print(f'{path.name}: {passed} passed, {failed} failed, {skipped} skipped/inconclusive')
if not passed or failed or root.get('result') != 'Passed':
    raise SystemExit(1)
