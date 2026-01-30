#!/bin/bash
# Script to install CosmoWhisper to /Applications
APP_PATH="/Users/louisdesouza/Documents/GitHub/CosmoWhisper-Native/CosmoWhisper/build_output/Build/Products/Debug/CosmoWhisper.app"

echo "📦 Installing CosmoWhisper to /Applications..."
sudo rm -rf /Applications/CosmoWhisper.app
sudo cp -R "$APP_PATH" /Applications/
sudo xattr -rd com.apple.quarantine /Applications/CosmoWhisper.app
echo "✅ Done! Opening CosmoWhisper..."
open /Applications/CosmoWhisper.app
