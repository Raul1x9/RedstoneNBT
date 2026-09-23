#!/usr/bin/env bash
set -e

VERSION="1.0.0"
PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
DIST_DIR="${PROJECT_ROOT}/dist"
OUT_DIR="${DIST_DIR}/releases"

echo "==========================================================="
echo " Building Redstone NBT Release Packages (v${VERSION}) "
echo "==========================================================="
mkdir -p "${OUT_DIR}"

# 1. Compile Linux (x64 and arm64)
echo "[1/6] Compiling Linux x64 and ARM64..."
rm -rf "${DIST_DIR}/linux-x64" "${DIST_DIR}/linux-arm64"

dotnet publish "${PROJECT_ROOT}/src/RedstoneNBT/RedstoneNBT.csproj" -c Release -r linux-x64 --self-contained false -o "${DIST_DIR}/linux-x64"
(cd "${DIST_DIR}/linux-x64" && ln -sf RedstoneNBT redstone-nbt)
cp "${PROJECT_ROOT}/src/RedstoneNBT/Assets/redstone-nbt.png" "${DIST_DIR}/linux-x64/"
cp "${PROJECT_ROOT}/src/RedstoneNBT/Assets/redstone-nbt.ico" "${DIST_DIR}/linux-x64/"
cp "${PROJECT_ROOT}/LICENSE" "${DIST_DIR}/linux-x64/"

dotnet publish "${PROJECT_ROOT}/src/RedstoneNBT/RedstoneNBT.csproj" -c Release -r linux-arm64 --self-contained false -o "${DIST_DIR}/linux-arm64"
(cd "${DIST_DIR}/linux-arm64" && ln -sf RedstoneNBT redstone-nbt)
cp "${PROJECT_ROOT}/src/RedstoneNBT/Assets/redstone-nbt.png" "${DIST_DIR}/linux-arm64/"
cp "${PROJECT_ROOT}/src/RedstoneNBT/Assets/redstone-nbt.ico" "${DIST_DIR}/linux-arm64/"
cp "${PROJECT_ROOT}/LICENSE" "${DIST_DIR}/linux-arm64/"

# 2. Compile Windows (x64 and arm64)
echo "[2/6] Compiling Windows x64 and ARM64..."
rm -rf "${DIST_DIR}/win-x64" "${DIST_DIR}/win-arm64"

dotnet publish "${PROJECT_ROOT}/src/RedstoneNBT/RedstoneNBT.csproj" -c Release -r win-x64 --self-contained false -o "${DIST_DIR}/win-x64"
cp "${PROJECT_ROOT}/src/RedstoneNBT/Assets/redstone-nbt.ico" "${DIST_DIR}/win-x64/"
cp "${PROJECT_ROOT}/src/RedstoneNBT/Assets/redstone-nbt.png" "${DIST_DIR}/win-x64/"
cp "${PROJECT_ROOT}/LICENSE" "${DIST_DIR}/win-x64/"
cp "${DIST_DIR}/win-x64/RedstoneNBT.exe" "${DIST_DIR}/win-x64/redstone-nbt.exe"

dotnet publish "${PROJECT_ROOT}/src/RedstoneNBT/RedstoneNBT.csproj" -c Release -r win-arm64 --self-contained false -o "${DIST_DIR}/win-arm64"
cp "${PROJECT_ROOT}/src/RedstoneNBT/Assets/redstone-nbt.ico" "${DIST_DIR}/win-arm64/"
cp "${PROJECT_ROOT}/src/RedstoneNBT/Assets/redstone-nbt.png" "${DIST_DIR}/win-arm64/"
cp "${PROJECT_ROOT}/LICENSE" "${DIST_DIR}/win-arm64/"
cp "${DIST_DIR}/win-arm64/RedstoneNBT.exe" "${DIST_DIR}/win-arm64/redstone-nbt.exe"

# 3. Compile macOS (x64 Intel and arm64 Apple Silicon)
echo "[3/6] Compiling macOS Intel (x64) and Apple Silicon (arm64)..."
rm -rf "${DIST_DIR}/osx-x64" "${DIST_DIR}/osx-arm64"

