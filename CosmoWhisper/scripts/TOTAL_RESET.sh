#!/bin/bash

echo "=================================================="
echo "🧟 KILLING ZOMBIES & NUKING EVERYTHING"
echo "=================================================="

# 1. Kill Processes
echo "💀 Killing processes..."
pkill -9 CosmoWhisper || true
pkill -9 xcodebuild || true
pkill -9 find || true # Stop the slow searches

# 2. Nuke Derived Data & Build Artifacts
echo "💥 Nuking DerivedData & Build Caches..."
rm -rf ~/Library/Developer/Xcode/DerivedData/CosmoWhisper-*
rm -rf ./build
rm -rf ./build_fix

# 3. Nuke Known Duplicate Locations (Fast Delete)
echo "🗑️  Deleting duplicates from likely locations..."
rm -rf ~/Desktop/release
rm -rf ~/Desktop/CosmoWhisper.app
rm -rf ~/Downloads/CosmoWhisper.app
rm -rf ~/Documents/CosmoWhisper.app
# Remove the one in current dir if it exists (except source)
find . -maxdepth 3 -name "CosmoWhisper.app" -type d -exec rm -rf {} +

# 4. Clean Permissions
echo "🛡️  Resetting Privacy Database..."
tccutil reset All com.cosmowhisper.CosmoWhisper || true

# 5. Clean /Applications (Prepare for fresh install)
echo "🧹 Cleaning /Applications..."
rm -rf /Applications/CosmoWhisper.app

# 6. Rebuild & Reinstall
echo "🏗️  Starting Fresh Build..."
./scripts/FULL-FORCE-FIX.sh
