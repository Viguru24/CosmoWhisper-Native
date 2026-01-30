#!/bin/bash

# COSMOWHISPER ULTIMATE FIXER
# Kill everything, wipe everything, build fresh.

echo "🔥 ULTIMATE FIXER STARTING..."

# 1. Aggressive Process Termination
echo "🔫 Killing all related processes..."
sudo pkill -9 CosmoWhisper 2>/dev/null
sudo pkill -9 xcodebuild 2>/dev/null
sudo pkill -f "SUPER-LAUNCH.sh" 2>/dev/null
sudo pkill -f "BuildAndRun.sh" 2>/dev/null
sudo pkill -f "ZOMBIE-KILLER.sh" 2>/dev/null

# 2. Complete Application Wipe
echo "🧹 Wiping /Applications/CosmoWhisper.app..."
sudo rm -rf /Applications/CosmoWhisper.app 2>/dev/null

# 3. Cache & Build Wipe
echo "🧹 Wiping build cache..."
rm -rf ./build_super_clean ./build_final_v16 ./build_final_v15 ./build_final_v14 2>/dev/null
rm -rf ~/Library/Developer/Xcode/DerivedData/CosmoWhisper-* 2>/dev/null

# 4. Fresh Compilation
echo "🚀 Compiling fresh code... (Please wait ~1 minute)"
xcodebuild -project CosmoWhisper.xcodeproj \
           -scheme CosmoWhisper \
           -configuration Debug \
           -derivedDataPath ./build_ultimate \
           clean build

if [ $? -ne 0 ]; then
    echo "❌ ERROR: Build failed. Please check the code for syntax errors."
    exit 1
fi

# 5. Final Installation
echo "📦 Installing fresh version to /Applications..."
sudo cp -R ./build_ultimate/Build/Products/Debug/CosmoWhisper.app /Applications/
sudo chown -R $(whoami) /Applications/CosmoWhisper.app
sudo xattr -rd com.apple.quarantine /Applications/CosmoWhisper.app 2>/dev/null
sudo xattr -cr /Applications/CosmoWhisper.app 2>/dev/null
sudo codesign --force --deep --sign - /Applications/CosmoWhisper.app

# 6. Launch
echo "🌈 Launching..."
open /Applications/CosmoWhisper.app

echo "---------------------------------------------------"
echo "✅ ULTIMATE FIX COMPLETE."
echo "The Dev Skip button should now be visible."
echo "---------------------------------------------------"
