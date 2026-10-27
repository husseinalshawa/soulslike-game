#!/usr/bin/env bash
# Runs the game-logic test suite.
#
# In this cloud container the Unity Editor cannot be installed (network policy),
# so tests run through the LocalTests harness: the same test sources compiled
# with dotnet + NUnit against a thin UnityEngine shim. On a machine with Unity
# installed, the same tests also run in the Unity Test Runner (EditMode).
set -euo pipefail
cd "$(dirname "$0")/.."
exec dotnet test LocalTests/SoulsGame.LocalTests.csproj --nologo -v q "$@"
