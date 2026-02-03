#!/bin/bash

echo "=================================================="
echo "☢️  FULL SYSTEM RESET & RELEASE PROTOCOL"
echo "=================================================="

# 1. Kill Zombies
echo "💀 Killing existing instances..."
pkill -9 CosmoWhisper || true
pkill -9 xcodebuild || true
pkill -9 codesign || true

# 2. Deep Clean
echo "🧹 Nuking build artifacts..."
rm -rf ./build_release
rm -rf ./build_local
rm -rf ./release
rm -rf ~/Library/Developer/Xcode/DerivedData/CosmoWhisper-*

# 3. Reset Permissions
echo "🛡️  Resetting TCC Permissions..."
tccutil reset All com.cosmowhisper.CosmoWhisper || true
tccutil reset Accessibility com.cosmowhisper.CosmoWhisper || true
tccutil reset Microphone com.cosmowhisper.CosmoWhisper || true

# 4. Run Notarization
echo "🚀 Starting Notarization Pipeline..."
./scripts/NotarizeUniversal.sh

echo "=================================================="
echo "✅ SEQUENCE COMPLETE"
echo "=================================================="
