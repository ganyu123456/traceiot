# TraceIoT — 物联网 GPS 定位轨迹管理平台

> 基于 .NET 8 + ABP Framework 后端、React + Ant Design 前端的完整 IoT 轨迹追踪系统，支持 MQTT 协议接入、实时位置展示、历史轨迹回放与告警管理。

---

## 架构概览

```
┌─────────────────────────────────────────────────────────────┐
│                      前端（React SPA）                        │
│         Ant Design · HighCharts · 百度地图 JS API             │
│                     http://your-ip:80                        │
└─────────────────────┬───────────────────────────────────────┘
                      │  HTTP /api/（Nginx 反代）
┌─────────────────────▼───────────────────────────────────────┐
│                   后端（.NET 8 / ABP）                        │
│          RESTful API · MqttWorker · EF Core · AutoMapper     │
│                     http://your-ip:5000                      │
└──────┬──────────────┬─────────────────┬──────────────────────┘
       │              │                 │
┌──────▼──────┐ ┌────▼──────┐ ┌───────▼──────┐
│ PostgreSQL  │ │   Redis   │ │   InfluxDB   │
│  结构化数据  │ │ 实时位置  │ │  时序轨迹数据 │
│   :5432     │ │  :6379    │ │    :8086     │
└─────────────┘ └───────────┘ └──────────────┘
                                      ↑  MQTT Subscribe
┌─────────────────────────────────────┘
│                  EMQX Broker
│         MQTT :1883 / WS :8083 / Dashboard :18083
└─────────────────────────────────────────────────
                      ↑  MQTT Publish
┌─────────────────────┘
│   GPS 设备（CAT1 模块）/ Python 模拟器
└─────────────────────────────────────────────────
```

---

## 技术栈

| 层次 | 技术 |
|------|------|
| 后端框架 | .NET 8 · ABP Framework（DDD 分层） |
| 消息队列 | EMQX 5.8（MQTT Broker） |
| 结构化存储 | PostgreSQL 16 |
| 缓存 / 实时 | Redis 7 |
| 时序存储 | InfluxDB 2.7 |
| 前端框架 | React 18 · Vite · TypeScript |
| UI 组件库 | Ant Design 5 |
| 状态管理 | Zustand |
| 图表 | HighCharts |
| 地图 | 百度地图 JS API |
| 容器化 | Docker · Docker Compose |
| CI/CD | GitHub Actions · GHCR |
| 模拟器 | Python 3 · paho-mqtt |

---

## 快速启动（在线部署）

### 前提条件

- Docker Engine ≥ 24.0
- Docker Compose V2（`docker compose version` 验证）
- 端口 80、5000、1883、5432、6379、8086、18083 未被占用

### 三步部署

```bash
# 1. 克隆仓库
git clone https://github.com/your-org/traceiot.git
cd traceiot

# 2. 启动所有服务（首次运行会自动拉取镜像）
docker compose up -d

# 3. 查看服务状态
docker compose ps
```

访问前端：**http://your-server-ip**
默认账号：`admin` / `Admin@123456`

---

## 离线部署

适用于无外网访问的内网服务器。

### 方式一：使用 Release 包（推荐）

