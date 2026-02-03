#!/bin/bash

# COSMOWHISPER HYPER-REPAIR (SUDO EDITION)
# Uses provided password to force-reset macOS permissions.

PASS="sugmad24"
BUNDLE_ID="com.cosmowhisper.CosmoWhisper"
APP_PATH="./build_local/Build/Products/Debug/CosmoWhisper.app"

echo "🔥 STARTING HYPER-REPAIR WITH SUDO..."

# 1. Kill everything
echo "🔪 Killing all instances..."
pkill -9 CosmoWhisper || true

# 2. Force Purge TCC with sudo (just in case)
echo "🛡️  Resetting TCC database..."
echo "$PASS" | sudo -S tccutil reset All $BUNDLE_ID || true
echo "$PASS" | sudo -S tccutil reset Accessibility $BUNDLE_ID || true
echo "$PASS" | sudo -S tccutil reset AppleEvents $BUNDLE_ID || true

# 3. Attributes and Permissions
if [ -d "$APP_PATH" ]; then
    echo "🧹 Deep cleaning app attributes..."
    echo "$PASS" | sudo -S chown -R $(whoami) "$APP_PATH"
    echo "$PASS" | sudo -S xattr -cr "$APP_PATH"
    echo "$PASS" | sudo -S xattr -rd com.apple.quarantine "$APP_PATH" 2>/dev/null || true
fi

# 4. Re-sign
echo "🔐 Ad-hoc signing..."
codesign --force --deep --sign - "$APP_PATH"

# 5. Launch
echo "🚀 Launching..."
open "$APP_PATH"

# 6. Instructions
echo "-------------------------------------------------------"
echo "✅ HYPER-REPAIR COMPLETE."
echo "-------------------------------------------------------"
echo "I have used your password to force-reset everything."
echo "1. The app is launching now."
echo "2. Check the Dashboard."
echo "3. If Accessibility is still RED:"
echo "   - Click 'Request Access'"
echo "   - IN SYSTEM SETTINGS: Find CosmoWhisper."
echo "   - TOGGLE it OFF and back ON again (Very Important!)"
echo "-------------------------------------------------------"

sleep 1
open "x-apple.systempreferences:com.apple.preference.security?Privacy_Accessibility"
