#!/bin/bash

echo "☢️  INITIATING NUCLEAR RESET..."

# 1. Kill everything
pkill -9 CosmoWhisper 2>/dev/null
pkill -9 xcodebuild 2>/dev/null

# 2. Wipe Permissions (The critical step)
echo "🧹 Wiping Permission Database..."
tccutil reset Accessibility com.cosmowhisper.CosmoWhisper
tccutil reset AppleEvents com.cosmowhisper.CosmoWhisper

# 3. Clean Install
echo "🚚 Re-installing App..."
sudo rm -rf /Applications/CosmoWhisper.app
# improved: verify source exists first
if [ -d "/Users/louisdesouza/Documents/GitHub/CosmoWhisper-Native/CosmoWhisper/build_test/Build/Products/Debug/CosmoWhisper.app" ]; then
    sudo cp -R /Users/louisdesouza/Documents/GitHub/CosmoWhisper-Native/CosmoWhisper/build_test/Build/Products/Debug/CosmoWhisper.app /Applications/
else
    # Fallback to local build if test build missing
    echo "⚠️ Test build missing, using local build folder..."
    sudo cp -R /Users/louisdesouza/Documents/GitHub/CosmoWhisper-Native/CosmoWhisper/build_final/Build/Products/Debug/CosmoWhisper.app /Applications/
fi

# 4. Fix Ownership & Quarantine
sudo chown -R $(whoami) /Applications/CosmoWhisper.app
xattr -rd com.apple.quarantine /Applications/CosmoWhisper.app 2>/dev/null

# 5. Launch
echo "🚀 Launching..."
open /Applications/CosmoWhisper.app

echo "---------------------------------------------------"
echo "✅ RESET COMPLETE."
echo "ACTION: Go to System Settings -> Accessibility."
echo "If CosmoWhisper is there: Toggle it OFF then ON."
echo "If not there: It will ask you to add it. Add it and turn ON."
echo "---------------------------------------------------"
