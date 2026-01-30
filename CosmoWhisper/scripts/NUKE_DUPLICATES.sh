#!/bin/bash

# Define the blessed path
BLESSED="/Applications/CosmoWhisper.app"

echo "🔍 Searching for duplicate CosmoWhisper apps..."
# Find all CosmoWhisper.app directories
# Exclude the blessed path
# Use -print0 to handle spaces safely

# We'll use a temporary file to list them first
listing_file=$(mktemp)

find "$HOME" -name "CosmoWhisper.app" -type d -prune ! -path "$BLESSED" > "$listing_file"

count=$(wc -l < "$listing_file" | xargs)

if [ "$count" -eq "0" ]; then
    echo "✅ No duplicates found. You are clean!"
    rm "$listing_file"
    exit 0
fi

echo "⚠️  Found $count duplicate copies causing permission conflicts:"
cat "$listing_file"
echo "---------------------------------------------------"
echo "🧨  Nuking duplicates to fix 'Ghost Permission' bug..."

while IFS= read -r app_path; do
    echo "🗑  Deleting: $app_path"
    rm -rf "$app_path"
done < "$listing_file"

rm "$listing_file"

echo "---------------------------------------------------"
echo "✨ Duplicates removed."
echo "👉 Now, go to System Settings -> Privacy & Security -> Accessibility"
echo "👉 REMOVE (using the minus button) any 'CosmoWhisper' entries."
echo "👉 Re-open /Applications/CosmoWhisper.app and add it fresh."
