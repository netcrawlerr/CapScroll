#!/bin/bash


set -e

APP_NAME="CapScroll.Desktop"
PACKAGE_NAME="capscroll"
VERSION="1.0.0"
ARCH="amd64"

PROJECT_DIR="$(pwd)"
DESKTOP_PROJ="$PROJECT_DIR/src/CapScroll.Desktop/CapScroll.Desktop.csproj"
DIST_DIR="$PROJECT_DIR/dist"
PUBLISH_DIR="$DIST_DIR/CapScroll-publish"
DEBIAN_DIR="$DIST_DIR/debian"

echo "================================================"
echo " Building CapScroll $VERSION"
echo "================================================"


if [ ! -f "$DESKTOP_PROJ" ]; then
    echo "Error: $DESKTOP_PROJ not found."
    echo "Run this script from the solution root repository directory."
    exit 1
fi

echo
echo "[1/7] Cleaning previous release..."

rm -rf "$DIST_DIR"
mkdir -p "$PUBLISH_DIR"


echo
echo "[2/7] Restoring dependencies..."

dotnet restore "$DESKTOP_PROJ"


echo
echo "[3/7] Publishing Linux x64 application..."

dotnet publish "$DESKTOP_PROJ" \
    -c Release \
    -r linux-x64 \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:PublishTrimmed=false \
    -o "$PUBLISH_DIR"


echo
echo "[4/7] Creating Debian package structure..."

mkdir -p "$DEBIAN_DIR/DEBIAN"
mkdir -p "$DEBIAN_DIR/usr/bin"
mkdir -p "$DEBIAN_DIR/usr/share/$PACKAGE_NAME"
mkdir -p "$DEBIAN_DIR/usr/share/applications"


echo
echo "[5/7] Copying application files..."

cp -r "$PUBLISH_DIR/"* "$DEBIAN_DIR/usr/share/$PACKAGE_NAME/"


cat << EOF > "$DEBIAN_DIR/usr/bin/$PACKAGE_NAME"
#!/bin/sh

exec /usr/share/$PACKAGE_NAME/$APP_NAME "\$@"
EOF

chmod 755 "$DEBIAN_DIR/usr/bin/$PACKAGE_NAME"


cat << EOF > "$DEBIAN_DIR/DEBIAN/control"
Package: $PACKAGE_NAME
Version: $VERSION
Section: utils
Priority: optional
Architecture: $ARCH
Maintainer: Mejid <github@netcrawlerr>
Depends: libicu-dev, libx11-6
Description: CapScroll
 Linux scrolling screenshot and region capture tool.
 Built with .NET and Avalonia.
EOF


cat << EOF > "$DEBIAN_DIR/usr/share/applications/$PACKAGE_NAME.desktop"
[Desktop Entry]
Version=1.0
Type=Application
Name=CapScroll
Comment=Scrolling screenshot and region capture tool
Exec=/usr/bin/$PACKAGE_NAME
Terminal=false
Categories=Graphics;Utility;
EOF


echo
echo "[6/7] Preparing Debian package..."

sudo chown -R root:root "$DEBIAN_DIR"

sudo chmod 755 "$DEBIAN_DIR/DEBIAN"
sudo chmod 755 "$DEBIAN_DIR/usr/bin/$PACKAGE_NAME"

sudo dpkg-deb \
    --build \
    "$DEBIAN_DIR" \
    "$DIST_DIR/${PACKAGE_NAME}_${VERSION}_${ARCH}.deb"


sudo chown -R "$(whoami):$(whoami)" "$DIST_DIR"

echo
echo "[7/7] Release complete!"
echo "================================================"
echo " CapScroll $VERSION"
echo "================================================"
echo
echo "Package built at:"
echo "  $DIST_DIR/${PACKAGE_NAME}_${VERSION}_${ARCH}.deb"
echo
echo "Install with:"
echo "  sudo apt install ./dist/${PACKAGE_NAME}_${VERSION}_${ARCH}.deb"
echo "================================================"
