#!/usr/bin/env bash
set -e -u -x -o pipefail

cd "$(dirname "$0")"

if [ "$#" -ne 2 ]; then
    echo "Wrong number of arguments. Usage: <game deployment launch config path> <game deployment snapshot path>"
fi

LAUNCH_CONFIG_PATH="$1"
SNAPSHOT_PATH="$2"

cp ${LAUNCH_CONFIG_PATH}  "$(pwd)/DeploymentManager/launch_config.json"
cp ${SNAPSHOT_PATH}  "$(pwd)/DeploymentManager/default.snapshot"

dotnet build -c Release -p:Platform=x64 GeneratedCode/GeneratedCode.csproj
dotnet publish DeploymentManager/DeploymentManager.csproj -r osx-x64 -c Release -p:Platform=x64 --self-contained