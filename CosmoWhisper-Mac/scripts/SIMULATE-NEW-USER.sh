#!/bin/bash

# ==============================================================================
# CosmoWhisper NEW USER SIMULATION
# ==============================================================================
# This script prepares your Mac to experience CosmoWhisper as if you were a
# brand new user downloading it for the first time.
# ==============================================================================

BUNDLE_ID="com.cosmowhisper.CosmoWhisper"
APP_NAME="CosmoWhisper.app"
DMG_NAME="CosmoWhisper_Universal_2.1.0_Notarized.dmg"

echo "🛑 STEP 1: EXITING COSMOWHISPER..."
pkill -9 -f "CosmoWhisper" || true

echo "🗑️  STEP 2: UNINSTALLING PREVIOUS COPIES..."
rm -rf "/Applications/$APP_NAME" || true
rm -rf "$HOME/Applications/$APP_NAME" || true

echo "🔐 STEP 3: PURGING PRIVACY PERMISSIONS..."
tccutil reset Accessibility "$BUNDLE_ID" 2>/dev/null || true
tccutil reset Microphone "$BUNDLE_ID" 2>/dev/null || true
tccutil reset AppleEvents "$BUNDLE_ID" 2>/dev/null || true
tccutil reset ScreenCapture "$BUNDLE_ID" 2>/dev/null || true
tccutil reset All "$BUNDLE_ID" 2>/dev/null || true

echo "🧹 STEP 4: WIPING LOCAL DATA & CACHES..."
rm -rf "$HOME/Library/Application Support/CosmoWhisper"
rm -rf "$HOME/Library/Preferences/$BUNDLE_ID.plist"
rm -rf "$HOME/Library/Caches/$BUNDLE_ID"

echo "💀 STEP 5: CLEARING SYSTEM EVENTS CACHE..."
pkill -9 "System Events" || true

echo "----------------------------------------------------------------"
echo "✅ PREPARATION COMPLETE."
echo "🎁 SIMULATING DOWNLOAD..."
echo "----------------------------------------------------------------"

if [ -f "release/$DMG_NAME" ]; then
    echo "📂 Opening Latest Distribution DMG: $DMG_NAME"
    open "release/$DMG_NAME"
else
    echo "❌ ERROR: No notarized DMG found in release/ folder."
    echo "Please run ./scripts/BuildRelease.sh first."
fi
