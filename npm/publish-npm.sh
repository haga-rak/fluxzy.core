#!/usr/bin/env bash
set -euo pipefail

# publish-npm.sh - Download Fluxzy CLI binaries from a GitHub release and publish to npm
#
# Usage: ./npm/publish-npm.sh <release-tag>
# Example: ./npm/publish-npm.sh v1.2.3
#
# Environment variables (required):
#   NPM_TOKEN    - npm authentication token for publishing
#
# Environment variables (optional):
#   GITHUB_TOKEN - GitHub token for downloading release assets (needed for private repos)
#   NPM_DRY_RUN - set to "true" to run npm publish with --dry-run

REPO="haga-rak/fluxzy.core"
SCOPE="@fluxzy"
BIN_NAME="fluxzy"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# --- Platform definitions ---
# Format: RID;npm_os;npm_cpu;archive_ext;binary_name
PLATFORMS=(
  "win-x64;win32;x64;zip;fluxzy.exe"
  "win-x86;win32;ia32;zip;fluxzy.exe"
  "win-arm64;win32;arm64;zip;fluxzy.exe"
  "linux-x64;linux;x64;tar.gz;fluxzy"
  "linux-arm64;linux;arm64;tar.gz;fluxzy"
  "osx-x64;darwin;x64;tar.gz;fluxzy"
  "osx-arm64;darwin;arm64;tar.gz;fluxzy"
)

