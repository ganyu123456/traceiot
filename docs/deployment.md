# TraceIoT 部署运行指南

## 环境要求

| 组件 | 版本要求 |
|------|---------|
| Docker + Docker Compose | 24.x+ |
| .NET SDK | 8.0+ |
| Node.js | 20+ |
| Python | 3.11+ |

---

## 一、启动基础设施（Docker Compose）

```bash
# 在项目根目录执行
docker compose up -d
```

启动后各服务端口：

| 服务 | 端口 | 说明 |
|------|------|------|
| PostgreSQL | 5432 | 数据库 |
| Redis | 6379 | 缓存 |
| InfluxDB | 8086 | 时序数据库，Web UI: http://localhost:8086 |
| EMQX MQTT | 1883 | MQTT Broker |
| EMQX Dashboard | 18083 | 管理控制台（默认 admin/public） |

等待所有容器健康（约 15-30 秒）：
```bash
docker compose ps
```

---

## 二、初始化 InfluxDB

InfluxDB 通过 docker-compose 环境变量已自动初始化：
- 组织：`traceiot`
- Bucket：`gps`
- Token：`traceiot-influxdb-token-2024`
- 用户名/密码：`admin / traceiot123`

验证访问：http://localhost:8086（用上述账号登录）

---

## 三、启动后端（.NET 8 ABP）

```bash
cd backend

# 还原 NuGet 依赖（首次）
# 注意：如果本机 ~/.nuget/packages 有 root 所有者的包导致权限错误，使用以下命令
mkdir -p /tmp/nuget-packages
NUGET_PACKAGES=/tmp/nuget-packages dotnet restore
# 正常情况直接：
dotnet restore

# 启动（自动执行 EF Core Migration + 创建 admin 用户）
dotnet run --project src/TraceIot.HttpApi.Host
# 若有 NuGet 权限问题：
NUGET_PACKAGES=/tmp/nuget-packages dotnet run --project src/TraceIot.HttpApi.Host
```

**首次启动说明：**
- 自动连接 PostgreSQL 并执行 EF Core Migration（创建 ABP 框架所需的所有表）
- 自动创建默认用户：`admin / Admin@123456`，角色 `admin`

后端 API 地址：http://localhost:5000
Swagger 文档：http://localhost:5000/swagger

---

## 四、启动前端（React + Vite）

```bash
cd frontend

# 安装依赖（首次）
npm install

# 开发模式启动
npm run dev
```

前端地址：http://localhost:5173
默认登录账号：`admin / Admin@123456`

### 配置高德地图 Key

在两个文件中替换 `your-amap-key-here`：
- `src/pages/RealtimeMap/index.tsx`
- `src/pages/TrackReplay/index.tsx`

申请地址：https://console.amap.com/dev/key/app

---

## 五、启动 GPS 模拟器

```bash
cd simulator

# 安装依赖
pip install -r requirements.txt

# 启动（默认3台设备）
python simulator.py

# 自定义参数
python simulator.py -n 5              # 5台设备
python simulator.py --speed 80        # 80 km/h
python simulator.py --interval 2      # 2秒上报一次
python simulator.py --host 192.168.x.x  # 指定 MQTT Broker 地址
```

模拟器控制台输出示例：
```
10:30:15 [SIM-001] ✓ | 39.904200,116.507400 | 100.0 km/h | 南 | 进度 12.5%
10:30:15 [SIM-002] ✓ | 39.627200,116.883000 | 100.0 km/h | 南 | 进度 42.5%
```

---

## 六、整体启动顺序

```
1. docker compose up -d          # 启动 PG + Redis + InfluxDB + EMQX
2. dotnet run ...                 # 后端服务（含 MqttWorker 订阅）
3. npm run dev                    # 前端开发服务器
4. python simulator.py -n 3      # GPS 模拟器开始上报
```

---

## 七、真实 CAT1 硬件接入

真实 GPS 硬件（CAT1 模块）只需：

1. 配置 MQTT Broker 地址：`your-server-ip:1883`
2. MQTT Topic：`gps/{设备编号}/location`
3. Payload（JSON 格式）：

```json
{
  "deviceId": "你的设备IMEI或编号",
  "lat": 39.9042,
  "lng": 116.4074,
  "speed": 80.5,
  "direction": 135.0,
  "timestamp": 1746282000000
}
```

4. 先在系统后台「设备管理」页面注册对应的 deviceCode
5. 即可在「实时监控」页面看到设备位置

---

## 八、生产部署注意事项

### 修改默认密码和密钥

编辑 `backend/src/TraceIot.HttpApi.Host/appsettings.json`：
```json
{
  "ConnectionStrings": {
    "Default": "Host=prod-pg-host;..."
  },
  "Jwt": {
    "Key": "你的强密钥（至少32位随机字符）"
  }
}
```

### 前端生产构建

```bash
cd frontend
npm run build   # 输出到 dist/ 目录
# 用 Nginx 或 Docker 部署 dist/
```

### 后端 Docker 化（可选）

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY publish/ .
ENTRYPOINT ["dotnet", "TraceIot.HttpApi.Host.dll"]
```

---

## 九、常见问题

**Q: 后端启动报 "无法连接 PostgreSQL"**  
A: 检查 `docker compose ps` 确认 postgres 容器已运行，等待约 10 秒再重试

**Q: 模拟器报 "MQTT 连接失败"**  
A: 检查 EMQX 容器是否正常运行，端口 1883 是否开放

**Q: InfluxDB Token 错误**  
A: 确认 appsettings.json 中的 Token 与 docker-compose.yml 中的 `DOCKER_INFLUXDB_INIT_ADMIN_TOKEN` 一致

**Q: 地图不显示**  
A: 检查高德地图 Key 是否配置正确，开发环境需要在高德控制台添加域名 `localhost`

**Q: 轨迹数据为空**  
A: 确认模拟器已运行并成功上报，InfluxDB 数据写入时区为 UTC，查询时前端自动处理
