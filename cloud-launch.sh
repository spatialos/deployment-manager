#!/usr/bin/env bash
set -e -u -x -o pipefail

cd "$(dirname "$0")"

if [ "$#" -ne 2 ]; then
    echo "Wrong number of arguments. Usage: <deployment manager assembly name> <deployment manager deployment name>"
fi

ASSEMBLY_NAME="$1"
DEPLOYMENT_NAME="$2"

spatial alpha cloud upload --assembly_name=${ASSEMBLY_NAME} --main_config=config/spatialos.json --force
spatial alpha cloud launch --assembly_name=${ASSEMBLY_NAME} --deployment_name=${DEPLOYMENT_NAME} --main_config=config/spatialos.json --launch_config=config/deployment.json --snapshot=config/empty.snapshot
