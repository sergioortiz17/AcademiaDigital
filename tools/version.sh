#!/usr/bin/env bash
# Versión única de la plataforma (frontend, backend y Finance), con versionado semántico.
# Fuente de verdad: /VERSION. Este script la propaga y verifica que todo coincida.
#
#   tools/version.sh             muestra la versión actual
#   tools/version.sh check       falla si algún archivo no coincide con /VERSION (lo usa CodeBuild)
#   tools/version.sh set 1.2.0   cambia la versión en todos los archivos
#
# Ver VERSIONADO.md para cuándo subir MAJOR, MINOR o PATCH.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
VERSION_FILE="$ROOT/VERSION"
PACKAGE_JSON="$ROOT/frontend-angular/package.json"
PROPS_FILES=(
  "$ROOT/backend-dotnet/src/Directory.Build.props"
  "$ROOT/services/AcademiaDigital.Finance/src/Directory.Build.props"
)
SEMVER_RE='^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)(-[0-9A-Za-z.-]+)?$'

current() { tr -d '[:space:]' < "$VERSION_FILE"; }

package_version() {
  sed -n 's/^  "version": "\(.*\)",$/\1/p' "$PACKAGE_JSON" | head -1
}

props_version() {
  sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' "$1" | head -1
}

check() {
  local expected ok=0
  expected="$(current)"
  [[ "$expected" =~ $SEMVER_RE ]] || { echo "ERROR: /VERSION no es SemVer: '$expected'" >&2; exit 1; }

  local pkg
  pkg="$(package_version)"
  if [[ "$pkg" != "$expected" ]]; then
    echo "ERROR: frontend-angular/package.json tiene $pkg y /VERSION $expected" >&2; ok=1
  fi
  for f in "${PROPS_FILES[@]}"; do
    local v
    v="$(props_version "$f")"
    if [[ "$v" != "$expected" ]]; then
      echo "ERROR: ${f#"$ROOT"/} tiene $v y /VERSION $expected" >&2; ok=1
    fi
  done
  [[ $ok -eq 0 ]] && echo "Versión $expected consistente en frontend, backend y Finance."
  return $ok
}

set_version() {
  local new="$1"
  [[ "$new" =~ $SEMVER_RE ]] || { echo "ERROR: '$new' no es una versión SemVer (X.Y.Z)" >&2; exit 1; }

  echo "$new" > "$VERSION_FILE"
  # npm actualiza package.json y package-lock.json sin crear commit ni tag.
  (cd "$ROOT/frontend-angular" && npm version "$new" --no-git-tag-version --allow-same-version >/dev/null)
  for f in "${PROPS_FILES[@]}"; do
    sed -i "s:<Version>.*</Version>:<Version>$new</Version>:" "$f"
  done
  check
  echo "Siguiente: agregar la sección [$new] en CHANGELOG.md y abrir el PR."
}

case "${1:-}" in
  "")    current; echo ;;
  check) check ;;
  set)   [[ -n "${2:-}" ]] || { echo "Uso: $0 set X.Y.Z" >&2; exit 1; }; set_version "$2" ;;
  *)     echo "Uso: $0 [check | set X.Y.Z]" >&2; exit 1 ;;
esac
