#!/bin/bash

# --- 1. SETTINGS ---
TEAM_ID="9AAQ3L289K"
APPLE_ID="louisdesouza@gmail.com"
APPLE_PASS="enus-gkwr-gybr-koov"
IDENTITY="Developer ID Application: Louis de Souza (9AAQ3L289K)"
BUNDLE_ID="com.cosmowhisper.CosmoWhisper"
APP_NAME="CosmoWhisper"
VERSION="2.1.4"
DMG_NAME="CosmoWhisper_v${VERSION}_Universal.dmg"
OUT_DIR="release"
BUILD_DIR="build_release"

echo "🚀 Starting Official Professional Notarized Universal Build..."

# 2. Cleanup
mkdir -p "$OUT_DIR"
rm -rf "./$BUILD_DIR"
# Remove old DMG if exists to avoid create-dmg errors
rm -f "$OUT_DIR/$DMG_NAME"

# 3. Build & Archive
echo "📦 Building Universal Binary..."
xcodebuild -project CosmoWhisper.xcodeproj \
           -scheme CosmoWhisper \
           -configuration Release \
           -derivedDataPath ./"$BUILD_DIR" \
           ARCHS="arm64 x86_64" \
           ONLY_ACTIVE_ARCH=NO \
           clean build -quiet

if [ $? -ne 0 ]; then
    echo "❌ Build failed!"
    exit 1
fi

APP_BUNDLE="./$BUILD_DIR/Build/Products/Release/$APP_NAME.app"
ENTITLEMENTS="CosmoWhisper/CosmoWhisper.entitlements"

# 4. Sign with Developer ID (Hardened Runtime required for notarization)
echo "🔐 Signing App Bundle..."
codesign --force --options runtime --deep --sign "$IDENTITY" --entitlements "$ENTITLEMENTS" "$APP_BUNDLE"

# 5. Create Standard DMG (No Mounting Required)
echo "📦 Creating Standard DMG..."
DMG_PATH="$OUT_DIR/$DMG_NAME"
# Simple directory-to-DMG conversion. No mounting needed.
hdiutil create -volname "CosmoWhisper_Installer" -srcfolder "$APP_BUNDLE" -ov -format UDZO "$DMG_PATH"

# 6. Sign the DMG (Recommended for Gatekeeper)
echo "🔐 Signing DMG..."
codesign --force --sign "$IDENTITY" "$DMG_PATH"

# 7. Notarize
echo "🚀 Submitting to Apple for Notarization..."
xcrun notarytool submit "$DMG_PATH" \
      --apple-id "$APPLE_ID" \
      --password "$APPLE_PASS" \
      --team-id "$TEAM_ID" \
      --wait

if [ $? -eq 0 ]; then
    echo "✅ Notarization Successful!"
    echo "🧐 Stapling Ticket..."
    xcrun stapler staple "$DMG_PATH"
    echo "------------------------------------------------"
    echo "🎉 YOUR PROFESSIONAL NOTARIZED UNIVERSAL DMG IS READY!"
    echo "📍 Path: $DMG_PATH"
    echo "------------------------------------------------"
else
    echo "❌ Notarization failed."
    exit 1
fi
