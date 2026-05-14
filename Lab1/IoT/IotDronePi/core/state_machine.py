"""
State Machine (FSM) for Drone Delivery System
Manages all delivery phases and state transitions
"""

import sys
sys.path.append('/utils')
from helpers import log_message


class DroneState:
    """
    Enum-like class for drone FSM states
    """
    # Idle and initialization
    IDLE = "IDLE"

    # Order fetching
    CHECK_ORDERS = "CHECK_ORDERS"
    ORDER_ASSIGNED = "ORDER_ASSIGNED"

    # Flight preparation
    MOTORS_ON = "MOTORS_ON"

    # Pickup phase
    FLIGHT_TO_PICKUP = "FLIGHT_TO_PICKUP"
    AT_PICKUP = "AT_PICKUP"
    OPEN_COMPARTMENT_PICKUP = "OPEN_COMPARTMENT_PICKUP"
    LOADING = "LOADING"
    CLOSE_COMPARTMENT_PICKUP = "CLOSE_COMPARTMENT_PICKUP"

    # Delivery phase
    FLIGHT_TO_DROPOFF = "FLIGHT_TO_DROPOFF"
    AT_DROPOFF = "AT_DROPOFF"
    OPEN_COMPARTMENT_DROPOFF = "OPEN_COMPARTMENT_DROPOFF"
    WAIT_FOR_PICKUP = "WAIT_FOR_PICKUP"
    PACKAGE_DELIVERED = "PACKAGE_DELIVERED"
    CLOSE_COMPARTMENT_DROPOFF = "CLOSE_COMPARTMENT_DROPOFF"

    # Return to charging
    FLIGHT_TO_CHARGING = "FLIGHT_TO_CHARGING"
    AT_CHARGING_STATION = "AT_CHARGING_STATION"
    CHARGING = "CHARGING"

    # Error handling
    ERROR = "ERROR"


