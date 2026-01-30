#!/bin/bash
# DEV-PERM-FIX - Uses the Real Developer ID for long-term permission persistence

PASS="sugmad24"
IDENTITY="Developer ID Application: Louis de Souza (9AAQ3L289K)"
BUNDLE_ID="com.cosmowhisper.CosmoWhisper"
ENTITLEMENTS="./CosmoWhisper/CosmoWhisper.entitlements"

echo "🛑 Stopping app..."
pkill -9 CosmoWhisper || true

# echo "🧹 Clearing TCC baggage (Old Ad-hoc entries)..."
# echo "$PASS" | sudo -S tccutil reset All $BUNDLE_ID || true

echo "🔨 Building fresh binary..."
xcodebuild -project CosmoWhisper.xcodeproj -scheme CosmoWhisper -configuration Debug -derivedDataPath ./build_perm_fix clean build -quiet

if [ $? -ne 0 ]; then
    echo "❌ Build failed."
    exit 1
fi

APP_SOURCE="./build_perm_fix/Build/Products/Debug/CosmoWhisper.app"
APP_DEST="/Applications/CosmoWhisper.app"

echo "✍️  Signing with Developer ID (The Fix)..."
codesign --force --deep --sign "$IDENTITY" --entitlements "$ENTITLEMENTS" "$APP_SOURCE"

echo "📦 Installing to /Applications..."
echo "$PASS" | sudo -S rm -rf "$APP_DEST"
echo "$PASS" | sudo -S cp -R "$APP_SOURCE" "$APP_DEST"
echo "$PASS" | sudo -S chown -R $(whoami) "$APP_DEST"

echo "✨ Cleaning attributes..."
xattr -rd com.apple.quarantine "$APP_DEST" 2>/dev/null || true

echo "🚀 Launching..."
echo "$PASS" | sudo -S open "$APP_DEST"

echo "✅ SUCCESS: The app is now signed with your Developer ID."
echo "👉 ONE LAST TIME: Go to Accessibility and toggle CosmoWhisper ON."
echo "👉 After this, it should STAY on even after future builds."
open "x-apple.systempreferences:com.apple.preference.security?Privacy_Accessibility"
