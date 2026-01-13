#!/usr/bin/env bash
set -euo pipefail

SRC_IN="${1:-}"
OUT="${2:-}"

if [[ -z "${SRC_IN}" ]]; then
  echo "Usage: bash merge_scripts_utf8.sh \"<source_folder>\" [output_file]"
  exit 1
fi

if [[ -z "${OUT}" ]]; then
  OUT="AllScripts_UTF8_$(date +%Y%m%d_%H%M%S).txt"
fi

if command -v cygpath >/dev/null 2>&1; then
  SRC="$(cygpath -u "$SRC_IN" 2>/dev/null || true)"
else
  SRC="$SRC_IN"
fi

if [[ ! -d "$SRC" ]]; then
  echo "Folder not found: $SRC"
  echo "Tip: try path like /d/git\\ hub/DDT/Assets/2_Stage1/Demo/Scripts"
  exit 1
fi

: > "$OUT"

ps_cat_utf8() {
  local unix_path="$1"
  local win_path
  if command -v cygpath >/dev/null 2>&1; then
    win_path="$(cygpath -w "$unix_path")"
  else
    win_path="$unix_path"
  fi

  powershell.exe -NoProfile -Command \
    "\$OutputEncoding = New-Object System.Text.UTF8Encoding(\$false); [Console]::OutputEncoding = \$OutputEncoding; Get-Content -Raw -LiteralPath '$win_path'"
}

find "$SRC" -type f \( -iname "*.cs" \) -print0 \
| sort -z \
| while IFS= read -r -d '' file; do
    rel="${file#$SRC/}"

    {
      echo ""
      echo "/* ====================================================================="
      echo "   FILE: $rel"
      echo "   ===================================================================== */"
      echo ""
    } >> "$OUT"

    ps_cat_utf8 "$file" >> "$OUT"
    echo "" >> "$OUT"
  done

echo "DONE -> $OUT"
