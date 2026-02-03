#!/bin/bash

# AUTO-AUTH ENABLED
PASSWORD="sugmad24"

mysudo() {
    echo "$PASSWORD" | sudo -S "$@"
}

# 0. DEEP CLEANUP
echo "🧹 Terminating all stuck build processes..."
mysudo killall xcodebuild 2>/dev/null
mysudo killall CosmoWhisper 2>/dev/null
mysudo rm -rf build_release
# Give it a second to settle
sleep 1

# 1. Kill any running instances
echo "🛑 Stopping CosmoWhisper..."
killall CosmoWhisper 2>/dev/null

# 2. Reset TCC database
echo "🧹 Clearing macOS permission cache..."
mysudo tccutil reset Accessibility com.cosmowhisper.CosmoWhisper
mysudo tccutil reset AppleEvents com.cosmowhisper.CosmoWhisper
mysudo tccutil reset ScreenCapture com.cosmowhisper.CosmoWhisper

# 3. Build a fresh version
echo "📦 Building NEW version with UI click fixes..."
xcodebuild -project CosmoWhisper.xcodeproj -scheme CosmoWhisper -configuration Debug -derivedDataPath ./build_release build > build_log.txt 2>&1

if [ $? -ne 0 ]; then
    echo "❌ Build failed. Check build_log.txt for details."
    exit 1
fi

# 4. Find the app
APP_PATH="./build_release/Build/Products/Debug/CosmoWhisper.app"

if [ ! -d "$APP_PATH" ] || [ ! -f "$APP_PATH/Contents/MacOS/CosmoWhisper" ]; then
    echo "❌ App bundle missing at $APP_PATH"
    exit 1
fi

# 5. Ad-hoc sign
echo "🔐 Ad-hoc signing..."
codesign --force --deep --sign - "$APP_PATH"

# 6. Move to /Applications
echo "🚚 Installing to /Applications..."
mysudo rm -rf /Applications/CosmoWhisper.app
mysudo cp -R "$APP_PATH" /Applications/

# 7. Permissions & Ownership
echo "🛠 Finalizing system trust..."
mysudo chown -R $(whoami) /Applications/CosmoWhisper.app
mysudo chmod -R +x /Applications/CosmoWhisper.app
mysudo xattr -rd com.apple.quarantine /Applications/CosmoWhisper.app 2>/dev/null

# 8. Open it
echo "🚀 Launching stabilized version..."
open /Applications/CosmoWhisper.app

if [ $? -ne 0 ]; then
    echo "⚠️ Launch failed. Right-click /Applications/CosmoWhisper.app and select 'Open'."
else
    echo "✅ DONE!"
    echo "--------------------------------------------------"
    echo "FIXES APPLIED:"
    echo "1. Widget clicking is now raw-hardware powered (no more missed clicks)."
    echo "2. Stars no longer block clicks."
    echo "3. Stuck background scripts have been cleared."

    echo "--------------------------------------------------"
    echo "ACTION: Please go to System Settings -> Accessibility and toggle it ON."
fi
