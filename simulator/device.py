"""GPS 设备模拟器 - 路线插值与位置计算"""

import math
from typing import List, Tuple


def haversine_distance(lat1: float, lng1: float, lat2: float, lng2: float) -> float:
    """计算两点间的球面距离（米）"""
    R = 6371000
    phi1, phi2 = math.radians(lat1), math.radians(lat2)
    dphi = math.radians(lat2 - lat1)
    dlam = math.radians(lng2 - lng1)
    a = math.sin(dphi / 2) ** 2 + math.cos(phi1) * math.cos(phi2) * math.sin(dlam / 2) ** 2
    return R * 2 * math.atan2(math.sqrt(a), math.sqrt(1 - a))


def bearing(lat1: float, lng1: float, lat2: float, lng2: float) -> float:
    """计算从点1到点2的方位角（0-360度，正北为0）"""
    phi1 = math.radians(lat1)
    phi2 = math.radians(lat2)
    dlam = math.radians(lng2 - lng1)
    x = math.sin(dlam) * math.cos(phi2)
    y = math.cos(phi1) * math.sin(phi2) - math.sin(phi1) * math.cos(phi2) * math.cos(dlam)
    return (math.degrees(math.atan2(x, y)) + 360) % 360


def interpolate_route(
    route: List[Tuple[float, float]],
    speed_kmh: float,
    interval_sec: float
) -> List[Tuple[float, float, float, float]]:
    """
    将路线点插值为匀速运动的轨迹点序列
    
    Returns:
        List of (lat, lng, speed, direction)
    """
    # 每个上报间隔走的距离（米）
    step_dist = (speed_kmh / 3.6) * interval_sec

    result: List[Tuple[float, float, float, float]] = []
    cur_lat, cur_lng = route[0]
    remaining_dist   = 0.0

    for i in range(1, len(route)):
        next_lat, next_lng = route[i]
        seg_dist  = haversine_distance(cur_lat, cur_lng, next_lat, next_lng)
        seg_bear  = bearing(cur_lat, cur_lng, next_lat, next_lng)

        # 在当前段内按步长插值
        while remaining_dist + seg_dist >= step_dist:
            frac = (step_dist - remaining_dist) / seg_dist
            p_lat = cur_lat + frac * (next_lat - cur_lat)
            p_lng = cur_lng + frac * (next_lng - cur_lng)
            result.append((p_lat, p_lng, speed_kmh, seg_bear))
            cur_lat = p_lat
            cur_lng = p_lng
            seg_dist    -= (step_dist - remaining_dist)
            remaining_dist = 0.0

        remaining_dist += seg_dist
        cur_lat = next_lat
        cur_lng = next_lng

    return result


class DeviceSimulator:
    """单台设备模拟器"""

    def __init__(
        self,
        device_code: str,
        device_name: str,
        route:       List[Tuple[float, float]],
        speed_kmh:   float = 100.0,
        interval:    float = 1.0,
        start_offset: float = 0.0
    ):
        self.device_code = device_code
        self.device_name = device_name
        self.speed_kmh   = speed_kmh
        self.interval    = interval

        # 预计算完整轨迹点列表
        self._track  = interpolate_route(route, speed_kmh, interval)
        total        = len(self._track)
        start_idx    = int(start_offset * total)
        # 前进方向
        self._fwd_track = self._track[start_idx:] + self._track[:start_idx]
        # 反向（折返）
        self._rev_track = list(reversed(self._track))

        self._is_forward = True
        self._track_ptr  = 0
        self._current    = self._fwd_track[0] if self._fwd_track else (39.9042, 116.4074, 0.0, 0.0)

    @property
    def lat(self)  -> float: return self._current[0]
    @property
    def lng(self)  -> float: return self._current[1]
    @property
    def speed(self) -> float: return self._current[2]
    @property
    def direction(self) -> float: return self._current[3]

    @property
    def progress(self) -> float:
        """当前行驶进度 0.0~1.0"""
        active = self._fwd_track if self._is_forward else self._rev_track
        return self._track_ptr / max(len(active) - 1, 1)

    def step(self):
        """移动一步（按 interval 时间）"""
        active = self._fwd_track if self._is_forward else self._rev_track
        self._track_ptr += 1
        if self._track_ptr >= len(active):
            # 到达终点，折返
            self._is_forward = not self._is_forward
            self._track_ptr  = 0
            active = self._fwd_track if self._is_forward else self._rev_track
        self._current = active[self._track_ptr]
