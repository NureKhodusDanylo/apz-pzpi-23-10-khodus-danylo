import time
import sys

sys.path.append('/config')
sys.path.append('/utils')

from config import GPS_CONFIG, ROBOT_CHARACTERISTICS, DEBUG
from helpers import log_message, calculate_distance, calculate_bearing, move_coordinates

class GPSSimulator:
    """
    GPS Navigation and Movement Simulator
    Simulates robot movement between coordinates
    """

    def __init__(self, robot):
        self.robot = robot
        self.start_latitude = GPS_CONFIG["START_LATITUDE"]
        self.start_longitude = GPS_CONFIG["START_LONGITUDE"]
        self.movement_step = GPS_CONFIG["MOVEMENT_STEP"]
        self.update_interval = GPS_CONFIG["UPDATE_INTERVAL"]
        self.max_speed_ms = ROBOT_CHARACTERISTICS["MAX_SPEED_MS"]

        # Initialize robot location only if not already set (from server)
        if self.robot.current_latitude is None or self.robot.current_longitude is None:
            self.robot.set_location(self.start_latitude, self.start_longitude)

        # Movement state
        self.is_moving = False
        self.last_update_time = 0

    def set_destination(self, target_lat, target_lon, target_node_id=None):
        """
        Set destination for robot navigation

        Args:
            target_lat: Target latitude
            target_lon: Target longitude
            target_node_id: Optional target node ID
        """
        self.robot.set_target(target_lat, target_lon, target_node_id)
        self.is_moving = True

        distance = calculate_distance(
            self.robot.current_latitude,
            self.robot.current_longitude,
            target_lat,
            target_lon
        )

        log_message("Destination set: ({:.6f}, {:.6f}), Distance: {:.2f}m".format(
            target_lat, target_lon, distance
        ))

    def update_position(self):
        """
        Update robot position (simulate movement)
        Should be called periodically

        Returns:
            bool: True if robot is still moving, False if reached destination
        """
        current_time = time.time()

        # Check if it's time to update
        if current_time - self.last_update_time < self.update_interval:
            return self.is_moving

        self.last_update_time = current_time

        if not self.is_moving or self.robot.target_latitude is None:
            return False

        current_lat = self.robot.current_latitude
        current_lon = self.robot.current_longitude
        target_lat = self.robot.target_latitude
        target_lon = self.robot.target_longitude

        # Calculate distance to target
        distance_to_target = calculate_distance(
            current_lat, current_lon,
            target_lat, target_lon
        )

        # Check if we've arrived
        if distance_to_target < 1.0:  # Within 1 meter
            self.robot.set_location(target_lat, target_lon, self.robot.target_node_id)
            self.is_moving = False

            log_message("Arrived at destination: ({:.6f}, {:.6f})".format(
                target_lat, target_lon
            ))

            # Drain battery for the remaining distance
            if distance_to_target > 0:
                self.robot.drain_battery(distance_to_target)

            return False

        # Calculate movement distance for this update
        # distance = speed * time
        movement_distance = self.max_speed_ms * self.update_interval

        # Don't overshoot the target
        if movement_distance > distance_to_target:
            movement_distance = distance_to_target

        # Calculate bearing to target
        bearing = calculate_bearing(current_lat, current_lon, target_lat, target_lon)

        # Calculate new position
        new_lat, new_lon = move_coordinates(
            current_lat, current_lon,
            bearing, movement_distance
        )

        # Update robot location
        self.robot.set_location(new_lat, new_lon, None)

        # Drain battery based on distance traveled and time elapsed
        self.robot.drain_battery(movement_distance, self.update_interval)

        # Log movement with INFO level every update
        log_message(
            "Moving to ({:.6f}, {:.6f}), remaining: {:.0f}m, battery: {:.1f}%".format(
                target_lat, target_lon,
                distance_to_target - movement_distance,
                self.robot.battery_level
            )
        )

        return True

    def stop_movement(self):
        """
        Stop robot movement
        """
        self.is_moving = False
        self.robot.target_latitude = None
        self.robot.target_longitude = None
        self.robot.target_node_id = None
        log_message("Movement stopped")

    def get_distance_to_target(self):
        """
        Calculate remaining distance to target

        Returns:
            float: Distance in meters or None if no target
        """
        if self.robot.target_latitude is None:
            return None

        return calculate_distance(
            self.robot.current_latitude,
            self.robot.current_longitude,
            self.robot.target_latitude,
            self.robot.target_longitude
        )

    def estimate_time_to_arrival(self):
        """
        Estimate time to reach destination

        Returns:
            float: Time in seconds or None if no target
        """
        distance = self.get_distance_to_target()
        if distance is None or self.max_speed_ms == 0:
            return None

        return distance / self.max_speed_ms

    def get_current_coordinates(self):
        """
        Get current GPS coordinates

        Returns:
            tuple: (latitude, longitude)
        """
        return self.robot.current_latitude, self.robot.current_longitude
