#!/bin/bash

# ==============================================================================
# CosmoWhisper NUKE PERMISSIONS & STATE
# ==============================================================================
# This script forcefully clears all privacy permissions and local application
# state to resolve persistent crashes and permission glitches.
# ==============================================================================

BUNDLE_ID="com.cosmowhisper.CosmoWhisper"
APP_NAME="CosmoWhisper.app"

echo "🛑 SHUTTING DOWN COSMOWHISPER..."
pkill -9 -f "CosmoWhisper" || true

echo "🔐 NUKING TCC PRIVACY DATABASE..."
# Reset all relevant privacy categories
tccutil reset Accessibility "$BUNDLE_ID" 2>/dev/null || true
tccutil reset Microphone "$BUNDLE_ID" 2>/dev/null || true
tccutil reset AppleEvents "$BUNDLE_ID" 2>/dev/null || true
tccutil reset ScreenCapture "$BUNDLE_ID" 2>/dev/null || true
tccutil reset All "$BUNDLE_ID" 2>/dev/null || true

echo "🧹 CLEARING LOCAL APPLICATION STATE..."
# Remove Application Support (Logs, persistent assets)
rm -rf "$HOME/Library/Application Support/CosmoWhisper"

# Remove Preferences (UserDefaults)
rm -rf "$HOME/Library/Preferences/$BUNDLE_ID.plist"

# Remove Caches
rm -rf "$HOME/Library/Caches/$BUNDLE_ID"

echo "💀 KILLING SYSTEM EVENTS (CLEAN SLATE)..."
pkill -9 "System Events" || true

echo "✅ DEEP RESET COMPLETE."
echo "----------------------------------------------------------------"
echo "👉 CosmoWhisper will now request all permissions again."
echo "👉 Ensure you grant Accessibility, Microphone, and Automation."
echo "----------------------------------------------------------------"

# Attempt to relaunch if it's in /Applications
if [ -d "/Applications/$APP_NAME" ]; then
    echo "🚀 Relaunching from /Applications..."
    open "/Applications/$APP_NAME"
fi
