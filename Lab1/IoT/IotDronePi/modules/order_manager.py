import ujson
import sys

sys.path.append('/config')
sys.path.append('/utils')

from config import API_CONFIG, DEBUG
from helpers import log_message
from api_client import ApiClient

class OrderManager:
    """
    Manages robot orders and delivery missions using unified ApiClient
    """

    def __init__(self, robot, auth_manager):
        self.robot = robot
        self.auth_manager = auth_manager
        self.api = ApiClient()
        self.current_order = None
        self.pickup_coordinates = None
        self.dropoff_coordinates = None
        self.route_waypoints = None

    def fetch_assigned_orders(self):
        """
        Fetch orders assigned to this robot from server
        """
        if not self.auth_manager.is_authenticated():
            return []

        response = self.api.get("/api/Robot/my-orders")

        if response and response.status_code == 200:
            orders = response.json()
            log_message("Fetched {} order(s)".format(len(orders)))
            response.close()
            return orders
        else:
            if response: response.close()
            return []

    def accept_order(self, order_id):
        """
        Accept an assigned order from server
        """
        if not self.auth_manager.is_authenticated():
            return False

        endpoint = "/api/Robot/order/{}/accept".format(order_id)
        log_message("Accepting order {}...".format(order_id))

        response = self.api.post(endpoint, json={}) 

        if response and response.status_code == 200:
            log_message("Order {} accepted".format(order_id))
            response.close()
            return True
        else:
            if response: response.close()
            return False

    def start_order(self, order_data):
        """
        Start a delivery order from server data
        """
        if self.robot.status != "Idle" and self.robot.status != "Delivering":
            log_message("Cannot start order: Robot is busy with status {}".format(self.robot.status), "WARNING")
            return False

        if self.robot.is_battery_low():
            log_message("Cannot start order: Battery too low", "WARNING")
            return False

        order_id = order_data.get("orderId")
        pickup_lat = order_data.get("pickupLatitude")
        pickup_lon = order_data.get("pickupLongitude")
        dropoff_lat = order_data.get("dropoffLatitude")
        dropoff_lon = order_data.get("dropoffLongitude")
        route = order_data.get("route", [])

        self.current_order = {
            "id": order_id,
            "name": order_data.get("orderName", ""),
            "weight": order_data.get("weight", 0),
            "pickup": {"lat": pickup_lat, "lon": pickup_lon, "nodeId": order_data.get("pickupNodeId")},
            "dropoff": {"lat": dropoff_lat, "lon": dropoff_lon, "nodeId": order_data.get("dropoffNodeId")},
            "totalDistance": order_data.get("totalDistanceMeters", 0),
            "batteryUsage": order_data.get("estimatedBatteryUsagePercent", 0)
        }

        self.pickup_coordinates = (pickup_lat, pickup_lon)
        self.dropoff_coordinates = (dropoff_lat, dropoff_lon)
        self.route_waypoints = route

        self.robot.set_status("Delivering")

        log_message("Order {} started: {} ({} kg)".format(
            order_id, self.current_order["name"], self.current_order["weight"]
        ))
        log_message("Route: {} waypoints, {:.0f}m total, {:.1f}% battery".format(
            len(route), self.current_order["totalDistance"], self.current_order["batteryUsage"]
        ))

        return True

    def update_order_phase(self, phase_name, latitude=None, longitude=None):
        """
        Update order delivery phase on server
        """
        if not self.current_order or not self.auth_manager.is_authenticated():
            return False

        order_id = self.current_order["id"]
        endpoint = "/api/Robot/order/{}/phase".format(order_id)

        body = {
            "phase": phase_name,
            "latitude": latitude if latitude is not None else self.robot.current_latitude,
            "longitude": longitude if longitude is not None else self.robot.current_longitude,
            "timestamp": "{}Z".format(self._get_utc_timestamp())
        }

        log_message("Updating order phase to: {}".format(phase_name))
        response = self.api.post(endpoint, json=body)

        if response and response.status_code == 200:
            log_message("Order phase updated")
            response.close()
            return True
        else:
            if response: response.close()
            return False

    def _get_utc_timestamp(self):
        import time
        t = time.localtime()
        return "{:04d}-{:02d}-{:02d}T{:02d}:{:02d}:{:02d}".format(
            t[0], t[1], t[2], t[3], t[4], t[5]
        )

    def get_pickup_coordinates(self):
        return self.pickup_coordinates

    def get_dropoff_coordinates(self):
        return self.dropoff_coordinates

    def get_route_waypoints(self):
        return self.route_waypoints

    def get_pickup_node_id(self):
        if self.current_order:
            return self.current_order.get("pickup", {}).get("nodeId")
        return None

    def get_dropoff_node_id(self):
        if self.current_order:
            return self.current_order.get("dropoff", {}).get("nodeId")
        return None

    def complete_order(self):
        if self.current_order:
            order_id = self.current_order["id"]
            log_message("Order {} completed".format(order_id))

            self.current_order = None
            self.pickup_coordinates = None
            self.dropoff_coordinates = None

            self.robot.complete_delivery()

    def cancel_order(self, reason="Unknown"):
        if self.current_order:
            order_id = self.current_order["id"]
            log_message("Order {} cancelled: {}".format(order_id, reason), "WARNING")

            self.current_order = None
            self.pickup_coordinates = None
            self.dropoff_coordinates = None

            self.robot.set_status("Idle")

    def has_active_order(self):
        return self.current_order is not None

    def get_current_order_id(self):
        if self.current_order:
            return self.current_order["id"]
        return None

    def get_order_phase(self):
        if self.current_order:
            return self.current_order["phase"]
        return None

