-- TraceIoT GPS 定位轨迹云平台 - PostgreSQL 数据库初始化脚本
-- 注：ABP 框架自身表（用户、角色、权限等）由 EF Core Migration 自动创建
-- 本文件仅创建业务相关表

-- =====================================================
-- 设备分组表
-- =====================================================
CREATE TABLE IF NOT EXISTS device_groups (
    id          UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    name        VARCHAR(64) NOT NULL,
    description VARCHAR(256),
    sort_order  INT         NOT NULL DEFAULT 0,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    is_deleted  BOOLEAN     NOT NULL DEFAULT FALSE
);

COMMENT ON TABLE  device_groups            IS '设备分组';
COMMENT ON COLUMN device_groups.name       IS '分组名称';
COMMENT ON COLUMN device_groups.sort_order IS '排序权重';

-- =====================================================
-- 设备表
-- =====================================================
CREATE TABLE IF NOT EXISTS devices (
    id                   UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    device_code          VARCHAR(64) NOT NULL UNIQUE,  -- 设备编号，IMEI 或自定义
    device_name          VARCHAR(128) NOT NULL,
    group_id             UUID        REFERENCES device_groups(id) ON DELETE SET NULL,
    status               SMALLINT    NOT NULL DEFAULT 0,  -- 0=离线 1=在线 2=禁用
    is_enabled           BOOLEAN     NOT NULL DEFAULT TRUE,
    last_heartbeat_at    TIMESTAMPTZ,
    last_lat             DECIMAL(10,7),
    last_lng             DECIMAL(10,7),
    last_speed           DECIMAL(8,2),
    last_direction       DECIMAL(6,2),
    remark               VARCHAR(512),
    -- 预留告警配置字段
    overspeed_threshold  DECIMAL(8,2) DEFAULT 120,     -- 超速阈值 km/h
    offline_timeout_sec  INT          DEFAULT 60,       -- 离线判定秒数
    geofence_enabled     BOOLEAN      DEFAULT FALSE,    -- 是否启用电子围栏
    geofence_config      JSONB,                         -- 围栏配置 JSON
    created_at           TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at           TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    is_deleted           BOOLEAN     NOT NULL DEFAULT FALSE,
    creator_id           UUID,
    last_modifier_id     UUID
);

CREATE INDEX IF NOT EXISTS idx_devices_group_id ON devices(group_id);
CREATE INDEX IF NOT EXISTS idx_devices_status   ON devices(status);
CREATE INDEX IF NOT EXISTS idx_devices_code     ON devices(device_code);

COMMENT ON TABLE  devices                    IS 'GPS设备';
COMMENT ON COLUMN devices.device_code        IS '设备唯一编号(IMEI或自定义)';
COMMENT ON COLUMN devices.status             IS '0=离线 1=在线 2=禁用';
COMMENT ON COLUMN devices.geofence_config    IS '电子围栏配置，GeoJSON格式';

-- =====================================================
-- 操作日志表
-- =====================================================
CREATE TABLE IF NOT EXISTS operation_logs (
    id             UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    operator_id    UUID,
    operator_name  VARCHAR(64),
    action         VARCHAR(64) NOT NULL,    -- 如 CreateDevice / UpdateDevice
    target_type    VARCHAR(64),             -- 操作对象类型
    target_id      VARCHAR(64),             -- 操作对象ID
    detail         TEXT,
    ip_address     VARCHAR(64),
    created_at     TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_operation_logs_operator ON operation_logs(operator_id);
CREATE INDEX IF NOT EXISTS idx_operation_logs_target   ON operation_logs(target_id);
CREATE INDEX IF NOT EXISTS idx_operation_logs_time     ON operation_logs(created_at DESC);

-- =====================================================
-- 告警配置表
-- =====================================================
CREATE TABLE IF NOT EXISTS alarm_configs (
    id           UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    device_id    UUID        REFERENCES devices(id) ON DELETE CASCADE,
    alarm_type   SMALLINT    NOT NULL,  -- 1=超速 2=电子围栏 3=离线
    threshold    DECIMAL(10,4),         -- 告警阈值（超速：km/h；离线：秒）
    is_enabled   BOOLEAN     NOT NULL DEFAULT TRUE,
    notify_email VARCHAR(256),
    created_at   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at   TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_alarm_configs_device ON alarm_configs(device_id);

COMMENT ON COLUMN alarm_configs.alarm_type IS '1=超速 2=电子围栏 3=离线';

-- =====================================================
-- 告警记录表
-- =====================================================
CREATE TABLE IF NOT EXISTS alarm_records (
    id            UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    device_id     UUID        NOT NULL REFERENCES devices(id) ON DELETE CASCADE,
    device_code   VARCHAR(64),
    alarm_type    SMALLINT    NOT NULL,   -- 1=超速 2=电子围栏 3=离线
    alarm_value   DECIMAL(10,4),          -- 触发时的值
    lat           DECIMAL(10,7),
    lng           DECIMAL(10,7),
    triggered_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    handled_at    TIMESTAMPTZ,
    is_handled    BOOLEAN     NOT NULL DEFAULT FALSE,
    handler_id    UUID,
    handler_note  TEXT
);

CREATE INDEX IF NOT EXISTS idx_alarm_records_device  ON alarm_records(device_id);
CREATE INDEX IF NOT EXISTS idx_alarm_records_time    ON alarm_records(triggered_at DESC);
CREATE INDEX IF NOT EXISTS idx_alarm_records_handled ON alarm_records(is_handled);

COMMENT ON COLUMN alarm_records.alarm_type IS '1=超速 2=电子围栏 3=离线';

-- =====================================================
-- 初始化默认分组
-- =====================================================
INSERT INTO device_groups (id, name, description, sort_order)
VALUES
  ('00000000-0000-0000-0000-000000000001', '默认分组', '系统默认设备分组', 0),
  ('00000000-0000-0000-0000-000000000002', '测试车队', '用于演示的测试设备组', 1)
ON CONFLICT DO NOTHING;
