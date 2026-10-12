#!/usr/bin/env bash
# Builds a .pkg installer for the RdlCmd command line tool. Run on macOS.
#
#   installers/macos/build-macos-pkg.sh <osx-arm64|osx-x64>
#
# Installs to /usr/local/lib/majorsilence-rdlcmd with a /usr/local/bin/rdlcmd symlink. Ad-hoc
# signed and not notarized. Output: Release-Builds/build-output/installers/
set -euo pipefail

RID="${1:?runtime id: osx-arm64 or osx-x64}"
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
VERSION=$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' "$ROOT/Directory.Build.props" | head -n1)
PKG_VERSION="${VERSION%%-*}"

WORK="$ROOT/Release-Builds/installer-work/RdlCmd-$RID"
OUT="$ROOT/Release-Builds/build-output/installers"
PAYLOAD="$WORK/payload"
LIB="$PAYLOAD/usr/local/lib/majorsilence-rdlcmd"
rm -rf "$WORK"; mkdir -p "$LIB" "$PAYLOAD/usr/local/bin" "$OUT"

dotnet publish "$ROOT/RdlCmd" -c Release -r "$RID" -f net10.0 --self-contained true \
  -p:GeneratePackageOnBuild=false -o "$LIB"
chmod 755 "$LIB/RdlCmd"
codesign --force -s - "$LIB/RdlCmd"
ln -s /usr/local/lib/majorsilence-rdlcmd/RdlCmd "$PAYLOAD/usr/local/bin/rdlcmd"

pkgbuild --root "$PAYLOAD" --identifier com.majorsilence.reporting.rdlcmd \
  --version "$PKG_VERSION" "$OUT/RdlCmd-$VERSION-$RID.pkg"
ls -lh "$OUT"
