import time
import sys

sys.path.append('/config')
sys.path.append('/utils')

from config import TELEMETRY_CONFIG, DEBUG
from helpers import log_message, clamp

class BatteryManager:
    """
    Manages robot battery state and charging
    """

    def __init__(self, robot, hardware_controller=None):
        self.robot = robot
        self.hardware_controller = hardware_controller
        self.drain_rate = TELEMETRY_CONFIG.get("BATTERY_DRAIN_RATE", 0.0)
        self.charging_rate = 2.0  # percent per second when charging
        self.last_update_time = time.time()
        self.is_charging = False

    def start_charging(self):
        """
        Start battery charging
        """
        self.is_charging = True
        self.robot.set_status("Charging")
        log_message("Battery charging started")

    def stop_charging(self):
        """
        Stop battery charging
        """
        self.is_charging = False
        log_message("Battery charging stopped")

    def update_battery(self):
        """
        Update battery level based on current state
        Should be called periodically
        """
        current_time = time.time()
        time_elapsed = current_time - self.last_update_time
        self.last_update_time = current_time

        if self.is_charging:
            # Charge battery
            charge_amount = self.charging_rate * time_elapsed
            new_level = self.robot.battery_level + charge_amount
            self.robot.update_battery_level(new_level)

            if self.robot.battery_level >= 100.0:
                log_message("Battery fully charged")
                self.stop_charging()

            if DEBUG and int(current_time) % 5 == 0:  # Log every 5 seconds
                log_message(
                    "Charging: Battery at {:.1f}%".format(self.robot.battery_level),
                    "DEBUG"
                )
        elif self.hardware_controller and self.hardware_controller.are_motors_running():
            # Drain battery for hovering/flying time
            # Only if not currently being handled by GPSSimulator movement
            # Actually, GPSSimulator handles distance-based drain.
            # But drone also consumes energy just by staying in the air.
            self.robot.drain_battery_time(time_elapsed)
        else:
            # Idle drain
            self.simulate_idle_drain_step(time_elapsed)

    def check_battery_critical(self):
        """
        Check if battery is at critical level

        Returns:
            bool: True if battery is critical, False otherwise
        """
        if self.robot.battery_level < 10.0:
            log_message(
                "CRITICAL: Battery level at {:.1f}%".format(self.robot.battery_level),
                "WARNING"
            )
            return True
        return False

    def check_battery_low(self):
        """
        Check if battery is low

        Returns:
            bool: True if battery is low, False otherwise
        """
        if self.robot.is_battery_low() and not self.is_charging:
            log_message(
                "Battery low: {:.1f}%".format(self.robot.battery_level),
                "WARNING"
            )
            return True
        return False

    def get_battery_percentage(self):
        """
        Get current battery percentage

        Returns:
            float: Battery percentage (0-100)
        """
        return self.robot.battery_level

    def can_complete_mission(self, distance_meters):
        """
        Check if robot has enough battery to complete a mission

        Args:
            distance_meters: Distance to travel in meters

        Returns:
            bool: True if enough battery, False otherwise
        """
        energy_required = distance_meters * self.robot.energy_consumption_per_meter
        energy_available = (self.robot.battery_level / 100.0) * self.robot.battery_capacity_joules

        # Add 20% safety margin
        return energy_available >= energy_required * 1.2

    def get_max_range_meters(self):
        """
        Calculate maximum range with current battery level

        Returns:
            float: Maximum range in meters
        """
        energy_available = (self.robot.battery_level / 100.0) * self.robot.battery_capacity_joules
        return energy_available / self.robot.energy_consumption_per_meter

    def simulate_idle_drain_step(self, time_elapsed):
        """
        Simulate small battery drain during idle state
        """
        if not self.is_charging and self.robot.status == "Idle":
            self.robot.drain_battery_idle(time_elapsed)

    def simulate_idle_drain(self):
        # Legacy method
        current_time = time.time()
        time_elapsed = current_time - self.last_update_time
        self.simulate_idle_drain_step(time_elapsed)
