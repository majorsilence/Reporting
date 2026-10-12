#!/usr/bin/env bash
# Builds .deb and .rpm packages for one desktop app on Linux.
#
#   installers/linux/build-linux-packages.sh <ReportDesigner|RdlReader> <linux-x64|linux-arm64>
#
# Needs: dotnet SDK, nfpm (https://nfpm.goreleaser.com), ImageMagick (icon conversion).
# Output: Release-Builds/build-output/installers/*.deb and *.rpm
set -euo pipefail

APP="${1:?app name: ReportDesigner or RdlReader}"
RID="${2:?runtime id: linux-x64 or linux-arm64}"
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

case "$APP" in
  ReportDesigner) TITLE="Majorsilence Report Designer"; SUMMARY="Cross-platform RDL report designer" ;;
  RdlReader)      TITLE="Majorsilence Report Reader";   SUMMARY="Cross-platform RDL report viewer" ;;
  *) echo "Unknown app: $APP" >&2; exit 1 ;;
esac
case "$RID" in
  linux-x64)   ARCH=amd64 ;;
  linux-arm64) ARCH=arm64 ;;
  *) echo "Unsupported RID: $RID" >&2; exit 1 ;;
esac

VERSION=$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' "$ROOT/Directory.Build.props" | head -n1)
BIN="$(echo "$APP" | tr '[:upper:]' '[:lower:]')"      # /usr/bin/reportdesigner
PKG="majorsilence-$BIN"
OPT="/opt/majorsilence-reporting/$BIN"

WORK="$ROOT/Release-Builds/installer-work/$PKG-$RID"
OUT="$ROOT/Release-Builds/build-output/installers"
rm -rf "$WORK"; mkdir -p "$WORK/publish" "$OUT"

dotnet publish "$ROOT/$APP" -c Release -r "$RID" -f net10.0 --self-contained true \
  -p:GeneratePackageOnBuild=false -o "$WORK/publish"
chmod 755 "$WORK/publish/$APP"

# Icon: the apps ship a .ico; desktop environments want a PNG.
convert "$ROOT/RdlReader/App.ico[0]" -resize 256x256 "$WORK/$PKG.png"

cat > "$WORK/$PKG.desktop" <<DESKTOP
[Desktop Entry]
Type=Application
Name=$TITLE
Comment=$SUMMARY
Exec=$BIN %f
Icon=$PKG
Terminal=false
Categories=Office;Development;
DESKTOP

# nfpm expands \${VAR} in the config from the environment.
export NFPM_VERSION="$VERSION" NFPM_ARCH="$ARCH" NFPM_PKG="$PKG" NFPM_SUMMARY="$SUMMARY" \
       NFPM_OPT="$OPT" NFPM_BIN="$BIN" NFPM_APP="$APP" NFPM_WORK="$WORK"
cat > "$WORK/nfpm.yaml" <<'YAML'
name: ${NFPM_PKG}
arch: ${NFPM_ARCH}
platform: linux
version: ${NFPM_VERSION}
section: utils
priority: optional
maintainer: Majorsilence <https://github.com/majorsilence/Reporting>
description: ${NFPM_SUMMARY}
homepage: https://github.com/majorsilence/Reporting
license: Apache-2.0
contents:
  - src: ${NFPM_WORK}/publish
    dst: ${NFPM_OPT}
    type: tree
  - src: ${NFPM_OPT}/${NFPM_APP}
    dst: /usr/bin/${NFPM_BIN}
    type: symlink
  - src: ${NFPM_WORK}/${NFPM_PKG}.desktop
    dst: /usr/share/applications/${NFPM_PKG}.desktop
  - src: ${NFPM_WORK}/${NFPM_PKG}.png
    dst: /usr/share/icons/hicolor/256x256/apps/${NFPM_PKG}.png
# The app is self-contained (.NET included); these are the native libraries Avalonia/Skia load.
overrides:
  deb:
    depends: [libfontconfig1, libx11-6, libice6, libsm6]
  rpm:
    depends: [fontconfig, libX11, libICE, libSM, libicu]
YAML

for fmt in deb rpm; do
  nfpm package --config "$WORK/nfpm.yaml" --packager "$fmt" --target "$OUT/"
done
ls -lh "$OUT"
