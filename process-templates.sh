#! /usr/bin/env nix-shell
#! nix-shell -i bash -p dotnetCorePackages.sdk_8_0
# shellcheck shell=bash
set -euo pipefail

if ! ret=$(dotnet tool install -g dotnet-t4 2>&1); then
    if [[ $ret == *"is already installed"* ]]; then
        :
    else
        echo "$ret"
        exit 1
    fi
fi

"$HOME/.dotnet/tools/t4" -p:Max=12 src/SimpleCompiler.Runtime/FunctionHelpers.tt
