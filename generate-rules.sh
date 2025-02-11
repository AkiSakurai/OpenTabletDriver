#!/usr/bin/env bash

set -eu

SRC_ROOT=$(readlink -f $(dirname ${BASH_SOURCE[0]}))
[ ! -d "${SRC_ROOT}" ] && exit 100;

PROJECT="${SRC_ROOT}/OpenTabletDriver.Tools.udev"

TABLET_CONFIGURATIONS="${SRC_ROOT}/OpenTabletDriver.Configurations/Configurations"
RULES_FILE="${SRC_ROOT}/bin/99-opentabletdriver.rules"

if [ "$#" -gt 0 ]; then
  # Pass arguments to utility instead of using defaults
  dotnet_args=($@)
else
  [ ! -d "${TABLET_CONFIGURATIONS}" ] && exit 101;
  dotnet_args=("-v" "${TABLET_CONFIGURATIONS}" "${RULES_FILE}")
fi

echo "Generating udev rules..."

dotnet run --project "${PROJECT}" -- ${dotnet_args[@]}
