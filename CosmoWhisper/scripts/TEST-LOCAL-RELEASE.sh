#!/bin/bash
echo '🚀 Building RELEASE version (Local)...'
# Note: NOT setting Notarization flags, just Release config
xcodebuild -project CosmoWhisper.xcodeproj \
           -scheme CosmoWhisper \
           -configuration Release \
           -derivedDataPath ./build_local_release \
           clean build > build_release_local.log 2>&1

if [ $? -ne 0 ]; then
    echo '❌ Build failed'
    tail -n 20 build_release_local.log
    exit 1
fi

echo '🚀 Launching RELEASE version...'
open ./build_local_release/Build/Products/Release/CosmoWhisper.app
