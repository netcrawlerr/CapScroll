#!/bin/bash

set -e

APP_NAME="CapScroll.Desktop"
PACKAGE_NAME="capscroll"
VERSION="1.2.0"
ARCH="amd64"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"

DESKTOP_PROJ="$PROJECT_DIR/src/CapScroll.Desktop/CapScroll.Desktop.csproj"
DIST_DIR="$PROJECT_DIR/dist"
PUBLISH_DIR="$DIST_DIR/CapScroll-publish"
DEBIAN_DIR="$DIST_DIR/debian"
APPDIR="$DIST_DIR/AppDir"

echo "================================================"
echo " Building CapScroll $VERSION (.deb & AppImage)"
echo "================================================"
echo
echo "Directory: $PROJECT_DIR"
echo "Script:    $SCRIPT_DIR"
echo

if [ ! -f "$DESKTOP_PROJ" ]; then
    echo "Error: $DESKTOP_PROJ not found."
    echo "Make sure this script is located in the directory's scripts directory."
    exit 1
fi

echo
echo "[1/9] Cleaning previous release artifacts..."

rm -rf "$DIST_DIR"
mkdir -p "$PUBLISH_DIR"

echo
echo "[2/9] Restoring dependencies..."

dotnet restore "$DESKTOP_PROJ"

echo
echo "[3/9] Publishing Linux x64 application..."

dotnet publish "$DESKTOP_PROJ" \
    -c Release \
    -r linux-x64 \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:PublishTrimmed=false \
    -o "$PUBLISH_DIR"

echo
echo "[4/9] Creating Debian package structure..."

mkdir -p "$DEBIAN_DIR/DEBIAN"
mkdir -p "$DEBIAN_DIR/usr/bin"
mkdir -p "$DEBIAN_DIR/usr/share/$PACKAGE_NAME"
mkdir -p "$DEBIAN_DIR/usr/share/applications"
mkdir -p "$DEBIAN_DIR/usr/share/pixmaps"

echo
echo "[5/9] Copying application files for Debian..."

cp -r "$PUBLISH_DIR/"* \
    "$DEBIAN_DIR/usr/share/$PACKAGE_NAME/"

cp "$PROJECT_DIR/src/CapScroll.Desktop/Assets/Images/logo.png" \
    "$DEBIAN_DIR/usr/share/pixmaps/$PACKAGE_NAME.png"

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
Icon=$PACKAGE_NAME
StartupWMClass=$APP_NAME
Terminal=false
Categories=Graphics;Utility;
EOF

echo
echo "[6/9] Building Debian package..."

sudo chown -R root:root "$DEBIAN_DIR"
sudo chmod 755 "$DEBIAN_DIR/DEBIAN"
sudo chmod 755 "$DEBIAN_DIR/usr/bin/$PACKAGE_NAME"

sudo dpkg-deb \
    --build \
    "$DEBIAN_DIR" \
    "$DIST_DIR/${PACKAGE_NAME}_${VERSION}_${ARCH}.deb"

sudo chown -R "$(whoami):$(whoami)" "$DIST_DIR"

echo
echo "[7/9] Preparing AppImage structure..."

mkdir -p "$APPDIR/usr/bin"
mkdir -p "$APPDIR/usr/share/icons/hicolor/256x256/apps"


cp -r "$PUBLISH_DIR/"* "$APPDIR/usr/bin/"

# Copy icon
cp "$PROJECT_DIR/src/CapScroll.Desktop/Assets/Images/logo.png" \
    "$APPDIR/usr/share/icons/hicolor/256x256/apps/$PACKAGE_NAME.png"
cp "$PROJECT_DIR/src/CapScroll.Desktop/Assets/Images/logo.png" \
    "$APPDIR/$PACKAGE_NAME.png"


cat << EOF > "$APPDIR/$PACKAGE_NAME.desktop"
[Desktop Entry]
Version=1.0
Type=Application
Name=CapScroll
Comment=Scrolling screenshot and region capture tool
Exec=$APP_NAME
Icon=$PACKAGE_NAME
StartupWMClass=$APP_NAME
Terminal=false
Categories=Graphics;Utility;
EOF

cat << 'EOF' > "$APPDIR/AppRun"
#!/bin/sh
HERE="$(dirname "$(readlink -f "${0}")")"
export PATH="${HERE}/usr/bin:${PATH}"
export LD_LIBRARY_PATH="${HERE}/usr/bin:${LD_LIBRARY_PATH}"
exec "${HERE}/usr/bin/CapScroll.Desktop" "$@"
EOF

chmod +x "$APPDIR/AppRun"

echo
echo "[8/9] Checking appimagetool..."

APPIMAGETOOL="$DIST_DIR/appimagetool"

if ! command -v appimagetool &> /dev/null; then
    if [ ! -f "$APPIMAGETOOL" ]; then
        echo "Downloading appimagetool..."
        curl -sL "https://github.com/AppImage/appimagetool/releases/download/continuous/appimagetool-x86_64.AppImage" -o "$APPIMAGETOOL"
        chmod +x "$APPIMAGETOOL"
    fi
    APPIMAGETOOL_CMD="$APPIMAGETOOL"
else
    APPIMAGETOOL_CMD="appimagetool"
fi

echo
echo "[9/9] Building AppImage..."

ARCH=x86_64 "$APPIMAGETOOL_CMD" "$APPDIR" "$DIST_DIR/CapScroll_${VERSION}_x86_64.AppImage"

echo
echo "================================================"
echo " CapScroll $VERSION Build Complete!"
echo "================================================"
echo
echo "Build Artifacts:"
echo "  Debian Package: $DIST_DIR/${PACKAGE_NAME}_${VERSION}_${ARCH}.deb"
echo "  AppImage:       $DIST_DIR/CapScroll_${VERSION}_x86_64.AppImage"
echo
echo "================================================"
