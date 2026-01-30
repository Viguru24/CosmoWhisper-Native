#!/bin/bash

# GENTLE BUILD - No Passwords, No Resets
# 1. Kill old instances
pkill -9 CosmoWhisper || true
rm -f "$HOME/Library/Application Support/CosmoWhisper/Logs/app.log"

# 2. Build local
echo "🔨 Building..."
xcodebuild -project CosmoWhisper.xcodeproj -scheme CosmoWhisper -configuration Debug -derivedDataPath ./build_gentle build -quiet

if [ $? -ne 0 ]; then
    echo "❌ Build failed."
    exit 1
fi

BUILD_PATH="./build_gentle/Build/Products/Debug/CosmoWhisper.app"
INSTALL_PATH="/Applications/CosmoWhisper.app"

# 3. Sign with Real Developer ID (Relaxed Runtime)
echo "✍️  Signing with Developer ID (Relaxed)..."
IDENTITY="Developer ID Application: Louis de Souza (9AAQ3L289K)"
ENTITLEMENTS="./CosmoWhisper/CosmoWhisper.entitlements"
codesign --force --deep --sign "$IDENTITY" --entitlements "$ENTITLEMENTS" "$BUILD_PATH"

# 4. Install to /Applications (Stable Path)
echo "📦 Installing to /Applications..."
echo "🔑 You may be asked for your password to overwrite the old app..."

# Use sudo to force overwrite and ownership (fixes permission drift)
sudo rm -rf "$INSTALL_PATH"
sudo cp -R "$BUILD_PATH" "$INSTALL_PATH"
sudo chown -R $(whoami) "$INSTALL_PATH"

# 5. Remove quarantine (just in case)
xattr -rd com.apple.quarantine "$INSTALL_PATH" 2>/dev/null || true

# 6. Launch
echo "🚀 Launching..."
open "$INSTALL_PATH"
