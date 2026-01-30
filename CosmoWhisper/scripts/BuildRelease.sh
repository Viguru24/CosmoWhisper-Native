#!/bin/bash

# CosmoWhisper Automated Release Script
# This script ensures the project is patched correctly before building and notarizing.

echo "🧹 Cleaning up old build artifacts..."
rm -rf build_release release

echo "🛠 Patching project file..."
python3 scripts/patch_project.py

if [ $? -ne 0 ]; then
    echo "❌ Project patching failed!"
    exit 1
fi

echo "🚀 Starting Notarization Build process..."
chmod +x scripts/NotarizeUniversal.sh
./scripts/NotarizeUniversal.sh

if [ $? -ne 0 ]; then
    echo "❌ Notarization process failed!"
    exit 1
fi

echo "✨ Release process complete!"
