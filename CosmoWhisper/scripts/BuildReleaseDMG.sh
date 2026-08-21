#!/bin/bash
set -e

# --- CONFIGURATION ---
APP_NAME="CosmoWhisper"
VERSION="2.1.0"
DMG_NAME="CosmoWhisper_v${VERSION}_Universal.dmg"
OUT_DIR="release"
BUILD_DIR="build_release"
IDENTITY="Developer ID Application: Louis de Souza (9AAQ3L289K)"
TEAM_ID="9AAQ3L289K"
APPLE_ID="louisdesouza@gmail.com"
APPLE_PASS="enus-gkwr-gybr-koov"

echo "=============================================="
echo "🚀 BUILDING & PACKAGING COSMOWHISPER DMG"
echo "=============================================="

# 1. Clean Directories
rm -rf "$BUILD_DIR" "$OUT_DIR"
mkdir -p "$OUT_DIR"

# 2. Build Universal Release Binary
echo "🔨 Compiling Universal Release Binary (Apple Silicon + Intel)..."
xcodebuild -project CosmoWhisper.xcodeproj \
           -scheme CosmoWhisper \
           -configuration Release \
           -derivedDataPath "./$BUILD_DIR" \
           ARCHS="arm64 x86_64" \
           ONLY_ACTIVE_ARCH=NO \
           clean build -quiet

APP_BUNDLE="./$BUILD_DIR/Build/Products/Release/$APP_NAME.app"
ENTITLEMENTS="CosmoWhisper/CosmoWhisper.entitlements"

# 3. Code Sign App Bundle with Hardened Runtime
echo "✍️  Deep Signing App Bundle with Developer ID..."
codesign --force --options runtime --deep --sign "$IDENTITY" --entitlements "$ENTITLEMENTS" "$APP_BUNDLE"
codesign --verify --deep --strict "$APP_BUNDLE"

# 4. Prepare Staging Folder for DMG
echo "📦 Staging DMG layout..."
DMG_STAGING="./$BUILD_DIR/dmg_staging"
rm -rf "$DMG_STAGING"
mkdir -p "$DMG_STAGING"
cp -R "$APP_BUNDLE" "$DMG_STAGING/"
ln -s /Applications "$DMG_STAGING/Applications"

DMG_PATH="$OUT_DIR/$DMG_NAME"

# 5. Create DMG
echo "📀 Generating DMG with create-dmg..."
if [ -x "/usr/local/bin/create-dmg" ]; then
    /usr/local/bin/create-dmg \
      --volname "$APP_NAME Installer" \
      --window-pos 200 120 \
      --window-size 660 400 \
      --icon-size 128 \
      --icon "$APP_NAME.app" 180 200 \
      --hide-extension "$APP_NAME.app" \
      --app-drop-link 480 200 \
      --skip-jenkins \
      "$DMG_PATH" \
      "$DMG_STAGING" || {
        echo "⚠️ create-dmg encountered a layout warning, falling back to clean hdiutil creation..."
        rm -f "$DMG_PATH"
        hdiutil create -volname "CosmoWhisper Installer" -srcfolder "$DMG_STAGING" -ov -format UDZO "$DMG_PATH"
      }
else
    echo "Creating DMG via hdiutil..."
    hdiutil create -volname "CosmoWhisper Installer" -srcfolder "$DMG_STAGING" -ov -format UDZO "$DMG_PATH"
fi

# 6. Sign the DMG
echo "✍️  Signing DMG..."
codesign --force --sign "$IDENTITY" "$DMG_PATH"

# 7. Notarize with Apple
echo "☁️  Submitting DMG to Apple Notarization Service..."
xcrun notarytool submit "$DMG_PATH" \
      --apple-id "$APPLE_ID" \
      --password "$APPLE_PASS" \
      --team-id "$TEAM_ID" \
      --wait

# 8. Staple Notarization Ticket
echo "📎 Stapling Notarization Ticket to DMG..."
xcrun stapler staple "$DMG_PATH"

echo "=============================================="
echo "🎉 SUCCESS! OFFICIAL NOTARIZED DMG CREATED!"
echo "📍 File Location: $(pwd)/$DMG_PATH"
echo "=============================================="
