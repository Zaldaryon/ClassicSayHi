#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

echo "Building ClassicSayHi..."
dotnet build ClassicSayHi.csproj -c Release

VERSION="$(grep -oP '"version":\s*"\K[^"]+' modinfo.json)"
RELEASE_DIR="$SCRIPT_DIR/releases"
mkdir -p "$RELEASE_DIR"

STAGE_DIR="$(mktemp -d)"
trap 'rm -rf "$STAGE_DIR"' EXIT

cp "$SCRIPT_DIR/bin/Release/ClassicSayHi.dll" "$STAGE_DIR/"
cp "$SCRIPT_DIR/modinfo.json" "$STAGE_DIR/"
cp "$SCRIPT_DIR/modicon.png" "$STAGE_DIR/"
cp "$SCRIPT_DIR/README.md" "$STAGE_DIR/"
cp -r "$SCRIPT_DIR/assets" "$STAGE_DIR/"

ZIP_PATH="$RELEASE_DIR/classicsayhi_v${VERSION}.zip"
rm -f "$ZIP_PATH"

(cd "$STAGE_DIR" && zip -r "$ZIP_PATH" .)

echo "Package created at: $ZIP_PATH"
