#!/usr/bin/env bash
#
# el — Eleven Legends CLI
#
# Usage:
#   ./el              Start dev server
#   ./el setup        Install all dependencies
#   ./el dev          Start dev server
#   ./el build        Production build
#   ./el test         Run tests
#   ./el lint         Lint code
#   ./el help         Show this help
#

set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
cd "$SCRIPT_DIR"

# Colors
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

header() {
  echo -e "${GREEN}⚽ Eleven Legends${NC} — $1"
  echo ""
}

ensure_mise() {
  if ! command -v mise &> /dev/null; then
    echo -e "${YELLOW}mise not found. Installing...${NC}"
    curl https://mise.jdx.dev/install.sh | sh
    eval "$(~/.local/bin/mise activate bash)"
  fi
}

ensure_node() {
  ensure_mise
  eval "$(mise activate bash 2>/dev/null)" || true
  if ! command -v node &> /dev/null; then
    echo -e "${YELLOW}Node.js not found. Running setup...${NC}"
    cmd_setup
  fi
}

ensure_deps() {
  ensure_node
  if [ ! -d "node_modules" ]; then
    echo -e "${YELLOW}Dependencies not installed. Running setup...${NC}"
    cmd_setup
  fi
}

cmd_setup() {
  header "Setup"

  ensure_mise
  eval "$(mise activate bash 2>/dev/null)" || true

  echo -e "${BLUE}Installing Node.js 22 via mise...${NC}"
  mise install

  echo -e "${BLUE}Installing pnpm...${NC}"
  npm install -g pnpm 2>/dev/null || true

  echo -e "${BLUE}Installing project dependencies...${NC}"
  pnpm install

  echo ""
  echo -e "${GREEN}✓ Setup complete!${NC}"
  echo -e "  Run ${BLUE}./el${NC} to start the dev server."
}

cmd_dev() {
  header "Dev Server"
  ensure_deps
  pnpm dev
}

cmd_build() {
  header "Production Build"
  ensure_deps
  pnpm build
}

cmd_test() {
  header "Tests"
  ensure_deps
  pnpm test
}

cmd_lint() {
  header "Lint"
  ensure_deps
  pnpm lint
}

cmd_help() {
  echo -e "${GREEN}⚽ Eleven Legends CLI${NC}"
  echo ""
  echo "Usage: ./el [command]"
  echo ""
  echo "Commands:"
  echo "  setup       Install Node.js, pnpm, and all dependencies"
  echo "  dev         Start development server (default)"
  echo "  build       Production build"
  echo "  test        Run tests"
  echo "  lint        Lint code"
  echo "  help        Show this help"
  echo ""
  echo "No command = dev server"
}

# Route command
case "${1:-dev}" in
  setup)  cmd_setup ;;
  dev)    cmd_dev ;;
  build)  cmd_build ;;
  test)   cmd_test ;;
  lint)   cmd_lint ;;
  help|-h|--help) cmd_help ;;
  *)
    echo -e "${RED}Unknown command: $1${NC}"
    echo ""
    cmd_help
    exit 1
    ;;
esac
