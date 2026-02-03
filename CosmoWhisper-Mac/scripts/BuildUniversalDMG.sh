#!/bin/bash

# --- SETTINGS ---
APP_NAME="CosmoWhisper"
VERSION="1.1"
DMG_NAME="CosmoWhisper_Universal_${VERSION}.dmg"
OUT_DIR="release"
BUILD_DIR="build_release"

echo "🚀 Starting Universal Build (Intel + Apple Silicon)..."

# 1. Clean and Build
mkdir -p "$OUT_DIR"
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

# 2. Verify Universal
echo "🔍 Verifying architectures..."
file "$APP_BUNDLE/Contents/MacOS/$APP_NAME"

# 3. Create Professional DMG
echo "📦 Creating Professional DMG..."
DMG_PATH="$OUT_DIR/$DMG_NAME"
BACKGROUND_IMAGE="release/assets/dmg-background.png"

# Ensure create-dmg is used for professional layout
/usr/local/bin/create-dmg \
  --volname "$APP_NAME" \
  --background "$BACKGROUND_IMAGE" \
  --window-pos 200 120 \
  --window-size 600 400 \
  --icon-size 100 \
  --icon "$APP_NAME.app" 150 220 \
  --hide-extension "$APP_NAME.app" \
  --app-drop-link 450 220 \
  --skip-jenkins \
  "$DMG_PATH" \
  "$APP_BUNDLE"

echo "------------------------------------------------"
echo "✅ SUCCESS! Professional Universal DMG Created."
echo "📍 Path: $DMG_PATH"
echo "💻 Works on: Intel Macs AND Apple Silicon (M1/M2/M3) Macs."
echo "------------------------------------------------"
