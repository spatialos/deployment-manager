#!/usr/bin/env bash
set -e -u -x -o pipefail

cd "$(dirname "$0")"

if [ "$#" -ne 2 ]; then
    echo "Wrong number of arguments. Usage: <SpatialOS project name> <service account token lifetime in days>"
fi

PROJECT_NAME="$1"
TOKEN_LIFETIME="$2"

dotnet build -c Release -p:Platform=x64 ServiceAccountGenerator/ServiceAccountGenerator.csproj
dotnet run --project ServiceAccountGenerator/ "$(pwd)/DeploymentManager" ${PROJECT_NAME} ${TOKEN_LIFETIME}