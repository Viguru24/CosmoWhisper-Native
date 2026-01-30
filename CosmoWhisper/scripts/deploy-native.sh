#!/bin/bash

# CosmoWhisper Native macOS Deployment & Notarization Script
# This script handles building, signing, and notarizing the Swift Native app.
# VERSIONING: This version automatically adds a timestamp to prevent overwrites.

# --- 1. SETTINGS ---
TEAM_ID="9AAQ3L289K"
APPLE_ID="louisdesouza@gmail.com"
APPLE_PASS="enus-gkwr-gybr-koov"
IDENTITY="Developer ID Application: Louis de Souza (9AAQ3L289K)"
BUNDLE_ID="com.cosmowhisper.CosmoWhisper"
APP_NAME="CosmoWhisper"
PROJECT_PATH="CosmoWhisper.xcodeproj"
SCHEME="CosmoWhisper"

# 2. GENERATE UNIQUE VERSION FOR THIS BUILD
TIMESTAMP=$(date +%Y%m%d-%H%M)
# Get version from Xcode project
RAW_VERSION=$(xcodebuild -showBuildSettings -project "$PROJECT_PATH" -scheme "$SCHEME" | grep MARKETING_VERSION | awk '{print $3}')
VERSION="${RAW_VERSION:-2.2.6}"
BUILD_NAME="${APP_NAME}_${VERSION}_${TIMESTAMP}"
OUT_DIR="release/$BUILD_NAME"

echo "🚀 Starting Production Build for $BUILD_NAME..."

# --- 3. CLEANUP & PREP ---
mkdir -p "$OUT_DIR"

# --- 4. BUILD ARCHIVE ---
echo "📦 Archiving Project..."
xcodebuild -project "$PROJECT_PATH" \
           -scheme "$SCHEME" \
           -configuration Release \
           -archivePath "$OUT_DIR/$APP_NAME.xcarchive" \
           archive -quiet

if [ $? -ne 0 ]; then
    echo "❌ ERROR: Archive failed."
    exit 1
fi

# --- 5. EXPORT APP ---
if [ ! -f "exportOptions.plist" ]; then
    echo "📄 Creating exportOptions.plist..."
    cat <<EOF > exportOptions.plist
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>method</key>
    <string>developer-id</string>
    <key>teamID</key>
    <string>$TEAM_ID</string>
    <key>signingStyle</key>
    <string>automatic</string>
</dict>
</plist>
EOF
fi

echo "📤 Exporting App Bundle..."
xcodebuild -exportArchive \
           -archivePath "$OUT_DIR/$APP_NAME.xcarchive" \
           -exportOptionsPlist exportOptions.plist \
           -exportPath "$OUT_DIR/Exported" -quiet

APP_BUNDLE="$OUT_DIR/Exported/$APP_NAME.app"

if [ ! -d "$APP_BUNDLE" ]; then
    echo "❌ ERROR: App bundle not found."
    exit 1
fi

# --- 6. SIGNING & HARDENING ---
echo "🔐 Hardening and Signing Application..."
codesign --force --options runtime --deep --sign "$IDENTITY" "$APP_BUNDLE"

# --- 7. CREATE DMG ---
echo "📦 Creating Unique DMG: $BUILD_NAME.dmg"
# Create a temporary directory for the DMG source to avoid issues
DMG_SRC="$OUT_DIR/dmg_source"
mkdir -p "$DMG_SRC"
cp -R "$APP_BUNDLE" "$DMG_SRC/"
ln -s /Applications "$DMG_SRC/Applications"

DMG_PATH="$OUT_DIR/$BUILD_NAME.dmg"
hdiutil create -volname "$APP_NAME" -srcfolder "$DMG_SRC" -ov -format UDZO "$DMG_PATH"
rm -rf "$DMG_SRC"

# --- 8. NOTARIZATION ---
echo "🚀 Submitting to Apple for Notarization: $DMG_PATH"
xcrun notarytool submit "$DMG_PATH" \
      --apple-id "$APPLE_ID" \
      --password "$APPLE_PASS" \
      --team-id "$TEAM_ID" \
      --wait

if [ $? -eq 0 ]; then
    echo "✅ Notarization Successful!"
    echo "🧐 Stapling Ticket..."
    xcrun stapler staple "$DMG_PATH"
    echo "🎉 Your UNIQUE notarized DMG is ready!"
    echo "📍 Path: $DMG_PATH"
else
    echo "❌ Notarization failed."
    exit 1
fi
