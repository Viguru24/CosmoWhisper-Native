#!/bin/bash

APP_BUNDLE_ID="com.cosmowhisper.CosmoWhisper"

echo "🛠️  Resetting Privacy Permissions for $APP_BUNDLE_ID..."

# 1. Reset Accessibility (The main culprit)
echo "Resetting Accessibility..."
tccutil reset Accessibility "$APP_BUNDLE_ID"

# 2. Reset Automation (Often gets stuck too)
echo "Resetting Automation..."
tccutil reset AppleEvents "$APP_BUNDLE_ID"

# 3. Reset Microphone (Just in case)
echo "Resetting Microphone..."
tccutil reset Microphone "$APP_BUNDLE_ID"

echo "✅ Permissions reset."
echo ""
echo "👉 Now launch CosmoWhisper.app and it should ask for permissions again."
