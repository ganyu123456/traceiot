#!/usr/bin/env bash
# =============================================================
# TraceIoT — 离线一键部署脚本
# 用法：将 offline-deploy.tar.gz 解压后执行本脚本
#   bash load-and-start.sh
# 可选参数：
#   --skip-third-party   跳过第三方镜像检查（已在线或已手动加载）
#   --no-start           只加载镜像，不启动服务
# =============================================================
set -euo pipefail

SKIP_THIRD_PARTY=false
NO_START=false

for arg in "$@"; do
  case $arg in
    --skip-third-party) SKIP_THIRD_PARTY=true ;;
    --no-start)         NO_START=true ;;
  esac
done

# ── 颜色输出 ──────────────────────────────────────────────
RED='\033[0;31m'; GREEN='\033[0;32m'; YELLOW='\033[1;33m'
BLUE='\033[0;34m'; BOLD='\033[1m'; NC='\033[0m'
info()    { echo -e "${BLUE}[INFO]${NC}  $*"; }
success() { echo -e "${GREEN}[OK]${NC}    $*"; }
warn()    { echo -e "${YELLOW}[WARN]${NC}  $*"; }
error()   { echo -e "${RED}[ERROR]${NC} $*" >&2; }

# ── 前置检查 ──────────────────────────────────────────────
echo -e "${BOLD}============================================${NC}"
echo -e "${BOLD}  TraceIoT 离线部署脚本${NC}"
echo -e "${BOLD}============================================${NC}"

if ! command -v docker &>/dev/null; then
  error "未找到 docker，请先安装 Docker Engine"
  exit 1
fi

if ! docker compose version &>/dev/null; then
  error "未找到 docker compose（需要 Docker Compose V2）"
  exit 1
fi

# ── 检测 CPU 架构 ─────────────────────────────────────────
ARCH=$(uname -m)
case $ARCH in
  x86_64)  SUFFIX="amd64" ;;
  aarch64) SUFFIX="arm64" ;;
  armv7l)  SUFFIX="arm64"; warn "armv7l 将尝试加载 arm64 镜像" ;;
  *)
    error "不支持的架构: $ARCH（仅支持 x86_64 / aarch64）"
    exit 1
    ;;
esac
info "检测到 CPU 架构：${BOLD}${ARCH}${NC}（使用 ${SUFFIX} 镜像包）"

# ── 加载自定义镜像 ────────────────────────────────────────
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

load_image() {
  local file="$1"
  local label="$2"
  if [ -f "$file" ]; then
    info "正在加载 ${label} 镜像：${file}"
    docker load -i "$file"
    success "${label} 镜像加载完成"
  else
    warn "未找到 ${label} 镜像文件：${file}，将尝试从镜像仓库拉取"
  fi
}

load_image "${SCRIPT_DIR}/traceiot-backend-${SUFFIX}.tar.gz"  "后端"
load_image "${SCRIPT_DIR}/traceiot-frontend-${SUFFIX}.tar.gz" "前端"

# ── 处理第三方镜像 ────────────────────────────────────────
if [ "$SKIP_THIRD_PARTY" = false ]; then
  info "检查第三方镜像..."

  ensure_image() {
    local image="$1"
    local tar_pattern="$2"

    # 优先从本地 tar 文件加载
    local tar_file
    tar_file=$(find "${SCRIPT_DIR}" -maxdepth 1 -name "${tar_pattern}-${SUFFIX}.tar.gz" 2>/dev/null | head -1)
    if [ -n "$tar_file" ]; then
      info "从本地文件加载：${tar_file}"
      docker load -i "$tar_file"
      return
    fi

    # 检查镜像是否已存在
    if docker image inspect "$image" &>/dev/null; then
      success "${image} 已存在，跳过"
      return
    fi

    # 在线拉取
    info "在线拉取 ${image}..."
    docker pull "$image"
    success "${image} 拉取成功"
  }

  ensure_image "postgres:16"    "postgres-16"
  ensure_image "redis:7-alpine" "redis-7"
  ensure_image "influxdb:2.7"   "influxdb-2.7"
  ensure_image "emqx:5.8"      "emqx-5.8"
fi

# ── 查找 docker-compose.yml ───────────────────────────────
COMPOSE_FILE="${SCRIPT_DIR}/docker-compose.yml"
if [ ! -f "$COMPOSE_FILE" ]; then
  # 向上查找
  COMPOSE_FILE="$(dirname "${SCRIPT_DIR}")/docker-compose.yml"
fi

if [ ! -f "$COMPOSE_FILE" ]; then
  error "未找到 docker-compose.yml，请确认脚本与 docker-compose.yml 在同一目录"
  exit 1
fi

info "使用配置文件：${COMPOSE_FILE}"

# ── 标记已加载的本地镜像 tag ─────────────────────────────
# 将 traceiot-backend:offline 重新 tag 为 compose 期望的格式
# （若 compose 使用 build: 字段会自动 build，这里适用于离线 image: 引用）
if docker image inspect traceiot-backend:offline &>/dev/null; then
  docker tag traceiot-backend:offline "ghcr.io/traceiot/traceiot-backend:latest" 2>/dev/null || true
fi
if docker image inspect traceiot-frontend:offline &>/dev/null; then
  docker tag traceiot-frontend:offline "ghcr.io/traceiot/traceiot-frontend:latest" 2>/dev/null || true
fi

# ── 启动服务 ──────────────────────────────────────────────
if [ "$NO_START" = false ]; then
  info "正在启动所有服务..."
  docker compose -f "$COMPOSE_FILE" up -d

  echo ""
  echo -e "${BOLD}============================================${NC}"
  success "TraceIoT 部署完成！"
  echo -e "${BOLD}============================================${NC}"

  # 获取本机 IP
  HOST_IP=$(hostname -I 2>/dev/null | awk '{print $1}' || echo "your-server-ip")

  echo ""
  echo -e "  ${BOLD}前端访问地址：${NC}  http://${HOST_IP}"
  echo -e "  ${BOLD}后端 API：${NC}      http://${HOST_IP}:5000/swagger"
  echo -e "  ${BOLD}EMQX 控制台：${NC}   http://${HOST_IP}:18083  (admin/public)"
  echo -e "  ${BOLD}InfluxDB：${NC}      http://${HOST_IP}:8086   (admin/traceiot123)"
  echo ""
  echo -e "  ${BOLD}默认登录账号：${NC}  admin / Admin@123456"
  echo ""
else
  success "镜像加载完成（--no-start 模式，未启动服务）"
fi
