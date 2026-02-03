#!/bin/bash
PASS="sugmad24"

echo "🔐 Resetting permissions for CosmoWhisper..."
echo "$PASS" | sudo -S tccutil reset Accessibility com.cosmowhisper.CosmoWhisper
echo "$PASS" | sudo -S tccutil reset AppleEvents com.cosmowhisper.CosmoWhisper

echo "🚀 Building fresh version..."
xcodebuild -project CosmoWhisper.xcodeproj -scheme CosmoWhisper -configuration Debug -derivedDataPath ./build_fix clean build > build_log.txt 2>&1

if [ $? -ne 0 ]; then
    echo "❌ Build failed. Check build_log.txt"
    exit 1
fi

APP_PATH="./build_fix/Build/Products/Debug/CosmoWhisper.app"

echo "🔐 Signing..."
choicesign --force --deep --sign - "$APP_PATH" 2>/dev/null || codesign --force --deep --sign - "$APP_PATH"

echo "📦 Installing to /Applications..."
echo "$PASS" | sudo -S rm -rf /Applications/CosmoWhisper.app
echo "$PASS" | sudo -S cp -R "$APP_PATH" /Applications/

echo "🛠 Trusting..."
echo "$PASS" | sudo -S chown -R $(whoami) /Applications/CosmoWhisper.app
echo "$PASS" | sudo -S xattr -rd com.apple.quarantine /Applications/CosmoWhisper.app 2>/dev/null

echo "🚀 Launching..."
open /Applications/CosmoWhisper.app
echo "✅ Done! Please check System Settings to re-enable permissions if prompted."
