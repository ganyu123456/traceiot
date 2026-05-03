"""模拟器配置"""

# MQTT Broker 连接配置（与 docker-compose 对应）
MQTT_HOST     = "localhost"
MQTT_PORT     = 1883
MQTT_USERNAME = ""
MQTT_PASSWORD = ""

# 上报配置
REPORT_INTERVAL  = 1.0   # 秒，每台设备上报间隔
SIMULATE_SPEED   = 100.0 # km/h，模拟车速
INTERPOLATE_STEP = 0.01  # 两个路线点之间的插值步长（越小越平滑）

# 模拟设备列表（device_code: device_name）
DEVICES = {
    "SIM-001": "模拟车辆01",
    "SIM-002": "模拟车辆02",
    "SIM-003": "模拟车辆03",
}

# 各设备起始位置偏移（百分比），让设备错开出发
DEVICE_START_OFFSETS = {
    "SIM-001": 0.0,
    "SIM-002": 0.3,
    "SIM-003": 0.6,
}
