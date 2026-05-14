import network
import time
import gc
import sys

# 1. IMMEDIATE WIFI INITIALIZATION
def connect_wifi_early():
    wlan = network.WLAN(network.STA_IF)
    wlan.active(False)
    time.sleep(0.5)
    gc.collect()
    
    print("[INIT] Activating WiFi early...")
    wlan.active(True)
    time.sleep(1.0)
    
    ssid = "Wokwi-GUEST"
    print("[INIT] Connecting to {}...".format(ssid))
    wlan.connect(ssid, "")
    
    for _ in range(15):
        if wlan.isconnected():
            print("[INIT] WiFi Connected! IP: {}".format(wlan.ifconfig()[0]))
            return True
        time.sleep(1)
        print(".", end="")
    return False

# Attempt early connection
wifi_success = connect_wifi_early()
gc.collect()

# 2. Add module paths
sys.path.append('/config')
sys.path.append('/core')
sys.path.append('/modules')
sys.path.append('/utils')

# 3. Import utility functions (Minimal)
from helpers import log_message

if not wifi_success:
    log_message("WiFi failed. System may fail later.", "ERROR")
    
# 4. Core Imports (Done after WiFi to save RAM for RF init)
from robot import Robot, RobotState as RobotStatus
from state_machine import DroneFSM, DroneState

