#!/usr/bin/env bash
set -e

VERSION="1.0.0"
PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
DIST_DIR="${PROJECT_ROOT}/dist"
OUT_DIR="${DIST_DIR}/releases"

echo "=== Building Redstone NBT Release Packages (v${VERSION}) ==="
mkdir -p "${OUT_DIR}"

# 1. Compile all platforms
echo "[1/5] Compiling Linux x64..."
rm -rf "${DIST_DIR}/linux-x64"
dotnet publish "${PROJECT_ROOT}/src/RedstoneNBT/RedstoneNBT.csproj" -c Release -r linux-x64 --self-contained false -o "${DIST_DIR}/linux-x64"
(cd "${DIST_DIR}/linux-x64" && ln -sf RedstoneNBT redstone-nbt)
cp "${PROJECT_ROOT}/src/RedstoneNBT/Assets/redstone-nbt.png" "${DIST_DIR}/linux-x64/"
cp "${PROJECT_ROOT}/src/RedstoneNBT/Assets/redstone-nbt.ico" "${DIST_DIR}/linux-x64/"
cp "${PROJECT_ROOT}/LICENSE" "${DIST_DIR}/linux-x64/"

echo "[2/5] Compiling Windows x64..."
dotnet publish "${PROJECT_ROOT}/src/RedstoneNBT/RedstoneNBT.csproj" -c Release -r win-x64 --self-contained false -o "${DIST_DIR}/win-x64"
cp "${PROJECT_ROOT}/src/RedstoneNBT/Assets/redstone-nbt.ico" "${DIST_DIR}/win-x64/"
cp "${PROJECT_ROOT}/src/RedstoneNBT/Assets/redstone-nbt.png" "${DIST_DIR}/win-x64/"
cp "${PROJECT_ROOT}/LICENSE" "${DIST_DIR}/win-x64/"
# Create friendly redstone-nbt.exe copy
cp "${DIST_DIR}/win-x64/RedstoneNBT.exe" "${DIST_DIR}/win-x64/redstone-nbt.exe"

echo "[3/5] Compiling macOS Intel (x64) and Apple Silicon (arm64)..."
dotnet publish "${PROJECT_ROOT}/src/RedstoneNBT/RedstoneNBT.csproj" -c Release -r osx-x64 --self-contained false -o "${DIST_DIR}/osx-x64"
cp "${PROJECT_ROOT}/src/RedstoneNBT/Assets/redstone-nbt.png" "${DIST_DIR}/osx-x64/"
cp "${PROJECT_ROOT}/LICENSE" "${DIST_DIR}/osx-x64/"
(cd "${DIST_DIR}/osx-x64" && ln -sf RedstoneNBT redstone-nbt)

dotnet publish "${PROJECT_ROOT}/src/RedstoneNBT/RedstoneNBT.csproj" -c Release -r osx-arm64 --self-contained false -o "${DIST_DIR}/osx-arm64"
cp "${PROJECT_ROOT}/src/RedstoneNBT/Assets/redstone-nbt.png" "${DIST_DIR}/osx-arm64/"
cp "${PROJECT_ROOT}/LICENSE" "${DIST_DIR}/osx-arm64/"
(cd "${DIST_DIR}/osx-arm64" && ln -sf RedstoneNBT redstone-nbt)

# 2. Package tar.gz and zip
echo "[4/5] Archiving standalone release archives..."

# Linux tar.gz
rm -rf "/tmp/redstone-nbt-${VERSION}-linux-x64"
mkdir -p "/tmp/redstone-nbt-${VERSION}-linux-x64"
cp -a "${DIST_DIR}/linux-x64/"* "/tmp/redstone-nbt-${VERSION}-linux-x64/"
tar -czf "${OUT_DIR}/redstone-nbt-${VERSION}-linux-x64.tar.gz" -C "/tmp" "redstone-nbt-${VERSION}-linux-x64"
rm -rf "/tmp/redstone-nbt-${VERSION}-linux-x64"

