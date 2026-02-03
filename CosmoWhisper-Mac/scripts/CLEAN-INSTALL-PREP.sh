#!/bin/bash
BUNDLE_ID="com.cosmowhisper.CosmoWhisper"
APP_NAME="CosmoWhisper.app"

echo "🧹 STARTING CLEANUP..."

# 1. Reset Permissions
echo "🔐 Resetting TCC Permissions for $BUNDLE_ID..."
tccutil reset Accessibility "$BUNDLE_ID"
tccutil reset Microphone "$BUNDLE_ID"
tccutil reset AppleEvents "$BUNDLE_ID"
# Also try resetting System Events just in case of lingering links
# tccutil reset AppleEvents "com.apple.systemevents" (Avoid unless necessary as it affects all apps)

# 2. Kill running processes
echo "💀 Killing running instances..."
pkill -f "CosmoWhisper"

# 3. Remove application files
echo "🗑️ Removing installed Application files..."
rm -rf "/Applications/$APP_NAME"
rm -rf "$HOME/Applications/$APP_NAME"

# 4. Clear derived data/cache specific to the app (optional but good for 'clean')
echo "🧹 Clearing local state..."
rm -rf "$HOME/Library/Application Support/CosmoWhisper"
rm -rf "$HOME/Library/Caches/$BUNDLE_ID"
rm -rf "$HOME/Library/Preferences/$BUNDLE_ID.plist"

echo "✅ CLEANUP COMPLETE."
echo "📂 Opening release folder..."
open release/
