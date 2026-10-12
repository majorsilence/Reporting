#!/usr/bin/env bash
# Builds a .snap for one app. Run on Ubuntu 24.04 (the snap is core24-based).
#
#   installers/linux/build-snap.sh <ReportDesigner|RdlReader|RdlCmd> <linux-x64|linux-arm64>
#
# Needs: dotnet SDK, snapcraft (sudo snap install snapcraft --classic). Builds with
# --destructive-mode on the host, so no LXD is needed. Output: Release-Builds/build-output/installers/*.snap
set -euo pipefail

APP="${1:?app name: ReportDesigner, RdlReader or RdlCmd}"
RID="${2:?runtime id: linux-x64 or linux-arm64}"
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

case "$APP" in
  ReportDesigner) SUMMARY="Cross-platform RDL report designer"; GUI=true ;;
  RdlReader)      SUMMARY="Cross-platform RDL report viewer";   GUI=true ;;
  RdlCmd)         SUMMARY="Command line report renderer";       GUI=false ;;
  *) echo "Unknown app: $APP" >&2; exit 1 ;;
esac

VERSION=$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' "$ROOT/Directory.Build.props" | head -n1)
GRADE=stable; case "$VERSION" in *-*) GRADE=devel ;; esac
NAME="majorsilence-$(echo "$APP" | tr '[:upper:]' '[:lower:]')"

WORK="$ROOT/Release-Builds/installer-work/snap-$NAME-$RID"
OUT="$ROOT/Release-Builds/build-output/installers"
rm -rf "$WORK"; mkdir -p "$WORK/publish" "$WORK/snap" "$OUT"

dotnet publish "$ROOT/$APP" -c Release -r "$RID" -f net10.0 --self-contained true \
  -p:GeneratePackageOnBuild=false -o "$WORK/publish"
chmod 755 "$WORK/publish/$APP"

if $GUI; then
  APPS="  $NAME:
    command: $APP
    extensions: [gnome]
    plugs: [home, network, removable-media, opengl, x11, wayland, desktop]
    environment:
      DOTNET_SYSTEM_GLOBALIZATION_INVARIANT: \"1\""
else
  # Strict confinement: the CLI can only read and write under \$HOME and removable media.
  APPS="  $NAME:
    command: $APP
    plugs: [home, network, removable-media]
    environment:
      DOTNET_SYSTEM_GLOBALIZATION_INVARIANT: \"1\""
fi

cat > "$WORK/snap/snapcraft.yaml" <<YAML
name: $NAME
title: Majorsilence $APP
version: '$VERSION'
summary: $SUMMARY
description: |
  $SUMMARY from Majorsilence Reporting.
  https://github.com/majorsilence/Reporting
license: Apache-2.0
base: core24
grade: $GRADE
confinement: strict
platforms:
  $([ "$RID" = linux-arm64 ] && echo arm64 || echo amd64):
    build-on: [$([ "$RID" = linux-arm64 ] && echo arm64 || echo amd64)]

apps:
$APPS

parts:
  app:
    plugin: dump
    source: publish
YAML

cd "$WORK"
snapcraft pack --destructive-mode --output "$OUT/"
ls -lh "$OUT"
