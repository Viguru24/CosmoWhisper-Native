#!/bin/bash

echo "🛑 Killing CosmoWhisper..."
pkill -9 CosmoWhisper || true

APP_PATH="/Applications/CosmoWhisper.app"
BUNDLE_ID="com.cosmowhisper.CosmoWhisper"

echo "🧹 Clearing privacy database for $BUNDLE_ID..."
# Reset Accessibility
tccutil reset Accessibility "$BUNDLE_ID" 2>/dev/null || true
# Reset Automation
tccutil reset AppleEvents "$BUNDLE_ID" 2>/dev/null || true
# Reset Microphone
tccutil reset Microphone "$BUNDLE_ID" 2>/dev/null || true
# Reset Screen Recording
tccutil reset ScreenCapture "$BUNDLE_ID" 2>/dev/null || true

echo "🛡️  Removing Quarantine..."
xattr -rd com.apple.quarantine "$APP_PATH" 2>/dev/null || true

echo "✅ Permissions Reset."
echo "----------------------------------------------------------------"
echo "👉 I am opening the Privacy Settings for you."
echo "👉 You MUST see 'CosmoWhisper' appear (or add it manually)."
echo "👉 Toggle it OFF and ON if it is already there."
echo "----------------------------------------------------------------"

open "x-apple.systempreferences:com.apple.preference.security?Privacy_Accessibility"
sleep 2
open "$APP_PATH"