dotnet publish "${PROJECT_ROOT}/src/RedstoneNBT/RedstoneNBT.csproj" -c Release -r osx-x64 --self-contained false -o "${DIST_DIR}/osx-x64"
cp "${PROJECT_ROOT}/src/RedstoneNBT/Assets/redstone-nbt.png" "${DIST_DIR}/osx-x64/"
cp "${PROJECT_ROOT}/LICENSE" "${DIST_DIR}/osx-x64/"
(cd "${DIST_DIR}/osx-x64" && ln -sf RedstoneNBT redstone-nbt)

dotnet publish "${PROJECT_ROOT}/src/RedstoneNBT/RedstoneNBT.csproj" -c Release -r osx-arm64 --self-contained false -o "${DIST_DIR}/osx-arm64"
cp "${PROJECT_ROOT}/src/RedstoneNBT/Assets/redstone-nbt.png" "${DIST_DIR}/osx-arm64/"
cp "${PROJECT_ROOT}/LICENSE" "${DIST_DIR}/osx-arm64/"
(cd "${DIST_DIR}/osx-arm64" && ln -sf RedstoneNBT redstone-nbt)

# 4. Create standalone tar.gz and zip archives
echo "[4/6] Archiving Linux and Windows release archives..."

# Linux x64 tar.gz
rm -rf "/tmp/redstone-nbt-${VERSION}-linux-x64"
mkdir -p "/tmp/redstone-nbt-${VERSION}-linux-x64"
cp -a "${DIST_DIR}/linux-x64/"* "/tmp/redstone-nbt-${VERSION}-linux-x64/"
tar -czf "${OUT_DIR}/redstone-nbt-${VERSION}-linux-x64.tar.gz" -C "/tmp" "redstone-nbt-${VERSION}-linux-x64"
rm -rf "/tmp/redstone-nbt-${VERSION}-linux-x64"

# Linux arm64 tar.gz
rm -rf "/tmp/redstone-nbt-${VERSION}-linux-arm64"
mkdir -p "/tmp/redstone-nbt-${VERSION}-linux-arm64"
cp -a "${DIST_DIR}/linux-arm64/"* "/tmp/redstone-nbt-${VERSION}-linux-arm64/"
tar -czf "${OUT_DIR}/redstone-nbt-${VERSION}-linux-arm64.tar.gz" -C "/tmp" "redstone-nbt-${VERSION}-linux-arm64"
rm -rf "/tmp/redstone-nbt-${VERSION}-linux-arm64"

# Windows x64 zip
(cd "${DIST_DIR}/win-x64" && zip -rq "${OUT_DIR}/redstone-nbt-${VERSION}-win-x64.zip" .)

# Windows arm64 zip
(cd "${DIST_DIR}/win-arm64" && zip -rq "${OUT_DIR}/redstone-nbt-${VERSION}-win-arm64.zip" .)

# 5. Create macOS App Bundles (.app inside .tar.gz)
echo "[5/6] Packaging macOS Application Bundles (.app)..."

create_macos_app() {
    local ARCH="$1"
    local SRC_DIR="$2"
    local APP_DIR="/tmp/Redstone NBT.app"
    rm -rf "${APP_DIR}"
    mkdir -p "${APP_DIR}/Contents/MacOS"
    mkdir -p "${APP_DIR}/Contents/Resources"

    cp -a "${SRC_DIR}/"* "${APP_DIR}/Contents/MacOS/"
    chmod +x "${APP_DIR}/Contents/MacOS/RedstoneNBT"
    cp "${PROJECT_ROOT}/src/RedstoneNBT/Assets/redstone-nbt.png" "${APP_DIR}/Contents/Resources/"

    cat <<EOF > "${APP_DIR}/Contents/Info.plist"
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleExecutable</key>
    <string>RedstoneNBT</string>
    <key>CFBundleIdentifier</key>
    <string>io.github.raul1x9.redstonenbt</string>
    <key>CFBundleName</key>
    <string>Redstone NBT</string>
    <key>CFBundleDisplayName</key>
    <string>Redstone NBT</string>
    <key>CFBundleVersion</key>
    <string>${VERSION}</string>
    <key>CFBundleShortVersionString</key>
    <string>${VERSION}</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleSignature</key>
    <string>????</string>
    <key>NSHighResolutionCapable</key>
    <true/>
</dict>
</plist>
EOF

    tar -czf "${OUT_DIR}/redstone-nbt-${VERSION}-osx-${ARCH}.tar.gz" -C "/tmp" "Redstone NBT.app"
    rm -rf "${APP_DIR}"
}

