#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."

VERSION=$(python3 -c "import json; print(json.load(open('manifest.json'))['version_number'])")
NAME=$(python3 -c "import json; print(json.load(open('manifest.json'))['name'])")

BUILD_ARGS=()
if [ -n "${VALHEIM_MANAGED_DIR:-}" ]; then
  BUILD_ARGS+=("-p:ValheimManagedDir=$VALHEIM_MANAGED_DIR")
fi
if [ -n "${BEPINEX_CORE_DIR:-}" ]; then
  BUILD_ARGS+=("-p:BepInExCoreDir=$BEPINEX_CORE_DIR")
fi

dotnet build src/LessSleep.csproj -c Release --nologo ${BUILD_ARGS[@]+"${BUILD_ARGS[@]}"}

rm -rf dist
mkdir -p dist/package/BepInEx/plugins
cp manifest.json README.md CHANGELOG.md icon.png dist/package/
cp src/bin/Release/LessSleep.dll dist/package/BepInEx/plugins/

(cd dist/package && zip -qr "../LessCx-${NAME}-${VERSION}.zip" .)
echo "built dist/LessCx-${NAME}-${VERSION}.zip"
