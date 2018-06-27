#!/bin/bash
# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

set -e

MONO_FX_DIR="/Library/Frameworks/Mono.framework/"
MONO_VERSIONS_DIR="${MONO_FX_DIR}Versions/"
MONO_CURRENT_SYMLINK="${MONO_VERSIONS_DIR}Current"
MONO_VERSIONS_AVAILABLE=($(
  find "$MONO_VERSIONS_DIR" -maxdepth 1 -mindepth 1 -type d -exec basename {} \; 2>/dev/null | sort -Vr
))
LATEST_MONO_TARGET_VERSION="${MONO_VERSIONS_AVAILABLE[0]}"
MONO_TARGET_VERSION="$1"

if [ -z "$LATEST_MONO_TARGET_VERSION" ]; then
  echo "No Mono installations found in ${MONO_VERSIONS_DIR}"
  exit 1
fi

function showAvailableVersions() {
  echo "Available Mono Versions:"
  echo "  latest ($LATEST_MONO_TARGET_VERSION)"
  for version in "${MONO_VERSIONS_AVAILABLE[@]}"; do
    echo "  ${version}"
  done
}

case "$MONO_TARGET_VERSION" in
''|[/-]h|[/-]?|[/-]help|--help|help)
  echo "usage: $0 INSTALLED_MONO_VERSION"
  echo
  showAvailableVersions
  exit 2
  ;;
latest)
  MONO_TARGET_VERSION="$LATEST_MONO_TARGET_VERSION"
  ;;
esac

showAvailableVersions

MONO_TARGET_VERSION_DIR="${MONO_VERSIONS_DIR}${MONO_TARGET_VERSION}"

if [ ! -d "$MONO_TARGET_VERSION_DIR" ]; then
  echo "Mono ${MONO_TARGET_VERSION} is not installed in ${MONO_VERSIONS_DIR}"
  exit 3
fi

echo "Selecting Mono ${MONO_TARGET_VERSION}"

rm -f "$MONO_CURRENT_SYMLINK"
ln -s "$MONO_TARGET_VERSION_DIR" "$MONO_CURRENT_SYMLINK"
mkdir -p /etc/paths.d
echo "$MONO_CURRENT_SYMLINK" > /etc/paths.d/mono-commands

if [ "$(readlink "$MONO_CURRENT_SYMLINK")" != "$MONO_TARGET_VERSION_DIR" ]; then
  echo "Failed to update ${MONO_CURRENT_SYMLINK} to point to ${MONO_TARGET_VERSION_DIR}"
  exit 4
fi

mono --version | head -n1