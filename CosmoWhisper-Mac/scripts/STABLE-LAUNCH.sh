#!/bin/bash

# COSMOWHISPER ULTIMATE FIX
# 1. Kills app
# 2. Resets permissions for the specific local build path AND bundle ID
# 3. Copies to /Applications to stabilize path
# 4. Launches

echo "🔧 Starting Ultimate Fix..."

# Kill
pkill -9 CosmoWhisper || true

# App Variables
SOURCE_APP="./build_fix/Build/Products/Debug/CosmoWhisper.app"
DEST_APP="/Applications/CosmoWhisper.app"
BUNDLE_ID="com.cosmowhisper.CosmoWhisper"

# Clean
echo "🧹 Cleaning previous installs..."
rm -rf "$DEST_APP"

# Copy
if [ -d "$SOURCE_APP" ]; then
    echo "📦 Moving to /Applications (Stable Path)..."
    cp -R "$SOURCE_APP" /Applications/
else
    echo "❌ Source app not found. Please run GENTLE-BUILD first."
    exit 1
fi

# Reset Permissions (Try local user reset first)
echo "🛡️  Resetting permissions..."
tccutil reset Accessibility "$BUNDLE_ID" || true
tccutil reset AppleEvents "$BUNDLE_ID" || true

# Clean Quarantine attributes
echo "✨ Removing quarantine bits..."
xattr -rd com.apple.quarantine "$DEST_APP" 2>/dev/null || true

# Re-sign in place
echo "✍️  Re-signing in /Applications..."
codesign --force --deep --sign - "$DEST_APP"

# Launch
echo "🚀 Launching from /Applications..."
open "$DEST_APP"

echo "✅ Done."
echo "---------------------------------------------------"
echo "IMPORTANT: The app is now in your Applications folder."
echo "1. Go to System Settings > Privacy & Security > Accessibility"
echo "2. If 'CosmoWhisper' is there, toggle it OFF and ON."
echo "3. If it's NOT there, click (+) and select it from Applications."
echo "---------------------------------------------------"
sleep 1
open "x-apple.systempreferences:com.apple.preference.security?Privacy_Accessibility"