# --- Argument parsing ---
if [[ $# -ne 1 ]]; then
  echo "Usage: $0 <release-tag>"
  echo "Example: $0 v1.2.3"
  exit 1
fi

TAG="$1"

# Strip leading 'v' and extract 3-part semver (drop 4th segment if present)
VERSION="${TAG#v}"

if [[ ! "$VERSION" =~ ^([0-9]+\.[0-9]+\.[0-9]+) ]]; then
  echo "Error: '$TAG' does not look like a valid version tag (expected vX.Y.Z or X.Y.Z)"
  exit 1
fi

VERSION="${BASH_REMATCH[1]}"

# --- Validate environment ---
if [[ -z "${NPM_TOKEN:-}" ]]; then
  echo "Error: NPM_TOKEN environment variable is not set"
  exit 1
fi

DRY_RUN_FLAG=""
if [[ "${NPM_DRY_RUN:-}" == "true" ]]; then
  DRY_RUN_FLAG="--dry-run"
  echo "*** DRY RUN MODE ***"
fi

# --- Check dependencies ---
for cmd in gh jq tar unzip node; do
  if ! command -v "$cmd" &>/dev/null; then
    echo "Error: '$cmd' is required but not found in PATH"
    exit 1
  fi
done

# --- Setup ---
WORK_DIR="$(mktemp -d)"
trap 'rm -rf "$WORK_DIR"' EXIT

echo "Publishing ${SCOPE}/cli@${VERSION} from release ${TAG}"
echo "Working directory: ${WORK_DIR}"

# Configure npm auth
echo "//registry.npmjs.org/:_authToken=${NPM_TOKEN}" > "${WORK_DIR}/.npmrc"

# --- Download release assets ---
echo ""
echo "=== Downloading release assets ==="

DOWNLOAD_DIR="${WORK_DIR}/downloads"
mkdir -p "$DOWNLOAD_DIR"

# Download archives only (exclude .sha256 hash files)
gh release download "$TAG" --repo "$REPO" --dir "$DOWNLOAD_DIR" \
  --pattern "fluxzy-cli-*.zip" --pattern "fluxzy-cli-*.tar.gz"

echo "Downloaded assets:"
ls -la "$DOWNLOAD_DIR"

# --- Publish platform-specific packages ---
echo ""
echo "=== Publishing platform packages ==="

PUBLISHED_PACKAGES=()

for platform_def in "${PLATFORMS[@]}"; do
  IFS=';' read -r rid npm_os npm_cpu archive_ext bin_name <<< "$platform_def"

  pkg_name="${SCOPE}/cli-${npm_os}-${npm_cpu}"
  pkg_dir="${WORK_DIR}/packages/${npm_os}-${npm_cpu}"
  bin_dir="${pkg_dir}/bin"

  echo ""
  echo "--- ${pkg_name}@${VERSION} (${rid}) ---"

  # Find the asset
  asset_pattern="fluxzy-cli-*-${rid}.${archive_ext}"
  asset_path=$(find "$DOWNLOAD_DIR" -name "$asset_pattern" | head -n1 || true)

  if [[ -z "$asset_path" ]]; then
    echo "Warning: No asset found matching ${asset_pattern} — skipping ${pkg_name}"
    continue
  fi

  echo "Using asset: $(basename "$asset_path")"

  # Create package directory
  mkdir -p "$bin_dir"

  # Extract binary
  if [[ "$archive_ext" == "tar.gz" ]]; then
    tar xzf "$asset_path" -C "$bin_dir"
  else
    unzip -q "$asset_path" -d "$bin_dir"
  fi

  # Verify binary exists
  if [[ ! -f "${bin_dir}/${bin_name}" ]]; then
    echo "Error: Binary '${bin_name}' not found after extraction"
    echo "Contents:"
    ls -la "$bin_dir"
    exit 1
  fi

  # Ensure binary is executable (Unix)
  if [[ "$bin_name" != *.exe ]]; then
    chmod +x "${bin_dir}/${bin_name}"
  fi

  # Create package.json
  cat > "${pkg_dir}/package.json" <<EOF
{
  "name": "${pkg_name}",
  "version": "${VERSION}",
  "description": "Fluxzy CLI binary for ${npm_os}-${npm_cpu}",
  "license": "GPL-3.0",
  "repository": {
    "type": "git",
    "url": "https://github.com/${REPO}.git"
  },
  "os": ["${npm_os}"],
  "cpu": ["${npm_cpu}"],
  "files": ["bin/"],
  "preferUnplugged": true
}
EOF

  # Publish
  npm publish "$pkg_dir" --access public --userconfig "${WORK_DIR}/.npmrc" $DRY_RUN_FLAG
  PUBLISHED_PACKAGES+=("${npm_os};${npm_cpu};${pkg_name}")
  echo "Published ${pkg_name}@${VERSION}"
done

if [[ ${#PUBLISHED_PACKAGES[@]} -eq 0 ]]; then
  echo "Error: No platform packages were published. Check that the release has matching assets."
  exit 1
fi

# --- Publish wrapper package ---
echo ""
echo "=== Publishing wrapper package: ${SCOPE}/cli ==="

WRAPPER_DIR="${WORK_DIR}/packages/cli"
WRAPPER_BIN_DIR="${WRAPPER_DIR}/bin"
mkdir -p "$WRAPPER_BIN_DIR"

# Build optionalDependencies from actually published packages
OPT_DEPS="{"
first=true
for published in "${PUBLISHED_PACKAGES[@]}"; do
  IFS=';' read -r npm_os npm_cpu pkg_name <<< "$published"
  if [[ "$first" == true ]]; then
    first=false
  else
    OPT_DEPS+=","
  fi
  OPT_DEPS+="\"${pkg_name}\": \"${VERSION}\""
done
OPT_DEPS+="}"

# Create wrapper package.json
cat > "${WRAPPER_DIR}/package.json" <<EOF
{
  "name": "${SCOPE}/cli",
  "version": "${VERSION}",
  "description": "Fluxzy - MITM proxy for HTTP traffic interception, recording, and alteration",
  "license": "GPL-3.0",
  "repository": {
    "type": "git",
    "url": "https://github.com/${REPO}.git"
  },
  "homepage": "https://www.fluxzy.io",
  "keywords": [
    "fluxzy",
    "proxy",
    "mitm",
    "http",
    "https",
    "http2",
    "websocket",
    "traffic",
    "intercept",
    "debug"
  ],
  "bin": {
    "${BIN_NAME}": "bin/cli.js"
  },
  "files": ["bin/"],
  "optionalDependencies": ${OPT_DEPS}
}
EOF

# Create wrapper CLI script
cp "${SCRIPT_DIR}/cli-wrapper.js" "${WRAPPER_BIN_DIR}/cli.js"

# Copy README
cp "${SCRIPT_DIR}/README.md" "${WRAPPER_DIR}/README.md"

# Publish wrapper
npm publish "$WRAPPER_DIR" --access public --userconfig "${WORK_DIR}/.npmrc" $DRY_RUN_FLAG

echo ""
echo "=== Done ==="
echo "Published ${SCOPE}/cli@${VERSION} with all platform packages"
echo ""
echo "Users can now install with:"
echo "  npm install -g ${SCOPE}/cli"
echo "  npx ${BIN_NAME}"
