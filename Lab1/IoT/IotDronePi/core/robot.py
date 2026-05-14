import sys
sys.path.append('/config')

from config import ROBOT_CHARACTERISTICS

class RobotState:
    """
    Enum-like class for robot states
    """
    IDLE = "Idle"
    DELIVERING = "Delivering"
    RETURNING = "Returning"
    CHARGING = "Charging"
    MAINTENANCE = "Maintenance"

class RobotType:
    """
    Enum-like class for robot types
    """
    DRONE = "Drone"
    GROUND_COURIER = "GroundCourier"

class Robot:
    """
    Core Robot class representing the physical robot
    Contains all robot characteristics and state
    """

    def __init__(self, robot_id=None):
        # Identity
        self.robot_id = robot_id
        self.name = None
        self.model = None
        self.serial_number = None

        # Type and Characteristics
        self.robot_type = ROBOT_CHARACTERISTICS["TYPE"]
        self.battery_capacity_joules = ROBOT_CHARACTERISTICS["BATTERY_CAPACITY_JOULES"]
        self.energy_consumption_per_meter = ROBOT_CHARACTERISTICS["ENERGY_CONSUMPTION_PER_METER"]
        self.hover_consumption_per_second = ROBOT_CHARACTERISTICS.get("HOVER_CONSUMPTION_PER_SECOND", 100)
        self.idle_consumption_per_second = ROBOT_CHARACTERISTICS.get("IDLE_CONSUMPTION_PER_SECOND", 5) # 5J/s idle
        self.max_speed_ms = ROBOT_CHARACTERISTICS["MAX_SPEED_MS"]
        self.min_battery_level = ROBOT_CHARACTERISTICS.get("MIN_BATTERY_LEVEL", 15.0)

        # Current State
        self.status = RobotState.IDLE
        self.battery_level = 100.0  # percentage

        # Location
        self.current_latitude = None
        self.current_longitude = None
        self.current_node_id = None

        # Navigation
        self.target_latitude = None
        self.target_longitude = None
        self.target_node_id = None

        # Order Management
        self.current_order_id = None
        self.payload_weight = 0.0  # kg
        self.pickup_node_id = None
        self.dropoff_node_id = None

    def set_robot_info(self, info_dict):
        """
        Set robot information from server response

        Args:
            info_dict: Dictionary containing robot information
        """
        if info_dict:
            self.robot_id = info_dict.get('id', self.robot_id)
            self.name = info_dict.get('name', self.name)
            self.model = info_dict.get('model', self.model)
            self.serial_number = info_dict.get('serialNumber', self.serial_number)
            self.status = info_dict.get('statusName', self.status)
            self.battery_level = info_dict.get('batteryLevel', self.battery_level)
            self.current_node_id = info_dict.get('currentNodeId', self.current_node_id)

    def update_battery_level(self, new_level):
        """
        Update battery level

        Args:
            new_level: New battery level (0-100)
        """
        self.battery_level = max(0.0, min(100.0, new_level))

    def drain_battery(self, distance_meters, time_seconds=0):
        """
        Drain battery based on distance and optionally time.
        Energy = (distance * consumption_per_meter) + (time * consumption_per_second)
        """
        # Account for payload weight (roughly 10% more energy per kg)
        # weight_factor = 1.0 + (self.payload_weight * 0.1)
        weight_factor = 1.0 + (self.payload_weight * 0.1)
        
        energy_propulsion = distance_meters * self.energy_consumption_per_meter * weight_factor
        energy_lift = time_seconds * self.hover_consumption_per_second * weight_factor
        
        total_energy = energy_propulsion + energy_lift
        self._consume_energy(total_energy)

    def drain_battery_time(self, seconds):
        """
        Drain battery based on time (hovering/flying)

        Args:
            seconds: Time in seconds
        """
        energy_consumed = seconds * self.hover_consumption_per_second
        self._consume_energy(energy_consumed)

    def drain_battery_idle(self, seconds):
        """
        Drain battery based on idle time (motors off)
        """
        energy_consumed = seconds * self.idle_consumption_per_second
        self._consume_energy(energy_consumed)

    def _consume_energy(self, energy_joules):
        """
        Internal method to consume specific amount of energy
        """
        if self.battery_capacity_joules <= 0:
            return

        energy_remaining = (self.battery_level / 100.0) * self.battery_capacity_joules
        energy_remaining -= energy_joules

        new_battery_level = (energy_remaining / self.battery_capacity_joules) * 100.0
        self.update_battery_level(new_battery_level)

    def is_battery_low(self):
        """
        Check if battery level is below minimum threshold

        Returns:
            bool: True if battery is low, False otherwise
        """
        return self.battery_level < self.min_battery_level

    def set_location(self, latitude, longitude, node_id=None):
        """
        Update robot location

        Args:
            latitude: Latitude coordinate
            longitude: Longitude coordinate
            node_id: Optional node ID if at a node
        """
        self.current_latitude = latitude
        self.current_longitude = longitude
        self.current_node_id = node_id

    def set_target(self, latitude, longitude, node_id=None):
        """
        Set navigation target

        Args:
            latitude: Target latitude
            longitude: Target longitude
            node_id: Optional target node ID
        """
        self.target_latitude = latitude
        self.target_longitude = longitude
        self.target_node_id = node_id

    def set_status(self, new_status):
        """
        Update robot status

        Args:
            new_status: New status (from RobotState)
        """
        valid_statuses = [
            RobotState.IDLE,
            RobotState.DELIVERING,
            RobotState.RETURNING,
            RobotState.CHARGING,
            RobotState.MAINTENANCE
        ]

        if new_status in valid_statuses:
            self.status = new_status

    def start_delivery(self, order_id, pickup_node_id, dropoff_node_id):
        """
        Start delivery mission

        Args:
            order_id: Order ID to deliver
            pickup_node_id: Pickup node ID
            dropoff_node_id: Dropoff node ID
        """
        self.current_order_id = order_id
        self.pickup_node_id = pickup_node_id
        self.dropoff_node_id = dropoff_node_id
        self.set_status(RobotState.DELIVERING)

    def complete_delivery(self):
        """
        Complete current delivery mission
        """
        self.current_order_id = None
        self.pickup_node_id = None
        self.dropoff_node_id = None
        self.target_latitude = None
        self.target_longitude = None
        self.target_node_id = None
        self.set_status(RobotState.IDLE)

    def get_state_dict(self):
        """
        Get robot state as dictionary for telemetry

        Returns:
            dict: Robot state dictionary
        """
        return {
            "status": self.status,
            "batteryLevel": self.battery_level,
            "currentNodeId": self.current_node_id,
            "currentLatitude": self.current_latitude,
            "currentLongitude": self.current_longitude,
            "targetNodeId": self.target_node_id
        }

    def __str__(self):
        """
        String representation of robot

        Returns:
            str: Robot information
        """
        return ("Robot(id={}, name={}, type={}, status={}, battery={:.1f}%, "
                "location=({:.6f}, {:.6f}))").format(
            self.robot_id, self.name, self.robot_type, self.status,
            self.battery_level, self.current_latitude or 0.0,
            self.current_longitude or 0.0
        )
