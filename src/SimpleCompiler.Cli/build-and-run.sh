#! /usr/bin/env bash
set -euo pipefail

OPTIMIZE=false
if [ "$1" = "-O" ]; then
    shift 1
    OPTIMIZE=true
fi

VERSION="AllWithIntegers"
if [ "$1" = "--lua" ]; then
    VERSION=$2
    shift 2
fi

while [ $# -gt 0 ]; do
    FILE_DIR=$(dirname "$1")
    FILE_NAME=$(basename -s.lua "$1")
    shift 1

    for trash in "$FILE_DIR/obj/$FILE_NAME".*; do
        if [ -e "$trash" ]; then
            rm "$trash"
        fi
    done

    if [ "$OPTIMIZE" = true ]; then
        echo "$FILE_DIR/$FILE_NAME.lua (Release):"
        dotnet run -c Debug -v quiet -- -Od --lua "$VERSION"  "$FILE_DIR/$FILE_NAME.lua" 2>&1 | sed -e 's/^/  build: /'
        # dotnet "$FILE_DIR/$FILE_NAME.dll" 2>&1 | sed -e 's/^/  run: /'
    else
        echo "$FILE_DIR/$FILE_NAME.lua (Debug):"
        dotnet run -c Debug -v quiet -- -d --lua "$VERSION" "$FILE_DIR/$FILE_NAME.lua" 2>&1 | sed -e 's/^/  build: /'
        # dotnet "$FILE_DIR/$FILE_NAME.dll" 2>&1 | sed -e 's/^/  run: /'
    fi
done
