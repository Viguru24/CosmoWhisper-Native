#!/bin/bash

echo "🔐 FINALIZE INSTALLATION: Locking in Permissions"

# 1. Kill any running instances
pkill -9 CosmoWhisper 2>/dev/null

# 2. Force remove the old signature/attributes (This removes the "quarantine" flag completely)
echo "🧹 Cleaning app attributes..."
sudo xattr -cr /Applications/CosmoWhisper.app

# 3. Apply a deep ad-hoc signature
# This tells macOS: "This app is valid and shouldn't be treated as a new stranger every time"
echo "✍️  Signing the app..."
sudo codesign --force --deep --sign - /Applications/CosmoWhisper.app

# 4. Verify the signature
echo "✅ verifying signature..."
codesign -dv --verbose=4 /Applications/CosmoWhisper.app

# 5. One last Ownership fix
sudo chown -R $(whoami) /Applications/CosmoWhisper.app

echo "---------------------------------------------------"
echo "🎉 DONE! The app is now permanently signed."
echo "You can now reboot and launch it normally."
echo "ACTION: Open the app -> Toggle Accessibility OFF/ON one last time -> You are free."
echo "---------------------------------------------------"
open /Applications/CosmoWhisper.app
