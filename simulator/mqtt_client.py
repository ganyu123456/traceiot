"""MQTT 上报客户端"""

import json
import time
import logging
from typing import Optional
import paho.mqtt.client as mqtt

logger = logging.getLogger(__name__)


class GpsMqttClient:
    def __init__(
        self,
        device_code: str,
        host: str = "localhost",
        port: int = 1883,
        username: str = "",
        password: str = "",
    ):
        self.device_code = device_code
        self.host        = host
        self.port        = port
        self._connected  = False

        self._client = mqtt.Client(
            client_id=f"sim-{device_code}",
            protocol=mqtt.MQTTv311,
            clean_session=True,
        )

        if username:
            self._client.username_pw_set(username, password)

        self._client.on_connect    = self._on_connect
        self._client.on_disconnect = self._on_disconnect

    def _on_connect(self, client, userdata, flags, rc):
        if rc == 0:
            self._connected = True
            logger.info("[%s] MQTT 连接成功", self.device_code)
            # 发送上线心跳
            self._client.publish(
                f"gps/{self.device_code}/heartbeat",
                json.dumps({"deviceId": self.device_code, "event": "online"}),
                qos=1
            )
        else:
            logger.error("[%s] MQTT 连接失败，rc=%d", self.device_code, rc)

    def _on_disconnect(self, client, userdata, rc):
        self._connected = False
        logger.warning("[%s] MQTT 断开连接，rc=%d", self.device_code, rc)

    def connect(self) -> bool:
        try:
            self._client.connect(self.host, self.port, keepalive=60)
            self._client.loop_start()
            # 等待连接建立
            for _ in range(30):
                if self._connected:
                    return True
                time.sleep(0.1)
            return False
        except Exception as e:
            logger.error("[%s] 连接异常: %s", self.device_code, e)
            return False

    def disconnect(self):
        self._client.loop_stop()
        self._client.disconnect()

    def publish_location(
        self, lat: float, lng: float,
        speed: float, direction: float
    ) -> bool:
        if not self._connected:
            return False
        payload = json.dumps({
            "deviceId":  self.device_code,
            "lat":       round(lat,       6),
            "lng":       round(lng,       6),
            "speed":     round(speed,     2),
            "direction": round(direction, 2),
            "timestamp": int(time.time() * 1000),
        })
        result = self._client.publish(
            f"gps/{self.device_code}/location",
            payload,
            qos=1
        )
        return result.rc == mqtt.MQTT_ERR_SUCCESS

    @property
    def is_connected(self) -> bool:
        return self._connected
