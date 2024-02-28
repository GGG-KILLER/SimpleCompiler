#! /usr/bin/env bash
set -euo pipefail

_RUNTIME_PATH=$(dirname "$(realpath "$(which dotnet)")")
for appDir in "$_RUNTIME_PATH"/shared/Microsoft.NETCore.App/8.*; do
    _RUNTIME_PATH="$appDir"
    break
done
VERSION="AllWithIntegers"
FLAGS=()
FILES=()

DOTNET_TOOL_COMMAND="install"
if [ -e "$HOME/.dotnet/tools/ilverify" ]; then
    DOTNET_TOOL_COMMAND="update"
fi
dotnet tool $DOTNET_TOOL_COMMAND --global dotnet-ilverify

DOT=""
# shellcheck disable=SC2016
if DOT=$(nix-shell -p graphviz --run 'realpath $(which dot)'); then
    :;
fi

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
        dotnet run -c Debug -v quiet -- --lua "$VERSION" "${FLAGS[@]}" "$FILE_DIR/$FILE_NAME.lua" 2>&1 | sed -e 's/^/  build:    /'

        if [ -n "$DOT" ]; then
            for f in "$FILE_DIR/obj/$FILE_NAME".*.dot; do
                "$DOT" -Tsvg "$f" > "${f/.dot/.svg}";
            done
        fi

        "$HOME/.dotnet/tools/ilverify" -ct \
            -r "$_RUNTIME_PATH"'/*.dll' \
            -r "$FILE_DIR/SimpleCompiler.Runtime.dll" \
            "$FILE_DIR/$FILE_NAME.dll" 2>&1 | sed -e 's/^/  validate: /'

        dotnet "$FILE_DIR/$FILE_NAME.dll" 2>&1 | sed -e 's/^/  run:     /'
    } || true
done
