#!/bin/bash
echo "🧹 Cleaning TCC Database for CosmoWhisper..."
tccutil reset Accessibility com.cosmowhisper.CosmoWhisper
tccutil reset AppleEvents com.cosmowhisper.CosmoWhisper
tccutil reset Microphone com.cosmowhisper.CosmoWhisper

echo "✅ Permissions reset."
echo "🚀 Launching App - Please Re-Grant Permissions when prompted!"

# Launch the local release build
open ./build_local_release/Build/Products/Release/CosmoWhisper.app