class DroneFSM:
    """
    Finite State Machine for drone delivery operations
    Manages state transitions and validates state changes
    """

    def __init__(self, robot):
        """
        Initialize FSM

        Args:
            robot: Robot instance
        """
        self.robot = robot
        self.current_state = DroneState.IDLE
        self.previous_state = None
        self.state_data = {}  # Store data for current state

        # Define valid state transitions
        self.valid_transitions = {
            DroneState.IDLE: [DroneState.CHECK_ORDERS, DroneState.CHARGING, DroneState.ERROR],

            DroneState.CHECK_ORDERS: [DroneState.IDLE, DroneState.CHARGING, DroneState.ORDER_ASSIGNED, DroneState.ERROR],

            DroneState.ORDER_ASSIGNED: [DroneState.MOTORS_ON, DroneState.ERROR],

            DroneState.MOTORS_ON: [DroneState.FLIGHT_TO_PICKUP, DroneState.ERROR],

            DroneState.FLIGHT_TO_PICKUP: [
                DroneState.AT_PICKUP,
                DroneState.ERROR
            ],

            DroneState.AT_PICKUP: [DroneState.OPEN_COMPARTMENT_PICKUP, DroneState.ERROR],

            DroneState.OPEN_COMPARTMENT_PICKUP: [DroneState.LOADING, DroneState.ERROR],

            DroneState.LOADING: [DroneState.CLOSE_COMPARTMENT_PICKUP, DroneState.ERROR],

            DroneState.CLOSE_COMPARTMENT_PICKUP: [DroneState.FLIGHT_TO_DROPOFF, DroneState.ERROR],

            DroneState.FLIGHT_TO_DROPOFF: [
                DroneState.AT_DROPOFF,
                DroneState.ERROR
            ],

            DroneState.AT_DROPOFF: [DroneState.OPEN_COMPARTMENT_DROPOFF, DroneState.ERROR],

            DroneState.OPEN_COMPARTMENT_DROPOFF: [DroneState.WAIT_FOR_PICKUP, DroneState.ERROR],

            DroneState.WAIT_FOR_PICKUP: [DroneState.PACKAGE_DELIVERED, DroneState.ERROR],

            DroneState.PACKAGE_DELIVERED: [DroneState.CLOSE_COMPARTMENT_DROPOFF, DroneState.ERROR],

            DroneState.CLOSE_COMPARTMENT_DROPOFF: [DroneState.FLIGHT_TO_CHARGING, DroneState.IDLE, DroneState.ERROR],

            DroneState.FLIGHT_TO_CHARGING: [
                DroneState.AT_CHARGING_STATION,
                DroneState.ERROR
            ],

            DroneState.AT_CHARGING_STATION: [DroneState.CHARGING, DroneState.ERROR],

            DroneState.CHARGING: [DroneState.CHECK_ORDERS, DroneState.IDLE, DroneState.ERROR],

            DroneState.ERROR: [DroneState.IDLE]
        }

        log_message("FSM initialized in {} state".format(self.current_state))

    def can_transition_to(self, new_state):
        """
        Check if transition to new state is valid

        Args:
            new_state: Target state

        Returns:
            bool: True if transition is valid
        """
        valid_states = self.valid_transitions.get(self.current_state, [])
        return new_state in valid_states

    def transition_to(self, new_state, data=None):
        """
        Transition to a new state

        Args:
            new_state: Target state
            data: Optional data for the new state

        Returns:
            bool: True if transition succeeded
        """
        if not self.can_transition_to(new_state):
            log_message(
                "Invalid state transition: {} -> {}".format(self.current_state, new_state),
                "ERROR"
            )
            return False

        self.previous_state = self.current_state
        self.current_state = new_state

        # Update state data
        if data:
            self.state_data = data
        else:
            self.state_data = {}

        log_message("State transition: {} -> {}".format(self.previous_state, new_state))

        return True

    def get_current_state(self):
        """
        Get current FSM state

        Returns:
            str: Current state
        """
        return self.current_state

    def get_state_data(self, key, default=None):
        """
        Get data for current state

        Args:
            key: Data key
            default: Default value if key not found

        Returns:
            Data value or default
        """
        return self.state_data.get(key, default)

    def set_state_data(self, key, value):
        """
        Set data for current state

        Args:
            key: Data key
            value: Data value
        """
        self.state_data[key] = value

    def is_idle(self):
        """Check if FSM is in IDLE state"""
        return self.current_state == DroneState.IDLE

    def is_busy(self):
        """Check if FSM is busy with a delivery"""
        busy_states = [
            DroneState.ORDER_ASSIGNED,
            DroneState.MOTORS_ON,
            DroneState.FLIGHT_TO_PICKUP,
            DroneState.AT_PICKUP,
            DroneState.OPEN_COMPARTMENT_PICKUP,
            DroneState.LOADING,
            DroneState.CLOSE_COMPARTMENT_PICKUP,
            DroneState.FLIGHT_TO_DROPOFF,
            DroneState.AT_DROPOFF,
            DroneState.OPEN_COMPARTMENT_DROPOFF,
            DroneState.WAIT_FOR_PICKUP,
            DroneState.PACKAGE_DELIVERED,
            DroneState.CLOSE_COMPARTMENT_DROPOFF,
        ]
        return self.current_state in busy_states

    def is_charging(self):
        """Check if FSM is in charging states"""
        return self.current_state in [DroneState.CHARGING, DroneState.AT_CHARGING_STATION, DroneState.FLIGHT_TO_CHARGING]

    def is_flying(self):
        """Check if drone is currently flying"""
        flying_states = [
            DroneState.FLIGHT_TO_PICKUP,
            DroneState.FLIGHT_TO_DROPOFF,
            DroneState.FLIGHT_TO_CHARGING
        ]
        return self.current_state in flying_states

    def reset_to_idle(self):
        """
        Force reset FSM to IDLE state (emergency use only)
        """
        log_message("Force resetting FSM to IDLE state", "WARNING")
        self.previous_state = self.current_state
        self.current_state = DroneState.IDLE
        self.state_data = {}

    def handle_error(self, error_message):
        """
        Handle error and transition to ERROR state

        Args:
            error_message: Error description
        """
        log_message("FSM Error: {}".format(error_message), "ERROR")
        self.transition_to(DroneState.ERROR, {"error": error_message})

    def get_server_phase_name(self):
        """
        Get phase name to send to server
        Maps FSM states to server phase names

        Returns:
            str: Phase name for server
        """
        state_to_phase = {
            DroneState.FLIGHT_TO_PICKUP: "FLIGHT_TO_PICKUP",
            DroneState.AT_PICKUP: "AT_PICKUP",
            DroneState.LOADING: "LOADING",
            DroneState.FLIGHT_TO_DROPOFF: "FLIGHT_TO_DROPOFF",
            DroneState.AT_DROPOFF: "AT_DROPOFF",
            DroneState.WAIT_FOR_PICKUP: "UNLOADING",
            DroneState.PACKAGE_DELIVERED: "PACKAGE_DELIVERED",
            DroneState.FLIGHT_TO_CHARGING: "FLIGHT_TO_CHARGING",
        }

        return state_to_phase.get(self.current_state, "UNKNOWN")

    def should_notify_server(self):
        """
        Check if current state requires server notification

        Returns:
            bool: True if should notify server
        """
        notify_states = [
            DroneState.AT_PICKUP,
            DroneState.AT_DROPOFF,
            DroneState.PACKAGE_DELIVERED,
            DroneState.AT_CHARGING_STATION
        ]
        return self.current_state in notify_states

    def __str__(self):
        """
        String representation of FSM

        Returns:
            str: FSM state info
        """
        return "FSM(state={}, data={})".format(self.current_state, self.state_data)