create_macos_app "x64" "${DIST_DIR}/osx-x64"
create_macos_app "arm64" "${DIST_DIR}/osx-arm64"

# 6. Build Debian / Ubuntu packages (.deb) for amd64 and arm64
echo "[6/6] Building Debian / Ubuntu (.deb) packages..."

build_deb() {
    local DEB_ARCH="$1"
    local SRC_DIR="$2"
    local DEB_BUILD="/tmp/redstone-nbt-deb-${DEB_ARCH}"
    rm -rf "${DEB_BUILD}"
    mkdir -p "${DEB_BUILD}/DEBIAN"
    mkdir -p "${DEB_BUILD}/usr/lib/redstone-nbt"
    mkdir -p "${DEB_BUILD}/usr/bin"
    mkdir -p "${DEB_BUILD}/usr/share/applications"
    mkdir -p "${DEB_BUILD}/usr/share/icons/hicolor/256x256/apps"
    mkdir -p "${DEB_BUILD}/usr/share/doc/redstone-nbt"

    cat <<EOF > "${DEB_BUILD}/DEBIAN/control"
Package: redstone-nbt
Version: ${VERSION}
Section: utils
Priority: optional
Architecture: ${DEB_ARCH}
Depends: dotnet-runtime-10.0 | dotnet-runtime-8.0 | dotnet-runtime-9.0 | libc6, libfontconfig1, libx11-6
Maintainer: Raul1x9 <raul@local>
Description: Modern cross-platform Minecraft NBT editor
 Redstone NBT is a high-performance cross-platform Minecraft NBT and region
 file editor revived and modernized from Justin Aquadro's classic NBTExplorer.
 Supports Java and Bedrock formats, inline editing, and region files.
EOF

    cp -a "${SRC_DIR}/"* "${DEB_BUILD}/usr/lib/redstone-nbt/"
    chmod +x "${DEB_BUILD}/usr/lib/redstone-nbt/redstone-nbt"
    chmod +x "${DEB_BUILD}/usr/lib/redstone-nbt/RedstoneNBT"
    ln -sf "/usr/lib/redstone-nbt/redstone-nbt" "${DEB_BUILD}/usr/bin/redstone-nbt"

    cp "${PROJECT_ROOT}/packaging/redstone-nbt.desktop" "${DEB_BUILD}/usr/share/applications/"
    cp "${PROJECT_ROOT}/src/RedstoneNBT/Assets/redstone-nbt.png" "${DEB_BUILD}/usr/share/icons/hicolor/256x256/apps/redstone-nbt.png"
    cp "${PROJECT_ROOT}/LICENSE" "${DEB_BUILD}/usr/share/doc/redstone-nbt/copyright"

    (
        cd "${DEB_BUILD}"
        echo "2.0" > /tmp/debian-binary
        (cd DEBIAN && tar --numeric-owner --owner=0 --group=0 -czf /tmp/control.tar.gz .)
        tar --numeric-owner --owner=0 --group=0 --exclude="./DEBIAN" -czf /tmp/data.tar.gz .
        ar rcs "${OUT_DIR}/redstone-nbt_${VERSION}_${DEB_ARCH}.deb" /tmp/debian-binary /tmp/control.tar.gz /tmp/data.tar.gz
        rm -f /tmp/debian-binary /tmp/control.tar.gz /tmp/data.tar.gz
    )
    rm -rf "${DEB_BUILD}"
}

build_deb "amd64" "${DIST_DIR}/linux-x64"
build_deb "arm64" "${DIST_DIR}/linux-arm64"

echo ""
echo "==========================================================="
echo " All 8 Release Packages Created Successfully in dist/releases/ "
echo "==========================================================="
ls -lh "${OUT_DIR}"
