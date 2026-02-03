#!/bin/bash

echo "🧟 AGGRESSIVE ZOMBIE KILLER ACTIVATED..."

# 1. Kill app and build tools
echo "🔫 Terminating app and build processes..."
sudo pkill -9 CosmoWhisper 2>/dev/null
sudo pkill -9 xcodebuild 2>/dev/null
sudo pkill -9 -f "CosmoWhisper" 2>/dev/null

# 2. Kill all our custom scripts that might be hanging
echo "🔫 Terminating custom scripts..."
sudo pkill -9 -f "SUPER-LAUNCH.sh" 2>/dev/null
sudo pkill -9 -f "BuildAndRun.sh" 2>/dev/null
sudo pkill -9 -f "RESET-AND-RUN.sh" 2>/dev/null

# 3. Clean up the /Applications folder specifically
echo "🧹 Removing potential old app from /Applications..."
sudo rm -rf /Applications/CosmoWhisper.app 2>/dev/null

# 4. Wipe build artifacts to force a re-link
echo "🧹 Wiping build folders..."
rm -rf ./build_super_clean ./build_final_v16 ./build_final_v15 2>/dev/null

# 5. Clear derived data to be 100% sure
echo "🧹 Clearing Xcode DerivedData..."
rm -rf ~/Library/Developer/Xcode/DerivedData/CosmoWhisper-* 2>/dev/null

echo "✅ ALL ZOMBIES PURGED."
echo "You are now on a clean slate. Run ./SUPER-LAUNCH.sh to build fresh."
