#!/bin/bash
# DEV RUN - No Sudo, Runs from Build Folder

# 1. Kill old
pkill -9 CosmoWhisper || true

# 1.5 Clean Gossip Log (Diagnostic Log) AND Recent Activity AND Keychain
rm -f "$HOME/Library/Application Support/CosmoWhisper/Logs/app.log"
# defaults delete com.cosmowhisper.CosmoWhisper recentTranscriptions || true
# defaults delete com.cosmowhisper.CosmoWhisper transcriptionCount || true
# security delete-generic-password -s "com.cosmowhisper.api" -a "groq" 2>/dev/null || true

# 2. Build
echo "🔨 Building..."
xcodebuild -project CosmoWhisper.xcodeproj -scheme CosmoWhisper -configuration Debug -derivedDataPath ./build_gentle build -quiet

if [ $? -ne 0 ]; then
    echo "❌ Build failed."
    exit 1
fi

APP_PATH="./build_gentle/Build/Products/Debug/CosmoWhisper.app"
INSTALL_PATH="/Applications/CosmoWhisper.app"

# 3. Sign (Relaxed)
echo "✍️  Signing..."
IDENTITY="Developer ID Application: Louis de Souza (9AAQ3L289K)"
ENTITLEMENTS="./CosmoWhisper/CosmoWhisper.entitlements"
codesign --force --deep --sign "$IDENTITY" --entitlements "$ENTITLEMENTS" "$APP_PATH"

# 4. Copy to /Applications to maintain stable Accessibility/TCC state
echo "🚚 Installing to /Applications..."
rm -rf "$INSTALL_PATH"
cp -R "$APP_PATH" "$INSTALL_PATH"

# 5. Remove Quarantine
xattr -rd com.apple.quarantine "$INSTALL_PATH" 2>/dev/null || true
chmod -R +x "$INSTALL_PATH"

# 6. Run
echo "🚀 Launching from /Applications..."
open "$INSTALL_PATH"
