#!/usr/bin/env bash
# =============================================================
# TraceIoT — 第三方镜像离线保存脚本
#
# 用途：在有网络的机器上拉取并保存第三方镜像为 tar.gz 文件，
#       用于完全气隔离（air-gapped）内网环境的离线部署。
#
# 用法：
#   bash save-third-party-images.sh              # 保存当前机器架构
#   bash save-third-party-images.sh --all-arch   # 同时保存 amd64 + arm64
#   bash save-third-party-images.sh --arch arm64 # 只保存 arm64
#
# 输出文件（默认保存到脚本同目录）：
#   postgres-16-amd64.tar.gz  / postgres-16-arm64.tar.gz
#   redis-7-amd64.tar.gz      / redis-7-arm64.tar.gz
#   influxdb-2.7-amd64.tar.gz / influxdb-2.7-arm64.tar.gz
#   emqx-5.8-amd64.tar.gz    / emqx-5.8-arm64.tar.gz
# =============================================================
set -euo pipefail

# ── 颜色输出 ──────────────────────────────────────────────
GREEN='\033[0;32m'; YELLOW='\033[1;33m'; BLUE='\033[0;34m'
BOLD='\033[1m'; NC='\033[0m'
info()    { echo -e "${BLUE}[INFO]${NC}  $*"; }
success() { echo -e "${GREEN}[OK]${NC}    $*"; }
warn()    { echo -e "${YELLOW}[WARN]${NC}  $*"; }

# ── 第三方镜像列表 ────────────────────────────────────────
declare -A IMAGES=(
  ["postgres-16"]="postgres:16"
  ["redis-7"]="redis:7-alpine"
  ["influxdb-2.7"]="influxdb:2.7"
  ["emqx-5.8"]="emqx:5.8"
)

# ── 参数解析 ──────────────────────────────────────────────
ALL_ARCH=false
ARCHS=()

# 默认使用当前机器架构
NATIVE_ARCH=$(uname -m)
case $NATIVE_ARCH in
  x86_64)  DEFAULT_ARCH="amd64" ;;
  aarch64) DEFAULT_ARCH="arm64" ;;
  *)       DEFAULT_ARCH="amd64" ;;
esac

i=1
while [ $i -le $# ]; do
  arg="${!i}"
  case $arg in
    --all-arch)
      ALL_ARCH=true
      ARCHS=("amd64" "arm64")
      ;;
    --arch)
      i=$((i+1))
      ARCHS=("${!i}")
      ;;
    *)
      warn "未知参数：$arg"
      ;;
  esac
  i=$((i+1))
done

if [ ${#ARCHS[@]} -eq 0 ]; then
  ARCHS=("$DEFAULT_ARCH")
fi

OUTPUT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

echo -e "${BOLD}============================================${NC}"
echo -e "${BOLD}  TraceIoT 第三方镜像离线保存工具${NC}"
echo -e "${BOLD}============================================${NC}"
info "目标架构：${ARCHS[*]}"
info "保存路径：${OUTPUT_DIR}"
echo ""

# ── 拉取并保存 ────────────────────────────────────────────
total=0
saved=0

for ARCH in "${ARCHS[@]}"; do
  for NAME in "${!IMAGES[@]}"; do
    IMAGE="${IMAGES[$NAME]}"
    OUTPUT="${OUTPUT_DIR}/${NAME}-${ARCH}.tar.gz"
    total=$((total+1))

    # 如果文件已存在则跳过
    if [ -f "$OUTPUT" ]; then
      warn "${OUTPUT} 已存在，跳过（删除文件可重新下载）"
      saved=$((saved+1))
      continue
    fi

    info "拉取 ${IMAGE} (${ARCH})..."
    docker pull --platform "linux/${ARCH}" "$IMAGE"

    info "保存为 ${OUTPUT}..."
    docker save "$IMAGE" | gzip > "$OUTPUT"

    SIZE=$(du -sh "$OUTPUT" | cut -f1)
    success "${NAME}-${ARCH}.tar.gz 已保存（${SIZE}）"
    saved=$((saved+1))
  done
done

echo ""
echo -e "${BOLD}============================================${NC}"
success "完成！共保存 ${saved}/${total} 个镜像包"
echo -e "${BOLD}============================================${NC}"
echo ""
echo "传输到离线服务器后，将这些 tar.gz 文件与 load-and-start.sh 放在同一目录，"
echo "执行 bash load-and-start.sh 即可自动加载。"
