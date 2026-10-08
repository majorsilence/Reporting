"""Validate the signed PDF samples written by Majorsilence.Pdf.Tests' PdfValidationSamples.

For every <name>.pdf in the folder, with <name>.json describing what it should hold:
  - an encrypted sample opens with the empty user password;
  - each signature is intact, valid, trusted (signer.pem is the trust root) and covers the whole file;
  - the field name and the /Reason, /Name and /Location strings decrypt to the expected values.

An independent reader is the point: Majorsilence.Pdf's own reader and writer could agree on a mistake
that every other reader rejects (#363: cleartext strings inside an encrypted, signed PDF).

Usage: python validate_pdf_signatures.py <samples-folder>
"""

import json
import sys
from pathlib import Path

from pyhanko.keys import load_cert_from_pemder
from pyhanko.pdf_utils.reader import PdfFileReader
from pyhanko.sign.validation.errors import SignatureValidationError
from pyhanko.sign.validation import validate_pdf_signature
from pyhanko.sign.validation.status import SignatureCoverageLevel
from pyhanko_certvalidator import ValidationContext


def check(pdf_path: Path, expected: dict, trust_root) -> list[str]:
    problems = []

    with pdf_path.open("rb") as f:
        reader = PdfFileReader(f)

        if bool(reader.encrypted) != expected["encrypted"]:
            problems.append(f"encrypted is {bool(reader.encrypted)}, expected {expected['encrypted']}")

        if reader.encrypted:
            reader.decrypt("")

        signatures = reader.embedded_signatures
        if len(signatures) != 1:
            return problems + [f"{len(signatures)} signatures, expected 1"]

        sig = signatures[0]

        if sig.field_name != expected["field_name"]:
            problems.append(f"field name {sig.field_name!r}, expected {expected['field_name']!r}")

        for key in ("reason", "name", "location"):
            want = expected.get(key)
            pdf_key = "/" + key.capitalize()
            got = sig.sig_object.get(pdf_key)
            # A string from an encrypted file comes wrapped; .decrypted is the plain value.
            got = None if got is None else str(getattr(got, "decrypted", got))
            if got != want:
                problems.append(f"{pdf_key} is {got!r}, expected {want!r}")

        context = ValidationContext(trust_roots=[trust_root], allow_fetching=False)
        try:
            status = validate_pdf_signature(sig, context)
        except (SignatureValidationError, ValueError) as e:
            return problems + [f"validation raised {type(e).__name__}: {e}"]

        if not status.intact:
            problems.append("signature is not intact")
        if not status.valid:
            problems.append("signature is not valid")
        if not status.trusted:
            problems.append("signer is not trusted")
        if status.coverage != SignatureCoverageLevel.ENTIRE_FILE:
            problems.append(f"coverage is {status.coverage}, expected ENTIRE_FILE")

    return problems


def main() -> int:
    if len(sys.argv) != 2:
        print(__doc__)
        return 2

    folder = Path(sys.argv[1])
    trust_root = load_cert_from_pemder(str(folder / "signer.pem"))
    samples = sorted(folder.glob("*.pdf"))

    if not samples:
        print(f"no PDF samples in {folder}")
        return 1

    failed = 0
    for pdf in samples:
        expected = json.loads(pdf.with_suffix(".json").read_text(encoding="utf-8-sig"))
        try:
            problems = check(pdf, expected, trust_root)
        except Exception as e:  # a reader that cannot even open the file is a failure of that sample
            problems = [f"{type(e).__name__}: {e}"]
        if problems:
            failed += 1
            print(f"FAIL {pdf.name}")
            for p in problems:
                print(f"     {p}")
        else:
            print(f"ok   {pdf.name}")

    print(f"{len(samples) - failed}/{len(samples)} samples valid")
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
