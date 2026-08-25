#!/bin/bash

set -e

PACKAGE_NAME="capscroll"

echo "================================================"
echo " Uninstalling CapScroll"
echo "================================================"

if ! dpkg-query -W -f='${Status}' "$PACKAGE_NAME" 2>/dev/null |
    grep -q "install ok installed"; then

    echo
    echo "CapScroll is not currently installed."
    exit 0
fi

echo
echo "Removing CapScroll..."

sudo apt remove --purge -y "$PACKAGE_NAME"

echo
echo "================================================"
echo " CapScroll uninstalled successfully"
echo "================================================"
