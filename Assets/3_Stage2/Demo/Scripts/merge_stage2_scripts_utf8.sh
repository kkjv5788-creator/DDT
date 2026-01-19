#!/usr/bin/env bash
set -euo pipefail

SRC_IN="${1:-}"
OUT="${2:-}"

if [[ -z "${SRC_IN}" ]]; then
  echo "Usage:"
  echo "  bash merge_stage2_scripts_utf8.sh \"C:\\Users\\A1\\Documents\\GitHub\\asdf\\DDT\\Assets\\3_Stage2\\Demo\\Scripts\" [output.txt]"
  exit 1
fi

# default output name
if [[ -z "${OUT}" ]]; then
  OUT="Stage2_AllScripts_UTF8_$(date +%Y%m%d_%H%M%S).txt"
fi

# Convert Windows path to Git-Bash (MSYS) path if possible
if command -v cygpath >/dev/null 2>&1; then
  SRC="$(cygpath -u "$SRC_IN" 2>/dev/null || true)"
else
  SRC="$SRC_IN"
fi

if [[ ! -d "$SRC" ]]; then
  echo "Folder not found: $SRC"
  echo "Tip: path example: /c/Users/A1/Documents/GitHub/asdf/DDT/Assets/3_Stage2/Demo/Scripts"
  exit 1
fi

# Start fresh output (UTF-8)
: > "$OUT"

# PowerShell: read bytes -> detect BOM -> decode (UTF-8/UTF-16/UTF-32) -> fallback UTF-8 then CP949 -> print UTF-8 text
ps_read_as_text() {
  local win_path="$1"
  powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "
    \$p = '$win_path';
    \$bytes = [System.IO.File]::ReadAllBytes(\$p);

    function Decode-Bytes([byte[]]\$b) {
      if (\$b.Length -ge 3 -and \$b[0]-eq 0xEF -and \$b[1]-eq 0xBB -and \$b[2]-eq 0xBF) {
        return [System.Text.Encoding]::UTF8.GetString(\$b, 3, \$b.Length-3)
      }
      if (\$b.Length -ge 2 -and \$b[0]-eq 0xFF -and \$b[1]-eq 0xFE) {
        return [System.Text.Encoding]::Unicode.GetString(\$b, 2, \$b.Length-2) # UTF-16 LE
      }
      if (\$b.Length -ge 2 -and \$b[0]-eq 0xFE -and \$b[1]-eq 0xFF) {
        return [System.Text.Encoding]::BigEndianUnicode.GetString(\$b, 2, \$b.Length-2) # UTF-16 BE
      }
      if (\$b.Length -ge 4 -and \$b[0]-eq 0xFF -and \$b[1]-eq 0xFE -and \$b[2]-eq 0x00 -and \$b[3]-eq 0x00) {
        return [System.Text.Encoding]::UTF32.GetString(\$b, 4, \$b.Length-4) # UTF-32 LE
      }
      if (\$b.Length -ge 4 -and \$b[0]-eq 0x00 -and \$b[1]-eq 0x00 -and \$b[2]-eq 0xFE -and \$b[3]-eq 0xFF) {
        return [System.Text.Encoding]::GetEncoding('utf-32BE').GetString(\$b, 4, \$b.Length-4) # UTF-32 BE
      }

      # No BOM: try UTF-8 first
      \$t = [System.Text.Encoding]::UTF8.GetString(\$b)

      # Heuristic: if replacement chars exist, try CP949
      if (\$t -match '�') {
        try {
          \$cp949 = [System.Text.Encoding]::GetEncoding(949)
          \$t2 = \$cp949.GetString(\$b)
          # if cp949 looks better (fewer replacement chars), prefer it
          if ((\$t2 -split '�').Length -gt (\$t -split '�').Length) { return \$t }
          return \$t2
        } catch {
          return \$t
        }
      }
      return \$t
    }

    \$text = Decode-Bytes \$bytes;
    [Console]::OutputEncoding = [System.Text.Encoding]::UTF8;
    Write-Output \$text
  " 2>/dev/null
}

# Collect target files (원하면 확장자 추가/삭제 가능)
# Unity Stage2 Scripts 기준으로 .cs 위주 + 필요시 shader/compute/json/uxml/uss 등 포함
mapfile -t FILES < <(
  find "$SRC" -type f \( \
    -iname "*.cs" -o -iname "*.shader" -o -iname "*.compute" -o -iname "*.cginc" -o \
    -iname "*.json" -o -iname "*.uxml" -o -iname "*.uss" -o -iname "*.txt" \
  \) | sort
)

if [[ ${#FILES[@]} -eq 0 ]]; then
  echo "No script-like files found in: $SRC"
  echo "Edit extensions in the script if needed."
  exit 0
fi

# Merge
for f in "${FILES[@]}"; do
  # Convert msys path -> windows path for powershell.exe
  if command -v cygpath >/dev/null 2>&1; then
    WIN_F="$(cygpath -w "$f")"
  else
    WIN_F="$f"
  fi

  {
    echo "============================================================"
    echo "FILE: ${f}"
    echo "============================================================"
    ps_read_as_text "$WIN_F"
    echo
    echo
  } >> "$OUT"
done

echo "Done."
echo "Output: $(pwd)/$OUT"
