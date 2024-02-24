#! /usr/bin/env bash
set -euo pipefail

VERSION="AllWithIntegers"
FLAGS=()
FILES=()

while [ "$#" -gt 0 ]; do
    case "$1" in
    --lua)
        VERSION="$2"
        shift 2
        ;;
    --lua=*)
        VERSION="${1#--lua=}"
        shift 1
        ;;
    --*=*)
        FLAGS+=("$1")
        shift 1
        ;;
    --*)
        FLAGS+=("$1")
        FLAGS+=("$2")
        shift 2
        ;;
    -*)
        FLAGS+=("$1")
        shift 1
        ;;
    *)
        FILES+=("$1")
        shift 1
        ;;
    esac
done

echo Version: "$VERSION"

for FILE in "${FILES[@]}"; do
    FILE_DIR=$(dirname "$FILE")
    FILE_NAME=$(basename -s.lua "$FILE")

    for trash in "$FILE_DIR/obj/$FILE_NAME".*; do
        if [ -e "$trash" ]; then
            rm "$trash"
        fi
    done

    {
        echo "$FILE_DIR/$FILE_NAME.lua:"
        dotnet run -c Debug -v quiet -- --lua "$VERSION" "${FLAGS[@]}" "$FILE_DIR/$FILE_NAME.lua" 2>&1 | sed -e 's/^/  build: /'
        dotnet "$FILE_DIR/$FILE_NAME.dll" 2>&1 | sed -e 's/^/  run: /'
    } || true
done
