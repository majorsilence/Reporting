#!/usr/bin/env bash
# Builds a .app bundle and .dmg for one desktop app. Run on macOS.
#
#   installers/macos/build-macos-dmg.sh <ReportDesigner|RdlReader> <osx-arm64|osx-x64>
#
# The app is ad-hoc signed, not Developer-ID signed or notarized, so Gatekeeper asks the user
# to approve it on first launch (right-click > Open). Output: Release-Builds/build-output/installers/
set -euo pipefail

APP="${1:?app name: ReportDesigner or RdlReader}"
RID="${2:?runtime id: osx-arm64 or osx-x64}"
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

case "$APP" in
  ReportDesigner) TITLE="Majorsilence Report Designer"; BUNDLE_ID="com.majorsilence.reporting.reportdesigner" ;;
  RdlReader)      TITLE="Majorsilence Report Reader";   BUNDLE_ID="com.majorsilence.reporting.rdlreader" ;;
  *) echo "Unknown app: $APP" >&2; exit 1 ;;
esac

VERSION=$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' "$ROOT/Directory.Build.props" | head -n1)
# CFBundleVersion must be dotted numerics; drop any pre-release label.
PLIST_VERSION="${VERSION%%-*}"

WORK="$ROOT/Release-Builds/installer-work/$APP-$RID"
OUT="$ROOT/Release-Builds/build-output/installers"
BUNDLE="$WORK/dmg/$TITLE.app"
rm -rf "$WORK"; mkdir -p "$WORK/publish" "$BUNDLE/Contents/MacOS" "$BUNDLE/Contents/Resources" "$OUT"

dotnet publish "$ROOT/$APP" -c Release -r "$RID" -f net10.0 --self-contained true \
  -p:GeneratePackageOnBuild=false -o "$WORK/publish"
cp -R "$WORK/publish/." "$BUNDLE/Contents/MacOS/"
chmod 755 "$BUNDLE/Contents/MacOS/$APP"

# Icon: .ico -> .png -> .icns
sips -s format png "$ROOT/RdlReader/App.ico" --out "$WORK/icon.png" >/dev/null
ICONSET="$WORK/App.iconset"; mkdir -p "$ICONSET"
for s in 16 32 64 128 256 512; do
  sips -z $s $s "$WORK/icon.png" --out "$ICONSET/icon_${s}x${s}.png" >/dev/null
done
iconutil -c icns "$ICONSET" -o "$BUNDLE/Contents/Resources/App.icns"

cat > "$BUNDLE/Contents/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0"><dict>
  <key>CFBundleName</key><string>$TITLE</string>
  <key>CFBundleDisplayName</key><string>$TITLE</string>
  <key>CFBundleIdentifier</key><string>$BUNDLE_ID</string>
  <key>CFBundleExecutable</key><string>$APP</string>
  <key>CFBundleIconFile</key><string>App</string>
  <key>CFBundlePackageType</key><string>APPL</string>
  <key>CFBundleShortVersionString</key><string>$VERSION</string>
  <key>CFBundleVersion</key><string>$PLIST_VERSION</string>
  <key>NSHighResolutionCapable</key><true/>
  <key>LSMinimumSystemVersion</key><string>12.0</string>
</dict></plist>
PLIST

# arm64 binaries must be signed to run at all; ad-hoc is enough for that.
codesign --force --deep -s - "$BUNDLE"

ln -s /Applications "$WORK/dmg/Applications"
hdiutil create -volname "$TITLE" -srcfolder "$WORK/dmg" -ov -format UDZO \
  "$OUT/$APP-$VERSION-$RID.dmg"
ls -lh "$OUT"
