#!/bin/bash
echo "🚀 Building CosmoWhisper..."
pkill -9 CosmoWhisper
xcodebuild -project CosmoWhisper.xcodeproj -scheme CosmoWhisper -configuration Debug -derivedDataPath ./build_final_v16 build

if [ $? -ne 0 ]; then
    echo "❌ Build Failed"
    exit 1
fi

echo "✅ Build Success. Installing..."
echo "Enter password if prompted:"
sudo rm -rf /Applications/CosmoWhisper.app
sudo cp -R ./build_final_v16/Build/Products/Debug/CosmoWhisper.app /Applications/
sudo chown -R $(whoami) /Applications/CosmoWhisper.app
xattr -rd com.apple.quarantine /Applications/CosmoWhisper.app

echo "🚀 Launching..."
open /Applications/CosmoWhisper.app