1. 前往 [GitHub Releases](https://github.com/your-org/traceiot/releases) 下载最新的 `offline-deploy.tar.gz`

2. 将文件上传到目标服务器并解压：
   ```bash
   tar -xzf offline-deploy.tar.gz
   cd offline-deploy
   ```

3. 执行部署脚本（自动检测 CPU 架构）：
   ```bash
   bash load-and-start.sh
   ```
   脚本会：
   - 自动识别 amd64 / arm64 架构
   - 加载对应的自定义镜像包
   - 尝试加载同目录下的第三方镜像 tar，如不存在则在线拉取
   - 执行 `docker compose up -d`

### 方式二：完全气隔离（无网络）

在联网机器上预先保存所有第三方镜像：

```bash
# 同时保存 amd64 和 arm64 的第三方镜像
bash scripts/save-third-party-images.sh --all-arch

# 只保存当前架构
bash scripts/save-third-party-images.sh

# 只保存 arm64
bash scripts/save-third-party-images.sh --arch arm64
```

然后将所有 `.tar.gz` 文件连同 `load-and-start.sh` 和 `docker-compose.yml` 一起传到目标服务器。

---

## 端口映射

| 端口 | 服务 | 说明 |
|------|------|------|
| 80 | 前端 (Nginx) | Web 管理界面 |
| 5000 | 后端 API | REST API + Swagger |
| 1883 | EMQX MQTT | GPS 设备接入 |
| 8083 | EMQX WS | MQTT over WebSocket |
| 18083 | EMQX Dashboard | 消息代理管理界面 |
| 5432 | PostgreSQL | 数据库（生产环境可不对外暴露）|
| 6379 | Redis | 缓存（生产环境可不对外暴露）|
| 8086 | InfluxDB | 时序数据库 |

---

## 环境变量说明

`docker-compose.yml` 中后端服务支持以下环境变量覆盖：

| 环境变量 | 默认值 | 说明 |
|---------|--------|------|
| `ConnectionStrings__Default` | PostgreSQL 连接串 | 数据库地址 |
| `Redis__Configuration` | `traceiot-redis:6379` | Redis 地址 |
| `InfluxDB__Url` | `http://traceiot-influxdb:8086` | InfluxDB 地址 |
| `InfluxDB__Token` | `traceiot-influxdb-token-2024` | InfluxDB Token（生产环境请修改）|
| `Mqtt__Host` | `traceiot-emqx` | MQTT Broker 地址 |
| `Jwt__Key` | 默认密钥 | JWT 签名密钥（**生产环境必须修改**）|
| `App__CorsOrigins` | `http://localhost` | 允许的前端 CORS 来源 |

---

## 高德 / 百度地图 Key 配置

前端默认使用百度地图 JS API，需要申请 API Key：

1. 前往 [百度地图开放平台](https://lbsyol.baidu.com/) 申请应用 AK
2. 修改 `frontend/src/pages/RealtimeMap/RealtimeMap.tsx` 中的 `ak` 参数：
   ```typescript
   const MAP_AK = 'YOUR_BAIDU_MAP_AK_HERE';
   ```
3. 重新构建或重启服务

---

## GPS 模拟器

用于在没有真实 GPS 设备时模拟多辆车沿北京-天津高速路线发送轨迹数据。

### 快速启动

```bash
cd simulator
pip install -r requirements.txt

# 启动模拟器（默认连接本机 EMQX）
python simulator.py

# 自定义参数
MQTT_HOST=your-server-ip \
MQTT_PORT=1883 \
DEVICE_COUNT=5 \
PUBLISH_INTERVAL=3 \
python simulator.py
```

### 配置参数

| 参数 | 默认值 | 说明 |
|------|--------|------|
| `MQTT_HOST` | `127.0.0.1` | MQTT Broker 地址 |
| `MQTT_PORT` | `1883` | MQTT 端口 |
| `DEVICE_COUNT` | `3` | 模拟设备数量 |
| `PUBLISH_INTERVAL` | `5` | 上报间隔（秒）|
| `SPEED_KMH` | `80` | 模拟车速（km/h）|

### MQTT Topic 格式

```
gps/{device_id}
```

上报 JSON 格式：
```json
{
  "deviceId": "SIM-001",
  "latitude": 39.9042,
  "longitude": 116.4074,
  "speed": 80.5,
  "heading": 120,
  "altitude": 50,
  "timestamp": "2024-01-01T12:00:00Z",
  "signalStrength": -75,
  "batteryLevel": 85
}
```

---

## CI/CD 工作流

### PR 编译检查（ci.yml）

- 触发：推送到 `main` / `develop` 分支或 Pull Request
- 并行执行后端（.NET 8）和前端（Node 20）编译检查

### 发布流水线（release.yml）

- 触发：推送版本标签（如 `git tag v1.0.0 && git push --tags`）
- 构建 `amd64` + `arm64` 双架构 Docker 镜像
- 推送多架构 Manifest 到 GitHub Container Registry (GHCR)
- 生成离线镜像包并创建 GitHub Release

```bash
# 发布新版本
git tag v1.0.0
git push origin v1.0.0
```

---

## 目录结构

```
traceiot/
├── backend/                    # .NET 8 后端
│   ├── TraceIot.sln
│   └── src/
│       ├── TraceIot.Domain.Shared/   # 枚举、常量
│       ├── TraceIot.Domain/          # 领域实体、仓储接口
│       ├── TraceIot.Application/     # 应用服务
│       ├── TraceIot.Application.Contracts/  # DTO、接口
│       ├── TraceIot.EntityFrameworkCore/    # EF Core 实现
│       ├── TraceIot.HttpApi/         # Controller 层
│       ├── TraceIot.HttpApi.Host/    # 启动项目（入口）
│       └── TraceIot.MqttWorker/      # MQTT 后台服务
├── frontend/                   # React 前端
│   ├── src/
│   │   ├── api/                # Axios 接口封装
│   │   ├── pages/              # 页面组件
│   │   ├── store/              # Zustand 状态
│   │   └── router/             # React Router v6
│   ├── Dockerfile
│   └── nginx.conf
├── simulator/                  # Python GPS 模拟器
│   ├── simulator.py
│   ├── device.py
│   ├── mqtt_client.py
│   └── requirements.txt
├── scripts/
│   ├── load-and-start.sh       # 离线部署脚本
│   └── save-third-party-images.sh  # 第三方镜像打包脚本
├── docs/
│   └── db-schema.sql           # 数据库初始化脚本
├── .github/
│   └── workflows/
│       ├── ci.yml              # PR 编译检查
│       └── release.yml         # 发布流水线
├── Dockerfile                  # 后端镜像构建
├── docker-compose.yml          # 一键部署配置
└── README.md
```

---

## 常用命令

```bash
# 查看所有服务状态
docker compose ps

# 查看后端日志
docker compose logs -f backend

# 查看前端日志
docker compose logs -f frontend

# 重启单个服务
docker compose restart backend

# 停止所有服务（保留数据）
docker compose stop

# 完全清除（含数据卷！）
docker compose down -v

# 本地重新构建并启动
docker compose up -d --build
```

---

## License

MIT
