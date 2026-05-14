import ujson
import time
import sys
from machine import Pin
import tm1637

sys.path.append('/config')
sys.path.append('/utils')

from config import API_CONFIG, TELEMETRY_CONFIG, DEBUG
from helpers import log_message
from api_client import ApiClient

class TelemetryManager:
    """
    Manages telemetry data transmission using unified ApiClient
    """

    def __init__(self, robot, auth_manager):
        self.robot = robot
        self.auth_manager = auth_manager
        self.api = ApiClient()
        self.status_endpoint = API_CONFIG["ROBOT_STATUS_ENDPOINT"]
        self.me_endpoint = API_CONFIG["ROBOT_ME_ENDPOINT"]
        self.update_interval = TELEMETRY_CONFIG["UPDATE_INTERVAL"]
        self.last_update_time = 0
        
        try:
            self.tm = tm1637.TM1637(clk=Pin(18), dio=Pin(19))
            self.tm.brightness(7)
        except Exception as e:
            log_message("Display init failed: " + str(e), "ERROR")
            self.tm = None

    def send_status_update(self, force=False):
        """
        Send robot status update to server
        """
        current_time = time.time()

        if not force and (current_time - self.last_update_time < self.update_interval):
            return True

        self.last_update_time = current_time

        if not self.auth_manager.is_authenticated():
            log_message("Cannot send telemetry: Not authenticated", "WARNING")
            return False

        # Prepare telemetry payload
        payload = {
            "status": self.robot.status,
            "batteryLevel": round(self.robot.battery_level, 2),
            "currentNodeId": self.robot.current_node_id,
            "currentLatitude": self.robot.current_latitude,
            "currentLongitude": self.robot.current_longitude,
            "targetNodeId": self.robot.target_node_id
        }

        if self.tm:
            self.tm.number(int(self.robot.battery_level))

        response = self.api.post(self.status_endpoint, json=payload)

        if response and response.status_code == 200:
            response.close()
            return True
        else:
            status = response.status_code if response else "Unknown"
            log_message("Telemetry failed: Status {}".format(status), "ERROR")
            if response: response.close()
            return False

    def fetch_robot_info(self):
        """
        Fetch robot information from server
        """
        if not self.auth_manager.is_authenticated():
            return None

        response = self.api.get(self.me_endpoint)

        if response and response.status_code == 200:
            data = response.json()
            log_message("Robot info fetched")
            self.robot.set_robot_info(data)
            response.close()
            return data
        else:
            if response: response.close()
            return None

    def should_send_update(self):
        current_time = time.time()
        return (current_time - self.last_update_time) >= self.update_interval

    def fetch_node_info(self, node_id):
        """
        Fetch node information from server
        """
        if not self.auth_manager.is_authenticated():
            return None

        endpoint = "/api/Node/{}".format(node_id)
        response = self.api.get(endpoint)

        if response and response.status_code == 200:
            data = response.json()
            log_message("Node {} fetched".format(node_id))
            response.close()
            return data
        else:
            if response: response.close()
            return None

