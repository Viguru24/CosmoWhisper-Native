#!/bin/bash

# COSMOWHISPER PERMISSION REPAIR & FORCE TRIGGER
# This script resets TCC and attempts to force-trigger prompts.

BUNDLE_ID="com.cosmowhisper.CosmoWhisper"
APP_PATH="/Users/louisdesouza/Library/Developer/Xcode/DerivedData/CosmoWhisper-frekypqxjdjctxcgjrpyqbdfqsba/Build/Products/Debug/CosmoWhisper.app"

echo "🎯 Targeting: $APP_PATH"

# 1. Kill any existing instances
echo "🔪 Killing current instances..."
pkill -9 -f "CosmoWhisper" || true

# 2. Reset the TCC database for this bundle
echo "🛡️  Resetting permissions database..."
tccutil reset All $BUNDLE_ID || true
tccutil reset Accessibility $BUNDLE_ID || true
tccutil reset AppleEvents $BUNDLE_ID || true

# 3. Launch the app in the background
echo "🚀 Launching app..."
open "$APP_PATH"

# 4. Wait for it to warmup
sleep 3

# 5. Force-trigger Accessibility prompt
echo "📢 Forcing Accessibility request..."
# This AppleScript attempt to use the app to click something simple often triggers the OS prompt
osascript -e 'tell application "System Events" to set frontmost of process "CosmoWhisper" to true' || true

# 6. Force-trigger Automation prompt
echo "📢 Forcing Automation request..."
# Controlling System Events from the app's perspective is what triggers it.
# We'll use a shell level osascript aimed at the app.
osascript -e "tell application \"$APP_PATH\" to activate" || true
sleep 1
osascript -e 'tell application "System Events" to tell process "CosmoWhisper" to log "Force Trigger"' || true

# 7. Open the settings for the user
echo "⚙️  Opening Accessibility settings..."
open "x-apple.systempreferences:com.apple.preference.security?Privacy_Accessibility"

echo "✅ DONE."
echo "I've launched the app and opened the Accessibility settings."
echo "1. Look for CosmoWhisper in the list."
echo "2. If it's there but OFF, turn it ON."
echo "3. If it's NOT there, drag the app from the Finder window I just opened into the list."
open -R "$APP_PATH"