class RobotControllerFSM:
    def __init__(self):
        log_message("=" * 50)
        log_message("IoT Robot Delivery System (FSM) Starting...")
        log_message("=" * 50)

        # Initialize core robot
        self.robot = Robot()
        self.fsm = DroneFSM(self.robot)

        # Use existing network interface if available
        from wifi_manager import WiFiManager
        self.wifi_manager = WiFiManager()
        self.wifi_manager.initialized = True
        self.wifi_manager.wlan = network.WLAN(network.STA_IF)

        self.auth_manager = None
        self.gps_simulator = None
        self.battery_manager = None
        self.telemetry_manager = None
        self.order_manager = None
        self.hardware_controller = None
        self.display_manager = None

        self.running = True
        self.initialized = False
        self.last_order_check_time = 0
        self.order_check_interval = 10
        self.current_waypoint_index = 0
        self.button = None 
        self.home_charging_lat = None
        self.home_charging_lon = None
        self.home_charging_node_id = None

    def initialize(self):
        log_message("Initializing robot subsystems...")
        gc.collect()

        # 1. Button
        try:
            from machine import Pin
            self.button = Pin(12, Pin.IN, Pin.PULL_UP)
            log_message("Button initialized")
        except:
            pass

        # 2. Display
        from display_manager import DisplayManager
        self.display_manager = DisplayManager()
        self.display_manager.display_boot()

        # 3. Auth
        from auth_manager import AuthManager
        self.auth_manager = AuthManager()
        
        # Already connected to WiFi in main script
        if not self.wifi_manager.wlan.isconnected():
            log_message("WiFi not connected, attempting in-manager connect...")
            if not self.wifi_manager.connect():
                self.display_manager.display_wifi_error()
                return False

        # 4. Login
        self.display_manager.display_authenticating()
        if not self.auth_manager.login():
            log_message("Auth failed", "ERROR")
            self.display_manager.display_auth_error()
            return False

        self.robot.robot_id = self.auth_manager.get_robot_id()
        self.display_manager.display_auth_success(self.robot.robot_id)
        
        # 5. Load remaining modules
        from battery_manager import BatteryManager
        from telemetry import TelemetryManager
        from order_manager import OrderManager
        from hardware_controller import HardwareController
        from gps_simulator import GPSSimulator

        self.hardware_controller = HardwareController()
        self.battery_manager = BatteryManager(self.robot, self.hardware_controller)
        self.telemetry_manager = TelemetryManager(self.robot, self.auth_manager)
        self.order_manager = OrderManager(self.robot, self.auth_manager)
        self.gps_simulator = GPSSimulator(self.robot)

        # 6. Initial Position Sync
        from config import API_CONFIG
        start_node_id = API_CONFIG.get("START_NODE", 612)
        log_message("Syncing initial position (Node {})...".format(start_node_id))
        
        start_node_is_charging_station = False
        try:
            node_info = self.telemetry_manager.fetch_node_info(start_node_id)
            if node_info:
                lat = node_info.get('latitude')
                lon = node_info.get('longitude')
                node_type = node_info.get('type', 0)
                node_type_name = node_info.get('typeName', '')

                self.robot.set_location(lat, lon, start_node_id)
                log_message("Position synced: {},{}".format(lat, lon))

                # If it's a charging station, save as home
                if node_type == 1 or node_type_name == "ChargingStation":
                    self.home_charging_lat = lat
                    self.home_charging_lon = lon
                    self.home_charging_node_id = start_node_id
                    start_node_is_charging_station = True
                    log_message("Home Charging Station set to Node {}".format(start_node_id))
        except Exception as e:
            log_message("Could not sync start position: {}".format(e), "WARNING")

        # 7. Start charging if at station
        if start_node_is_charging_station:
            self.battery_manager.start_charging()
            self.robot.set_status("Charging")
            self.fsm.transition_to(DroneState.CHARGING)

        self.initialized = True
        log_message("System initialized")
        return True

    def main_loop(self):
        """
        Main control loop with FSM
        """
        log_message("Entering main control loop with FSM...")

        while self.running:
            try:
                # Check WiFi connection
                if not self.wifi_manager.reconnect_if_needed():
                    log_message("WiFi connection lost. Retrying...", "WARNING")
                    self.display_manager.display_wifi_error()
                    time.sleep(5)
                    continue

                # Update battery
                self.battery_manager.update_battery()

                # Check for critical battery
                if self.battery_manager.check_battery_critical():
                    if not self.battery_manager.is_charging:
                        log_message("Battery critical! Emergency charging", "WARNING")
                        self.handle_emergency_battery()
                        continue

                # Update GPS position if moving
                if self.gps_simulator.is_moving:
                    still_moving = self.gps_simulator.update_position()

                    if not still_moving:
                        # Reached destination
                        self.handle_arrival_at_destination()

                # Send telemetry update
                if self.telemetry_manager.should_send_update():
                    self.telemetry_manager.send_status_update()

                # Process FSM state
                self.process_current_state()

                # Small delay to prevent excessive CPU usage
                time.sleep(0.5)

            except KeyboardInterrupt:
                log_message("Received shutdown signal", "WARNING")
                self.running = False
                break
            except Exception as e:
                log_message("Error in main loop: {}".format(str(e)), "ERROR")
                self.display_manager.display_error("Sys Error: " + str(e))
                self.fsm.handle_error(str(e))
                time.sleep(1)

    def process_current_state(self):
        """
        Process current FSM state and execute appropriate actions
        """
        state = self.fsm.get_current_state()

        if state == DroneState.IDLE:
            self.state_idle()

        elif state == DroneState.CHECK_ORDERS:
            self.state_check_orders()

        elif state == DroneState.ORDER_ASSIGNED:
            self.state_order_assigned()

        elif state == DroneState.MOTORS_ON:
            self.state_motors_on()

        elif state == DroneState.FLIGHT_TO_PICKUP:
            self.state_flight_to_pickup()

        elif state == DroneState.AT_PICKUP:
            self.state_at_pickup()

        elif state == DroneState.OPEN_COMPARTMENT_PICKUP:
            self.state_open_compartment_pickup()

        elif state == DroneState.LOADING:
            self.state_loading()

        elif state == DroneState.CLOSE_COMPARTMENT_PICKUP:
            self.state_close_compartment_pickup()

        elif state == DroneState.FLIGHT_TO_DROPOFF:
            self.state_flight_to_dropoff()

        elif state == DroneState.AT_DROPOFF:
            self.state_at_dropoff()

        elif state == DroneState.OPEN_COMPARTMENT_DROPOFF:
            self.state_open_compartment_dropoff()

        elif state == DroneState.WAIT_FOR_PICKUP:
            self.state_wait_for_pickup()

        elif state == DroneState.PACKAGE_DELIVERED:
            self.state_package_delivered()

        elif state == DroneState.CLOSE_COMPARTMENT_DROPOFF:
            self.state_close_compartment_dropoff()

        elif state == DroneState.FLIGHT_TO_CHARGING:
            self.state_flight_to_charging()

        elif state == DroneState.AT_CHARGING_STATION:
            self.state_at_charging_station()

        elif state == DroneState.CHARGING:
            self.state_charging()

        elif state == DroneState.ERROR:
            self.state_error()

    # FSM State Handlers

    def state_idle(self):
        """IDLE state: Wait and check for orders periodically"""
        self.display_manager.display_idle(self.robot)
        
        # Ensure robot status is Idle
        if self.robot.status != "Idle":
            self.robot.set_status("Idle")

        current_time = time.time()

        if current_time - self.last_order_check_time >= self.order_check_interval:
            self.last_order_check_time = current_time
            self.fsm.transition_to(DroneState.CHECK_ORDERS)

    def state_check_orders(self):
        """CHECK_ORDERS state: Fetch orders from server"""
        self.display_manager.display_checking_orders(self.robot)
        orders = self.order_manager.fetch_assigned_orders()

        if orders and len(orders) > 0:
            # Take first order
            order = orders[0]
            self.fsm.transition_to(DroneState.ORDER_ASSIGNED, {"order": order})
        else:
            # No orders, return to previous state (IDLE or CHARGING)
            if self.fsm.previous_state == DroneState.CHARGING:
                self.fsm.transition_to(DroneState.CHARGING)
            else:
                self.fsm.transition_to(DroneState.IDLE)

    def state_order_assigned(self):
        """ORDER_ASSIGNED state: Accept order and prepare"""
        order = self.fsm.get_state_data("order")

        if order:
            order_id = order.get("orderId")
            self.display_manager.display_order_assigned(self.robot, order_id)

            # Accept order on server
            if self.order_manager.accept_order(order_id):
                # Start order locally
                if self.order_manager.start_order(order):
                    self.current_waypoint_index = 0
                    self.fsm.transition_to(DroneState.MOTORS_ON)
                else:
                    log_message("Failed to start order locally", "ERROR")
                    self.fsm.transition_to(DroneState.ERROR)
            else:
                log_message("Failed to accept order on server", "ERROR")
                self.fsm.transition_to(DroneState.ERROR)
        else:
            self.fsm.transition_to(DroneState.IDLE)

    def state_motors_on(self):
        """MOTORS_ON state: Start motors and begin flight"""
        # Show preparing/motors starting
        order = self.fsm.get_state_data("order")
        order_id = order.get("orderId") if order else "..."
        self.display_manager.display_order_assigned(self.robot, order_id)
        
        self.hardware_controller.start_motors()
        self.fsm.transition_to(DroneState.FLIGHT_TO_PICKUP)

    def state_flight_to_pickup(self):
        """FLIGHT_TO_PICKUP state: Flying to pickup location"""
        self.display_manager.display_flight_to_pickup(self.robot)
        
        # Check if destination has been set (one-time setup for current waypoint)
        if not self.gps_simulator.is_moving:
            waypoints = self.order_manager.get_route_waypoints()
            if waypoints and self.current_waypoint_index < len(waypoints):
                wp = waypoints[self.current_waypoint_index]
                action = wp.get("action", "").lower()
                
                # Check if this waypoint is actually a charging stop in the middle of travel to pickup
                if "charge" in action and not "travel" in action:
                    self.fsm.transition_to(DroneState.AT_CHARGING_STATION)
                    return

                self.gps_simulator.set_destination(wp["latitude"], wp["longitude"])
                log_message("Waypoint {}/{}: Heading to {} (Action: {})".format(
                    self.current_waypoint_index + 1, len(waypoints), 
                    "({:.4f}, {:.4f})".format(wp["latitude"], wp["longitude"]),
                    action
                ))

                # Notify server about flight phase if not already in it
                self.order_manager.update_order_phase("FLIGHT_TO_PICKUP")
            else:
                # If no more waypoints but we are in FLIGHT_TO_PICKUP, we might have arrived
                # handle_arrival_at_destination will transition to AT_PICKUP
                pass

    def state_at_pickup(self):
        """AT_PICKUP state: Arrived at pickup location"""
        self.display_manager.display_at_pickup(self.robot)
        
        # Update current node to pickup node
        pickup_node_id = self.order_manager.get_pickup_node_id()
        if pickup_node_id:
            self.robot.current_node_id = pickup_node_id
            log_message("Arrived at pickup node {}".format(pickup_node_id))

        # Notify server
        self.order_manager.update_order_phase("AT_PICKUP")

        # Stop motors
        self.hardware_controller.stop_motors()

        # Transition to open compartment
        time.sleep(1)
        self.fsm.transition_to(DroneState.OPEN_COMPARTMENT_PICKUP)

    def state_open_compartment_pickup(self):
        """OPEN_COMPARTMENT_PICKUP state: Open compartment for loading"""
        self.display_manager.display_at_pickup(self.robot) # Still at pickup
        self.hardware_controller.open_compartment()
        self.fsm.transition_to(DroneState.LOADING, {"entry_time": time.time()})

    def state_loading(self):
        self.display_manager.display_loading(self.robot, 0)
        
        if self.button.value() == 0:
            log_message("Button pressed - Package loaded")
 
            time.sleep(0.2) 
        
            self.fsm.transition_to(DroneState.CLOSE_COMPARTMENT_PICKUP)

    def state_close_compartment_pickup(self):
        """CLOSE_COMPARTMENT_PICKUP state: Close compartment after loading"""
        self.display_manager.display_custom_message("Package Loaded!", "Closing hatch...", "Preparing for", "takeoff")
        self.hardware_controller.close_compartment()
        
        # Set payload weight for battery consumption calculations
        if self.order_manager.current_order:
            self.robot.payload_weight = self.order_manager.current_order.get("weight", 0)
            log_message("Payload weight set to {} kg".format(self.robot.payload_weight))
            
        self.fsm.transition_to(DroneState.FLIGHT_TO_DROPOFF)

    def state_flight_to_dropoff(self):
        """FLIGHT_TO_DROPOFF state: Flying to dropoff location"""
        self.display_manager.display_flight_to_dropoff(self.robot)
        
        # Check if destination has been set (one-time setup for current waypoint)
        if not self.gps_simulator.is_moving:
            waypoints = self.order_manager.get_route_waypoints()
            if waypoints and self.current_waypoint_index < len(waypoints):
                wp = waypoints[self.current_waypoint_index]
                action = wp.get("action", "").lower()
                
                # Check if this waypoint is actually a charging stop
                if "charge" in action and not "travel" in action:
                    self.fsm.transition_to(DroneState.AT_CHARGING_STATION)
                    return

                # Start motors if they were off (e.g. after intermediate charging)
                self.hardware_controller.start_motors()

                self.gps_simulator.set_destination(wp["latitude"], wp["longitude"])
                log_message("Waypoint {}/{}: Heading to {} (Action: {})".format(
                    self.current_waypoint_index + 1, len(waypoints), 
                    "({:.4f}, {:.4f})".format(wp["latitude"], wp["longitude"]),
                    action
                ))

                # Notify server about flight phase
                self.order_manager.update_order_phase("FLIGHT_TO_DROPOFF")
            else:
                pass

    def state_at_dropoff(self):
        """AT_DROPOFF state: Arrived at dropoff location"""
        self.display_manager.display_at_dropoff(self.robot)
        
        # Update current node to dropoff node
        dropoff_node_id = self.order_manager.get_dropoff_node_id()
        if dropoff_node_id:
            self.robot.current_node_id = dropoff_node_id
            log_message("Arrived at dropoff node {}".format(dropoff_node_id))

        # Notify server
        self.order_manager.update_order_phase("AT_DROPOFF")

        # Stop motors
        self.hardware_controller.stop_motors()

        # Transition to open compartment
        time.sleep(1)
        self.fsm.transition_to(DroneState.OPEN_COMPARTMENT_DROPOFF)

    def state_open_compartment_dropoff(self):
        """OPEN_COMPARTMENT_DROPOFF state: Open compartment for unloading"""
        self.display_manager.display_at_dropoff(self.robot)
        self.hardware_controller.open_compartment()
        self.fsm.transition_to(DroneState.WAIT_FOR_PICKUP, {"entry_time": time.time()})

    def state_wait_for_pickup(self):
        entry_time = self.fsm.get_state_data("entry_time", time.time())
        elapsed = time.time() - entry_time
        self.display_manager.display_unloading(self.robot, elapsed)
    
        if self.button.value() == 0:
            log_message("Package picked up by recipient")
            
            time.sleep(0.2)  
            
            self.fsm.transition_to(DroneState.PACKAGE_DELIVERED)
    
        elif elapsed >= 10:
            log_message("Package pickup timeout (simulation)", "WARNING")
            self.fsm.transition_to(DroneState.PACKAGE_DELIVERED)

    def state_package_delivered(self):
        """PACKAGE_DELIVERED state: Package delivered successfully"""
        self.display_manager.display_package_delivered(self.robot)
        
        # Notify server
        self.order_manager.update_order_phase("PACKAGE_DELIVERED")

        # Complete order
        self.order_manager.complete_order()

        # Transition to close compartment
        time.sleep(1)
        self.fsm.transition_to(DroneState.CLOSE_COMPARTMENT_DROPOFF)

    def state_close_compartment_dropoff(self):
        """CLOSE_COMPARTMENT_DROPOFF state: Close compartment after delivery"""
        self.display_manager.display_custom_message("Delivery Done!", "Closing hatch...", "Return to base", "initiated")
        self.hardware_controller.close_compartment()

        # Reset payload weight
        self.robot.payload_weight = 0.0
        log_message("Payload weight reset to 0 kg")

        # Always return to charging station after delivery
        # This ensures robot is always ready and at known location
        self.fsm.transition_to(DroneState.FLIGHT_TO_CHARGING)

    def state_flight_to_charging(self):
        """FLIGHT_TO_CHARGING state: Flying to charging station"""
        self.display_manager.display_flight_to_charging(self.robot)
        
        # Check if destination has been set
        if not self.gps_simulator.is_moving:
            waypoints = self.order_manager.get_route_waypoints()
            if waypoints and self.current_waypoint_index < len(waypoints):
                wp = waypoints[self.current_waypoint_index]
                action = wp.get("action", "").lower()

                # Start motors
                self.hardware_controller.start_motors()

                self.gps_simulator.set_destination(wp["latitude"], wp["longitude"])
                log_message("Waypoint {}/{}: Returning to Base/Charging Station {} (Action: {})".format(
                    self.current_waypoint_index + 1, len(waypoints), 
                    "({:.4f}, {:.4f})".format(wp["latitude"], wp["longitude"]),
                    action
                ))

                # Notify server
                self.order_manager.update_order_phase("FLIGHT_TO_CHARGING")
            else:
                # Fallback to home charging station
                if self.home_charging_lat and self.home_charging_lon:
                    self.hardware_controller.start_motors()
                    self.gps_simulator.set_destination(
                        self.home_charging_lat,
                        self.home_charging_lon,
                        self.home_charging_node_id
                    )
                    self.order_manager.update_order_phase("FLIGHT_TO_CHARGING")
                else:
                    log_message("No more waypoints and no home station saved", "WARNING")
                    self.fsm.transition_to(DroneState.IDLE)

    def state_at_charging_station(self):
        """AT_CHARGING_STATION state: Arrived at charging station"""
        self.display_manager.display_charging(self.robot)
        
        # Stop motors
        self.hardware_controller.stop_motors()

        # Start charging
        self.battery_manager.start_charging()
        self.robot.set_status("Charging")

        # Send telemetry
        self.telemetry_manager.send_status_update(force=True)

        self.fsm.transition_to(DroneState.CHARGING)

    def state_charging(self):
        """CHARGING state: Charging battery"""
        self.display_manager.display_charging(self.robot)
        
        # Check if battery is high enough to resume (if we were in middle of order)
        if self.robot.battery_level >= 98:
            waypoints = self.order_manager.get_route_waypoints()
            if waypoints and self.current_waypoint_index < len(waypoints):
                log_message("Battery charged. Resuming mission at waypoint {}...".format(self.current_waypoint_index + 1))
                
                # Determine which state to resume to based on next waypoint action
                next_wp = waypoints[self.current_waypoint_index]
                next_action = next_wp.get("action", "").lower()
                
                if "pickup" in next_action:
                    self.fsm.transition_to(DroneState.FLIGHT_TO_PICKUP)
                elif "deliver" in next_action or "dropoff" in next_action:
                    self.fsm.transition_to(DroneState.FLIGHT_TO_DROPOFF)
                elif "charge" in next_action:
                    # Still need to charge? Just stay here
                    pass
                else:
                    # General flight
                    if self.fsm.previous_state == DroneState.FLIGHT_TO_PICKUP:
                        self.fsm.transition_to(DroneState.FLIGHT_TO_PICKUP)
                    elif self.fsm.previous_state == DroneState.FLIGHT_TO_DROPOFF:
                        self.fsm.transition_to(DroneState.FLIGHT_TO_DROPOFF)
                    else:
                        self.fsm.transition_to(DroneState.FLIGHT_TO_CHARGING)
                return

        # Regular idle/maintenance check for orders
        current_time = time.time()

        if current_time - self.last_order_check_time >= self.order_check_interval:
            self.last_order_check_time = current_time

            # Check if battery is ready for delivery (>=95%)
            if self.robot.battery_level >= 95:
                # Temporarily transition to CHECK_ORDERS to fetch assignments
                # FSM will return to CHARGING if no orders
                self.fsm.transition_to(DroneState.CHECK_ORDERS)

    def state_error(self):
        """ERROR state: Handle error condition"""
        error = self.fsm.get_state_data("error", "Unknown error")
        log_message("In ERROR state: {}".format(error), "ERROR")
        self.display_manager.display_error(str(error))

        # Stop hardware
        self.hardware_controller.stop_motors()
        self.hardware_controller.close_compartment()

        # Cancel any active order
        if self.order_manager.has_active_order():
            self.order_manager.cancel_order("Error: {}".format(error))

        # Reset robot status to Idle
        self.robot.set_status("Idle")

        # Wait a bit
        time.sleep(5)

        # Try to recover to IDLE
        self.fsm.transition_to(DroneState.IDLE)

    # Helper methods

    def handle_arrival_at_destination(self):
        """
        Handle robot arrival at destination waypoint
        """
        state = self.fsm.get_current_state()
        waypoints = self.order_manager.get_route_waypoints()
        
        if waypoints and self.current_waypoint_index < len(waypoints):
            wp = waypoints[self.current_waypoint_index]
            action = wp.get("action", "").lower()
            
            log_message("Arrived at waypoint {} (Action: {})".format(self.current_waypoint_index + 1, action))
            self.current_waypoint_index += 1
            
            # 1. Check if this waypoint marks a major transition
            if "pickup" in action and "travel" not in action:
                self.fsm.transition_to(DroneState.AT_PICKUP)
                return
            elif ("deliver" in action or "dropoff" in action) and "travel" not in action:
                self.fsm.transition_to(DroneState.AT_DROPOFF)
                return
            elif "charge" in action and "travel" not in action:
                self.fsm.transition_to(DroneState.AT_CHARGING_STATION)
                return
            
            # 2. Otherwise, we stay in the current flight state
            # The next process_current_state loop will trigger movement to the NEXT waypoint
            log_message("Continuing flight to next waypoint...")
            return

        # Fallback for manual/emergency moves
        if state == DroneState.FLIGHT_TO_PICKUP:
            self.fsm.transition_to(DroneState.AT_PICKUP)
        elif state == DroneState.FLIGHT_TO_DROPOFF:
            self.fsm.transition_to(DroneState.AT_DROPOFF)
        elif state == DroneState.FLIGHT_TO_CHARGING:
            self.fsm.transition_to(DroneState.AT_CHARGING_STATION)

    def handle_emergency_battery(self):
        """
        Handle emergency low battery situation
        """
        self.display_manager.display_low_battery_warning(self.robot)
        
        # Stop any active order
        if self.order_manager.has_active_order():
            self.order_manager.cancel_order("Emergency: Low battery")

        # Stop movement
        self.gps_simulator.stop_movement()
        self.hardware_controller.stop_motors()

        # Start charging
        self.battery_manager.start_charging()
        self.robot.set_status("Charging")

        # Send telemetry
        self.telemetry_manager.send_status_update(force=True)

        # Force FSM to charging state
        self.fsm.transition_to(DroneState.CHARGING)

    def shutdown(self):
        """
        Shutdown robot systems
        """
        log_message("Shutting down robot systems...")
        
        if self.display_manager:
            self.display_manager.shutdown()

        # Shutdown hardware
        if self.hardware_controller:
            self.hardware_controller.shutdown()

        # Send final telemetry
        if self.initialized:
            self.robot.set_status("Maintenance")
            self.telemetry_manager.send_status_update(force=True)

        # Disconnect WiFi
        self.wifi_manager.disconnect()

        log_message("Shutdown complete.")


def main():
    """
    Main entry point
    """
    # Give system time to stabilize after boot
    print("System initializing...")
    time.sleep(2)
    gc.collect()

    controller = RobotControllerFSM()

    try:
        # Initialize robot
        if controller.initialize():
            # Run main loop
            controller.main_loop()
        else:
            log_message("Initialization failed. Exiting.", "ERROR")

    except Exception as e:
        log_message("Fatal error: {}".format(str(e)), "ERROR")

    finally:
        # Cleanup
        controller.shutdown()


# Start the application
if __name__ == "__main__":
    main()
