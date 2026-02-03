#!/bin/bash
LOGO_PATH="/Users/louisdesouza/.gemini/antigravity/brain/a196578f-25fe-496d-bc2a-df7a9c6198d6/cosmo_whisper_logo_2_1769621340724.png"
ICONSET_DIR="/Users/louisdesouza/Documents/GitHub/CosmoWhisper-Native/CosmoWhisper/CosmoWhisper/Assets.xcassets/AppIcon.appiconset"

mkdir -p "$ICONSET_DIR"

# Generate various sizes
sips -s format png -z 16 16     "$LOGO_PATH" --out "$ICONSET_DIR/icon_16x16.png"
sips -s format png -z 32 32     "$LOGO_PATH" --out "$ICONSET_DIR/icon_16x16@2x.png"
sips -s format png -z 32 32     "$LOGO_PATH" --out "$ICONSET_DIR/icon_32x32.png"
sips -s format png -z 64 64     "$LOGO_PATH" --out "$ICONSET_DIR/icon_32x32@2x.png"
sips -s format png -z 128 128   "$LOGO_PATH" --out "$ICONSET_DIR/icon_128x128.png"
sips -s format png -z 256 256   "$LOGO_PATH" --out "$ICONSET_DIR/icon_128x128@2x.png"
sips -s format png -z 256 256   "$LOGO_PATH" --out "$ICONSET_DIR/icon_256x256.png"
sips -s format png -z 512 512   "$LOGO_PATH" --out "$ICONSET_DIR/icon_256x256@2x.png"
sips -s format png -z 512 512   "$LOGO_PATH" --out "$ICONSET_DIR/icon_512x512.png"
sips -s format png -z 1024 1024 "$LOGO_PATH" --out "$ICONSET_DIR/icon_512x512@2x.png"

# Create Contents.json
cat <<EOF > "$ICONSET_DIR/Contents.json"
{
  "images" : [
    { "idiom" : "mac", "scale" : "1x", "size" : "16x16", "filename" : "icon_16x16.png" },
    { "idiom" : "mac", "scale" : "2x", "size" : "16x16", "filename" : "icon_16x16@2x.png" },
    { "idiom" : "mac", "scale" : "1x", "size" : "32x32", "filename" : "icon_32x32.png" },
    { "idiom" : "mac", "scale" : "2x", "size" : "32x32", "filename" : "icon_32x32@2x.png" },
    { "idiom" : "mac", "scale" : "1x", "size" : "128x128", "filename" : "icon_128x128.png" },
    { "idiom" : "mac", "scale" : "2x", "size" : "128x128", "filename" : "icon_128x128@2x.png" },
    { "idiom" : "mac", "scale" : "1x", "size" : "256x256", "filename" : "icon_256x256.png" },
    { "idiom" : "mac", "scale" : "2x", "size" : "256x256", "filename" : "icon_256x256@2x.png" },
    { "idiom" : "mac", "scale" : "1x", "size" : "512x512", "filename" : "icon_512x512.png" },
    { "idiom" : "mac", "scale" : "2x", "size" : "512x512", "filename" : "icon_512x512@2x.png" }
  ],
  "info" : {
    "author" : "xcode",
    "version" : 1
  }
}
EOF
