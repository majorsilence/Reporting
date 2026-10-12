#!/usr/bin/env bash
# Builds a .flatpak bundle for one GUI app.
#
#   installers/linux/build-flatpak.sh <ReportDesigner|RdlReader> <linux-x64|linux-arm64>
#
# Needs: dotnet SDK, flatpak, flatpak-builder, ImageMagick. The Flathub remote must be reachable
# (the org.freedesktop runtime is installed from it). The arm64 bundle must be built on arm64.
# Output: Release-Builds/build-output/installers/*.flatpak
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
  linux-x64)   FARCH=x86_64 ;;
  linux-arm64) FARCH=aarch64 ;;
  *) echo "Unsupported RID: $RID" >&2; exit 1 ;;
esac

VERSION=$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' "$ROOT/Directory.Build.props" | head -n1)
APP_ID="com.majorsilence.$APP"
RUNTIME_VERSION=24.08

WORK="$ROOT/Release-Builds/installer-work/flatpak-$APP-$RID"
OUT="$ROOT/Release-Builds/build-output/installers"
rm -rf "$WORK"; mkdir -p "$WORK/publish" "$OUT"

dotnet publish "$ROOT/$APP" -c Release -r "$RID" -f net10.0 --self-contained true \
  -p:GeneratePackageOnBuild=false -o "$WORK/publish"
chmod 755 "$WORK/publish/$APP"

convert "$ROOT/RdlReader/App.ico[0]" -resize 256x256 "$WORK/$APP_ID.png"
cat > "$WORK/$APP_ID.desktop" <<DESKTOP
[Desktop Entry]
Type=Application
Name=$TITLE
Comment=$SUMMARY
Exec=$APP
Icon=$APP_ID
Terminal=false
Categories=Office;Development;
DESKTOP

# The .NET runtime is bundled (self-contained), so only the freedesktop runtime is needed.
# Globalization-invariant avoids a libicu dependency the runtime does not ship.
cat > "$WORK/$APP_ID.yml" <<YAML
app-id: $APP_ID
runtime: org.freedesktop.Platform
runtime-version: '$RUNTIME_VERSION'
sdk: org.freedesktop.Sdk
command: $APP
finish-args:
  - --share=ipc
  - --share=network
  - --socket=x11
  - --socket=wayland
  - --device=dri
  - --filesystem=home
  - --env=DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
modules:
  - name: $APP
    buildsystem: simple
    build-commands:
      - mkdir -p /app/lib/$APP /app/bin /app/share/applications /app/share/icons/hicolor/256x256/apps
      - cp -a publish/. /app/lib/$APP/
      - ln -s /app/lib/$APP/$APP /app/bin/$APP
      - install -Dm644 $APP_ID.desktop /app/share/applications/$APP_ID.desktop
      - install -Dm644 $APP_ID.png /app/share/icons/hicolor/256x256/apps/$APP_ID.png
    sources:
      - type: dir
        path: publish
        dest: publish
      - type: file
        path: $APP_ID.desktop
      - type: file
        path: $APP_ID.png
YAML

cd "$WORK"
flatpak remote-add --user --if-not-exists flathub https://dl.flathub.org/repo/flathub.flatpakrepo
flatpak-builder --user --force-clean --install-deps-from=flathub --arch="$FARCH" \
  --repo=repo build "$APP_ID.yml"
flatpak build-bundle --arch="$FARCH" repo "$OUT/$APP-$VERSION-$RID.flatpak" "$APP_ID"
ls -lh "$OUT"