# Windows zip
(cd "${DIST_DIR}/win-x64" && zip -rq "${OUT_DIR}/redstone-nbt-${VERSION}-win-x64.zip" .)

# macOS x64 tar.gz
rm -rf "/tmp/redstone-nbt-${VERSION}-osx-x64"
mkdir -p "/tmp/redstone-nbt-${VERSION}-osx-x64"
cp -a "${DIST_DIR}/osx-x64/"* "/tmp/redstone-nbt-${VERSION}-osx-x64/"
tar -czf "${OUT_DIR}/redstone-nbt-${VERSION}-osx-x64.tar.gz" -C "/tmp" "redstone-nbt-${VERSION}-osx-x64"
rm -rf "/tmp/redstone-nbt-${VERSION}-osx-x64"

# macOS arm64 tar.gz
rm -rf "/tmp/redstone-nbt-${VERSION}-osx-arm64"
mkdir -p "/tmp/redstone-nbt-${VERSION}-osx-arm64"
cp -a "${DIST_DIR}/osx-arm64/"* "/tmp/redstone-nbt-${VERSION}-osx-arm64/"
tar -czf "${OUT_DIR}/redstone-nbt-${VERSION}-osx-arm64.tar.gz" -C "/tmp" "redstone-nbt-${VERSION}-osx-arm64"
rm -rf "/tmp/redstone-nbt-${VERSION}-osx-arm64"

# 3. Build Debian / Ubuntu package (.deb)
echo "[5/5] Building Debian / Ubuntu (.deb) package..."
DEB_BUILD="/tmp/redstone-nbt-deb-build"
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
Architecture: amd64
Depends: dotnet-runtime-10.0 | dotnet-runtime-8.0 | dotnet-runtime-9.0 | libc6, libfontconfig1, libx11-6
Maintainer: Raul1x9 <raul@local>
Description: Modern cross-platform Minecraft NBT editor
 Redstone NBT is a high-performance cross-platform Minecraft NBT and region
 file editor revived and modernized from Justin Aquadro's classic NBTExplorer.
 Supports Java and Bedrock formats, inline editing, and region files.
EOF

cp -a "${DIST_DIR}/linux-x64/"* "${DEB_BUILD}/usr/lib/redstone-nbt/"
chmod +x "${DEB_BUILD}/usr/lib/redstone-nbt/redstone-nbt"
chmod +x "${DEB_BUILD}/usr/lib/redstone-nbt/RedstoneNBT"
ln -sf "/usr/lib/redstone-nbt/redstone-nbt" "${DEB_BUILD}/usr/bin/redstone-nbt"

cp "${PROJECT_ROOT}/packaging/redstone-nbt.desktop" "${DEB_BUILD}/usr/share/applications/"
cp "${PROJECT_ROOT}/src/RedstoneNBT/Assets/redstone-nbt.png" "${DEB_BUILD}/usr/share/icons/hicolor/256x256/apps/redstone-nbt.png"
cp "${PROJECT_ROOT}/LICENSE" "${DEB_BUILD}/usr/share/doc/redstone-nbt/copyright"

# Pack .deb using standard ar format
(
    cd "${DEB_BUILD}"
    echo "2.0" > /tmp/debian-binary
    (cd DEBIAN && tar --numeric-owner --owner=0 --group=0 -czf /tmp/control.tar.gz .)
    tar --numeric-owner --owner=0 --group=0 --exclude="./DEBIAN" -czf /tmp/data.tar.gz .
    ar rcs "${OUT_DIR}/redstone-nbt_${VERSION}_amd64.deb" /tmp/debian-binary /tmp/control.tar.gz /tmp/data.tar.gz
    rm -f /tmp/debian-binary /tmp/control.tar.gz /tmp/data.tar.gz
)
rm -rf "${DEB_BUILD}"

echo "=== All Release Packages Created Successfully ==="
ls -lh "${OUT_DIR}"
