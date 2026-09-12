#!/usr/bin/env bash

set -euo pipefail

PACKAGING_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
APP_DIR="$(dirname "$PACKAGING_DIR")"
CONTROL_FILE="$PACKAGING_DIR/deb/control"
OUTPUT_DIR="$APP_DIR/dist"

PACKAGE_NAME="$(sed -n 's/^Package: //p' "$CONTROL_FILE")"
PACKAGE_VERSION="$(sed -n 's/^Version: //p' "$CONTROL_FILE")"
PACKAGE_ARCHITECTURE="$(sed -n 's/^Architecture: //p' "$CONTROL_FILE")"

STAGING_DIR="$(mktemp -d)"
PACKAGE_ROOT="$STAGING_DIR/$PACKAGE_NAME"

trap 'rm -rf "$STAGING_DIR"' EXIT

install -d "$PACKAGE_ROOT/DEBIAN"
install -m 0644 "$CONTROL_FILE" "$PACKAGE_ROOT/DEBIAN/control"

install -m 0644 \
    "$PACKAGING_DIR/deb/conffiles" \
    "$PACKAGE_ROOT/DEBIAN/conffiles"

install -m 0755 \
    "$PACKAGING_DIR/deb/postinst" \
    "$PACKAGE_ROOT/DEBIAN/postinst"

install -m 0755 \
    "$PACKAGING_DIR/deb/postrm" \
    "$PACKAGE_ROOT/DEBIAN/postrm"

install -d "$PACKAGE_ROOT/usr/share/$PACKAGE_NAME"

cp -a \
    "$APP_DIR/public" \
    "$APP_DIR/src" \
    "$APP_DIR/database" \
    "$PACKAGE_ROOT/usr/share/$PACKAGE_NAME/"

install -d "$PACKAGE_ROOT/usr/share/$PACKAGE_NAME/config"
ln -s \
    /etc/realtime-chat-php/database.local.php \
    "$PACKAGE_ROOT/usr/share/$PACKAGE_NAME/config/database.local.php"

install -Dm0644 \
    "$PACKAGING_DIR/deb/nginx.conf" \
    "$PACKAGE_ROOT/etc/nginx/sites-available/$PACKAGE_NAME"

install -Dm0644 \
    "$APP_DIR/README.md" \
    "$PACKAGE_ROOT/usr/share/doc/$PACKAGE_NAME/README.md"
install -d "$OUTPUT_DIR"

OUTPUT_FILE="$OUTPUT_DIR/${PACKAGE_NAME}_${PACKAGE_VERSION}_${PACKAGE_ARCHITECTURE}.deb"

dpkg-deb \
    --root-owner-group \
    --build "$PACKAGE_ROOT" \
    "$OUTPUT_FILE"

echo "Package created: $OUTPUT_FILE"
