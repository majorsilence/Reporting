"""Validate the PDF/A samples written by ReportTests' PdfAValidationSamples with veraPDF.

Every .pdf in the folder is checked against the PDF/A flavour its XMP metadata claims (veraPDF's
default). The script fails when a file is not compliant, when veraPDF could not check a file, or
when the folder holds no PDFs at all.

Usage: python validate_pdfa.py <verapdf-executable> <samples-folder>
"""

import subprocess
import sys
import xml.etree.ElementTree as ET
from pathlib import Path


def local(tag: str) -> str:
    return tag.rsplit("}", 1)[-1]


def main() -> int:
    if len(sys.argv) != 3:
        print(__doc__)
        return 2
    verapdf, folder = sys.argv[1], Path(sys.argv[2])
    pdfs = sorted(folder.glob("*.pdf"))
    if not pdfs:
        print(f"No PDFs in {folder}")
        return 1

    result = subprocess.run(
        [verapdf, "--format", "mrr", *[str(p) for p in pdfs]],
        capture_output=True, text=True)
    if not result.stdout.strip():
        print(result.stderr)
        print(f"veraPDF wrote no report (exit code {result.returncode})")
        return 1

    root = ET.fromstring(result.stdout)
    checked, failures = set(), []
    for job in root.iter():
        if local(job.tag) != "job":
            continue
        name = next((e.text for e in job.iter() if local(e.tag) == "name"), "?")
        report = next((e for e in job.iter() if local(e.tag) == "validationReport"), None)
        if report is None:
            failures.append(f"{name}: not validated")
            continue
        checked.add(Path(name).name)
        if report.get("isCompliant") == "true":
            print(f"ok    {Path(name).name}  ({report.get('profileName')})")
            continue
        lines = [f"FAIL  {Path(name).name}  ({report.get('profileName')})"]
        for rule in report.iter():
            if local(rule.tag) == "rule" and rule.get("status") == "failed":
                desc = next((e.text for e in rule if local(e.tag) == "description"), "")
                lines.append(f"        {rule.get('specification')} {rule.get('clause')}-{rule.get('testNumber')}: "
                             f"{desc} ({rule.get('failedChecks')} checks)")
        failures.append("\n".join(lines))

    for pdf in pdfs:
        if pdf.name not in checked and not any(f.startswith(str(pdf)) for f in failures):
            failures.append(f"{pdf.name}: missing from the veraPDF report")

    for f in failures:
        print(f)
    print(f"{len(pdfs)} PDFs, {len(failures)} failing")
    return 1 if failures else 0


if __name__ == "__main__":
    sys.exit(main())
