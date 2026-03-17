#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SOURCE_DIR="$ROOT_DIR/docs/wiki"
DEFAULT_TARGET_DIR="$ROOT_DIR/../AskMkiM.wiki"
WIKI_REMOTE="https://github.com/GameShowLDA/AskMkiM.wiki.git"

TARGET_DIR=""
PUSH_AFTER_SYNC=0

usage() {
  cat <<'EOF'
Usage:
  scripts/sync-github-wiki.sh [target-dir] [--push]

Examples:
  scripts/sync-github-wiki.sh
  scripts/sync-github-wiki.sh ../AskMkiM.wiki
  scripts/sync-github-wiki.sh ../AskMkiM.wiki --push
EOF
}

for arg in "$@"; do
  case "$arg" in
    --push)
      PUSH_AFTER_SYNC=1
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      if [ -z "$TARGET_DIR" ]; then
        TARGET_DIR="$arg"
      else
        echo "Unexpected argument: $arg" >&2
        exit 1
      fi
      ;;
  esac
done

TARGET_DIR="${TARGET_DIR:-$DEFAULT_TARGET_DIR}"

if [ ! -d "$SOURCE_DIR" ]; then
  echo "Source directory not found: $SOURCE_DIR" >&2
  exit 1
fi

PAGES=(
  "01-solution-overview.md"
  "02-project-dependencies.md"
  "03-startup-lifecycle.md"
  "04-ui-workspace.md"
  "05-command-language-and-translation.md"
  "06-execution-engine.md"
  "07-metrology-and-hardware-tests.md"
  "08-devices-and-communication.md"
  "09-database-and-settings.md"
  "10-support-systems.md"
  "11-extension-points.md"
  "12-command-executor-algorithms.md"
)

convert_markdown() {
  local src="$1"
  local dst="$2"

  perl -0pe '
    s{\]\(\./README\.md\)}{](Home)}g;
    s{\]\(\./([^)]+)\.md\)}{]($1)}g;
  ' "$src" > "$dst"
}

mkdir -p "$TARGET_DIR"

if [ ! -d "$TARGET_DIR/.git" ]; then
  git init "$TARGET_DIR" >/dev/null
fi

if ! git -C "$TARGET_DIR" remote get-url origin >/dev/null 2>&1; then
  git -C "$TARGET_DIR" remote add origin "$WIKI_REMOTE"
fi

convert_markdown "$SOURCE_DIR/README.md" "$TARGET_DIR/Home.md"

for page in "${PAGES[@]}"; do
  convert_markdown "$SOURCE_DIR/$page" "$TARGET_DIR/$page"
done

cat > "$TARGET_DIR/_Sidebar.md" <<'EOF'
# AskMkiM

- [Главная](Home)
- [Обзор решения](01-solution-overview)
- [Зависимости проектов](02-project-dependencies)
- [Запуск и жизненный цикл](03-startup-lifecycle)
- [UI и рабочее пространство](04-ui-workspace)
- [Язык команд и трансляция](05-command-language-and-translation)
- [Исполнение программ контроля](06-execution-engine)
- [Метрология и проверки оборудования](07-metrology-and-hardware-tests)
- [Устройства и коммуникация](08-devices-and-communication)
- [База данных и настройки](09-database-and-settings)
- [Служебные подсистемы](10-support-systems)
- [Точки расширения](11-extension-points)
- [Алгоритмы исполнителей команд](12-command-executor-algorithms)
EOF

git -C "$TARGET_DIR" add Home.md _Sidebar.md "${PAGES[@]}"

if git -C "$TARGET_DIR" diff --cached --quiet; then
  echo "Wiki is already up to date in $TARGET_DIR"
else
  git -C "$TARGET_DIR" commit -m "Update wiki from docs/wiki" >/dev/null
  echo "Created local wiki commit in $TARGET_DIR"
fi

if [ "$PUSH_AFTER_SYNC" -eq 1 ]; then
  echo "Pushing wiki to $WIKI_REMOTE"
  git -C "$TARGET_DIR" push -u origin HEAD:master
fi
