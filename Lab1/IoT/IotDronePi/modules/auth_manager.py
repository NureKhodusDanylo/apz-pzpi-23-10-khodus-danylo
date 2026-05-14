import ujson
import sys

sys.path.append('/config')
sys.path.append('/utils')

from config import API_CONFIG, ROBOT_CONFIG, DEBUG, ROBOT_CHARACTERISTICS
from helpers import log_message
from api_client import ApiClient

class AuthManager:
    """
    Manages robot authentication with the server using unified ApiClient
    """

    def __init__(self):
        self.api = ApiClient()
        self.auth_endpoint = API_CONFIG["AUTH_ENDPOINT"]
        self.register_endpoint = "/api/Auth/robot/register"
        self.serial_number = ROBOT_CONFIG["SERIAL_NUMBER"]
        self.access_key = ROBOT_CONFIG["ACCESS_KEY"]
        self.robot_type = ROBOT_CONFIG.get("TYPE", "Drone")
        self.robot_id = None

    def login(self):
        """
        Authenticate robot with the server
        First tries to register, if robot exists - then login
        """
        # Try to register first
        if self._try_register():
            return True

        # If registration failed (robot already exists), try login
        log_message("Robot already registered. Attempting login...")
        return self._try_login()

    def _try_register(self):
        """
        Try to register robot on the server
        """
        payload = {
            "name": "ESP32_Wokwi_{}".format(self.serial_number[-4:]),
            "model": "Generic ESP32",
            "type": self.robot_type,
            "serialNumber": self.serial_number,
            "accessKey": self.access_key,
            "batteryCapacityJoules": ROBOT_CHARACTERISTICS["BATTERY_CAPACITY_JOULES"],
            "energyConsumptionPerMeterJoules": ROBOT_CHARACTERISTICS["ENERGY_CONSUMPTION_PER_METER"],
            "hoverConsumptionPerSecondJoules": ROBOT_CHARACTERISTICS["HOVER_CONSUMPTION_PER_SECOND"],
            "ipAddress": "10.10.0.2", 
            "port": 80,
            "currentNodeId": API_CONFIG.get("START_NODE", 16)
        }

        response = self.api.post(self.register_endpoint, json=payload)
        
        if not response:
            return False

        status = response.status_code
        if status == 200 or status == 201:
            data = response.json()
            token = data.get('token')
            self.robot_id = data.get('robotId')
            self.api.set_token(token)
            log_message("Robot registered! ID: {}".format(self.robot_id))
            response.close()
            return True
        elif status == 409 or status == 400: # Backend now returns 400 if already exists
            log_message("Registration skipped (Already exists).")
            response.close()
            return False
        else:
            log_message("Reg failed ({}): {}".format(status, response.text[:50]), "ERROR")
            response.close()
            return False

    def _try_login(self):
        """
        Try to login robot on the server
        """
        payload = {
            "serialNumber": self.serial_number,
            "accessKey": self.access_key
        }

        response = self.api.post(self.auth_endpoint, json=payload)
        
        if not response:
            return False

        status = response.status_code
        if status == 200:
            data = response.json()
            token = data.get('token')
            self.robot_id = data.get('robotId')

            if token:
                self.api.set_token(token)
                log_message("Login successful! Robot ID: {}".format(self.robot_id))
                response.close()
                return True
            else:
                log_message("Login failed: No token", "ERROR")
                response.close()
                return False
        else:
            log_message("Login failed ({}): {}".format(status, response.text[:50]), "ERROR")
            response.close()
            return False

    def get_auth_header(self):
        """Deprecated: Use ApiClient directly for headers"""
        return self.api._get_headers()

    def is_authenticated(self):
        return self.api.token is not None

    def get_robot_id(self):
        return self.robot_id

    def get_token(self):
        return self.api.token

    def refresh_token(self):
        return self.login()

    def logout(self):
        self.api.set_token(None)
        self.robot_id = None
        log_message("Logged out")

