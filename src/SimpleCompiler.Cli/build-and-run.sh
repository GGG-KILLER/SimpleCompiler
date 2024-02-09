#! /usr/bin/env bash
set -euo pipefail

while [ $# -gt 0 ]; do
    FILE="$1"
    shift 1

    rm "${FILE/.lua/}".{dll,*.mir,cil,runtimeconfig.json,*.lua} || true;
    echo "$FILE:"
    dotnet run -c Debug -v quiet -- build -d "$FILE" 2>&1 | sed -e 's/^/  build: /'
    dotnet "${FILE/.lua/.dll}" 2>&1 | sed -e 's/^/  run: /'
done
