#!/bin/bash

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"

PACKAGE_NAME="capscroll"
PACKAGE_FILE="$PROJECT_DIR/dist/${PACKAGE_NAME}_1.0.0_amd64.deb"

echo "================================================"
echo " Installing CapScroll"
echo "================================================"

if [ ! -f "$PACKAGE_FILE" ]; then
    echo "Error: Debian package not found:"
    echo "  $PACKAGE_FILE"
    echo
    echo "Build the package first:"
    echo "  ./scripts/build.sh"
    exit 1
fi

echo
echo "Installing:"
echo "  $PACKAGE_FILE"

sudo apt install -y "$PACKAGE_FILE"

echo
echo "================================================"
echo " CapScroll installed successfully"
echo "================================================"
echo
echo "Run with:"
echo "  capscroll"
echo
