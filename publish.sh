#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT="$SCRIPT_DIR/DnDManager/DnDManager.csproj"
OUTPUT_DIR="$SCRIPT_DIR/publish"
RIDS=("linux-x64" "win-x64" "osx-x64" "osx-arm64")

echo "=== DnD Manager Multi-Platform Publish ==="
echo ""

# Clean previous output
if [ -d "$OUTPUT_DIR" ]; then
    echo "Cleaning previous publish output..."
    rm -rf "$OUTPUT_DIR"
fi

FAILED=()
SUCCEEDED=()

for rid in "${RIDS[@]}"; do
    echo ""
    echo "--- Publishing for $rid ---"
    if dotnet publish "$PROJECT" -c Release -r "$rid" -o "$OUTPUT_DIR/$rid" --self-contained true; then
        SUCCEEDED+=("$rid")
    else
        echo "FAILED: $rid"
        FAILED+=("$rid")
    fi
done

echo ""
echo "=== Publish Summary ==="
for rid in "${SUCCEEDED[@]}"; do
    echo "  OK: $rid -> $OUTPUT_DIR/$rid/"
done
for rid in "${FAILED[@]}"; do
    echo "  FAILED: $rid"
done

if [ ${#FAILED[@]} -gt 0 ]; then
    echo ""
    echo "${#FAILED[@]} target(s) failed."
    exit 1
else
    echo ""
    echo "All ${#SUCCEEDED[@]} targets published successfully."
fi
