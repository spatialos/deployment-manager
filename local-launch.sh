#!/usr/bin/env bash
set -e -u -x -o pipefail

cd "$(dirname "$0")"

spatial alpha local launch --main_config=config/spatialos.json --launch_config=config/deployment.json --snapshot=config/empty.snapshot --log_directory=./logs --log_level=debug
