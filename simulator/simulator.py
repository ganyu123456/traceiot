#!/usr/bin/env python3
"""
TraceIoT GPS 模拟器
用法：
    python simulator.py                     # 使用 config.py 默认配置（3台设备）
    python simulator.py -n 5                # 自动生成 5 台模拟设备
    python simulator.py --interval 2        # 2 秒上报一次
    python simulator.py --speed 80          # 模拟速度 80 km/h
    python simulator.py --host 192.168.1.1  # 指定 MQTT Broker 地址
"""

import argparse
import asyncio
import logging
import sys
import time
from typing import Dict

import config
from device import DeviceSimulator
from mqtt_client import GpsMqttClient
from route_data import DEFAULT_ROUTE

# 彩色日志
try:
    import colorlog
    handler = colorlog.StreamHandler()
    handler.setFormatter(colorlog.ColoredFormatter(
        '%(log_color)s%(asctime)s [%(name)s] %(message)s',
        datefmt='%H:%M:%S',
        log_colors={'DEBUG': 'cyan', 'INFO': 'green', 'WARNING': 'yellow', 'ERROR': 'red'}
    ))
    logging.basicConfig(level=logging.INFO, handlers=[handler])
except ImportError:
    logging.basicConfig(
        level=logging.INFO,
        format='%(asctime)s [%(name)s] %(message)s',
        datefmt='%H:%M:%S'
    )

logger = logging.getLogger("simulator")


def parse_args():
    parser = argparse.ArgumentParser(description='TraceIoT GPS 模拟器')
    parser.add_argument('-n',         '--count',    type=int,   default=None,         help='模拟设备数量（覆盖 config.py）')
    parser.add_argument('--interval', '-i',         type=float, default=None,         help='上报间隔（秒），默认 1.0')
    parser.add_argument('--speed',    '-s',         type=float, default=None,         help='模拟速度（km/h），默认 100')
    parser.add_argument('--host',                   type=str,   default=None,         help='MQTT Broker 地址')
    parser.add_argument('--port',                   type=int,   default=None,         help='MQTT 端口，默认 1883')
    return parser.parse_args()


def build_devices(count: int | None, speed: float, interval: float) -> Dict[str, dict]:
    """构造设备配置"""
    if count is not None:
        devices = {}
        for i in range(1, count + 1):
            code = f"SIM-{i:03d}"
            devices[code] = {
                "name":   f"模拟车辆{i:02d}",
                "offset": (i - 1) / count
            }
        return devices
    else:
        return {
            code: {
                "name":   config.DEVICES[code],
                "offset": config.DEVICE_START_OFFSETS.get(code, 0.0)
            }
            for code in config.DEVICES
        }


async def run_device(
    device_code: str,
    device_name: str,
    start_offset: float,
    speed: float,
    interval: float,
    host: str,
    port: int,
):
    """单台设备的异步上报协程"""
    simulator = DeviceSimulator(
        device_code=device_code,
        device_name=device_name,
        route=DEFAULT_ROUTE,
        speed_kmh=speed,
        interval=interval,
        start_offset=start_offset,
    )

    client = GpsMqttClient(
        device_code=device_code,
        host=host,
        port=port,
        username=config.MQTT_USERNAME,
        password=config.MQTT_PASSWORD,
    )

    logger.info("正在连接 MQTT Broker [%s]...", device_code)
    if not client.connect():
        logger.error("[%s] 无法连接 MQTT Broker，跳过该设备", device_code)
        return

    direction_label = lambda d: ['北','东北','东','东南','南','西南','西','西北'][int((d + 22.5) / 45) % 8]

    try:
        logger.info("[%s] %s 开始模拟，速度: %.0f km/h，间隔: %.1f s",
                    device_code, device_name, speed, interval)

        step = 0
        while True:
            ok = client.publish_location(
                lat=simulator.lat,
                lng=simulator.lng,
                speed=simulator.speed,
                direction=simulator.direction,
            )

            if step % 10 == 0:  # 每10步打印一次
                status = "✓" if ok else "✗"
                direction = direction_label(simulator.direction)
                logger.info(
                    "[%s] %s | %.6f,%.6f | %.1f km/h | %s | 进度 %.1f%%",
                    device_code, status,
                    simulator.lat, simulator.lng,
                    simulator.speed, direction,
                    simulator.progress * 100
                )

            simulator.step()
            step += 1
            await asyncio.sleep(interval)

    except asyncio.CancelledError:
        logger.info("[%s] 停止", device_code)
    finally:
        client.disconnect()


async def main():
    args = parse_args()

    host     = args.host     or config.MQTT_HOST
    port     = args.port     or config.MQTT_PORT
    interval = args.interval or config.REPORT_INTERVAL
    speed    = args.speed    or config.SIMULATE_SPEED

    devices = build_devices(args.count, speed, interval)

    logger.info("=" * 60)
    logger.info("TraceIoT GPS 模拟器启动")
    logger.info("MQTT Broker: %s:%d", host, port)
    logger.info("设备数量: %d 台", len(devices))
    logger.info("模拟速度: %.0f km/h，上报间隔: %.1f 秒", speed, interval)
    logger.info("路线: 北京 → 天津（G2 京津塘高速）往返循环")
    logger.info("=" * 60)

    tasks = [
        asyncio.create_task(
            run_device(
                device_code=code,
                device_name=info["name"],
                start_offset=info["offset"],
                speed=speed,
                interval=interval,
                host=host,
                port=port,
            ),
            name=code
        )
        for code, info in devices.items()
    ]

    try:
        await asyncio.gather(*tasks)
    except KeyboardInterrupt:
        logger.info("\n正在停止所有模拟器...")
        for task in tasks:
            task.cancel()
        await asyncio.gather(*tasks, return_exceptions=True)
        logger.info("模拟器已停止")


if __name__ == "__main__":
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        print("\n已停止")
