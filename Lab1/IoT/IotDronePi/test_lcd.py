"""
LCD Display Test - Demo of all display screens
Test file for Wokwi simulator
"""

from machine import I2C, Pin
from time import sleep
import sys

# Add module paths
sys.path.append('/libs')
sys.path.append('/modules')
sys.path.append('/core')
sys.path.append('/utils')

try:
    from display_manager import DisplayManager
except ImportError:
    print("ERROR: display_manager not found!")
    sys.exit()


# Mock Robot class for testing
class MockRobot:
    """Simulates robot data for display testing"""
    def __init__(self):
        self.robot_id = "R-007"
        self.status = "Idle"
        self.battery_level = 87.5
        self.current_latitude = 55.7558
        self.current_longitude = 37.6173
        self.current_order_id = None
        self.current_node_id = None


def test_all_screens():
    """
    Test all display screens sequentially
    """
    print("=" * 50)
    print("LCD2004 Display Test - All Screens Demo")
    print("=" * 50)

    # Initialize I2C and display
    print("\n1. Initializing I2C...")
    i2c = I2C(0, scl=Pin(22), sda=Pin(21), freq=400000)

    print("2. Scanning I2C devices...")
    devices = i2c.scan()
    if not devices:
        print("ERROR: No I2C devices found!")
        return

    print("   Found devices at:", [hex(addr) for addr in devices])

    print("\n3. Initializing display manager...")
    display = DisplayManager()

    # Create mock robot
    robot = MockRobot()

    # Test sequence
    tests = [
        # System screens
        ("BOOT SCREEN", lambda: display.display_boot(), 2),
        ("SYSTEM CHECK", lambda: display.display_system_check(), 2),

        # WiFi screens
        ("WiFi Connecting (animated)", lambda: test_animated(display.display_wifi_connecting, "MyWiFi_5G", 3), 0),
        ("WiFi Connected", lambda: display.display_wifi_connected("MyWiFi_5G", "192.168.1.105"), 2),
        ("WiFi Error", lambda: display.display_wifi_error(), 2),

        # Authentication screens
        ("Authenticating (animated)", lambda: test_animated(display.display_authenticating, None, 3), 0),
        ("Auth Success", lambda: display.display_auth_success("R-007"), 2),
        ("Auth Error", lambda: display.display_auth_error(), 2),

        # Main status screens
        ("IDLE Status (animated)", lambda: test_animated_robot(display.display_idle, robot, 5), 0),
        ("Checking Orders (animated)", lambda: test_animated_robot(display.display_checking_orders, robot, 3), 0),
        ("Order Assigned", lambda: display.display_order_assigned(robot, "8841"), 2),

        # Flight screens
        ("Flight to Pickup (animated)", lambda: test_animated_robot(display.display_flight_to_pickup, robot, 5, 150), 0),
        ("Flight to Dropoff (animated)", lambda: test_animated_robot(display.display_flight_to_dropoff, robot, 5, 75), 0),
        ("Flight to Charging (animated)", lambda: test_animated_robot(display.display_flight_to_charging, robot, 5, 200), 0),

        # Pickup/Dropoff screens
        ("At Pickup Point", lambda: display.display_at_pickup(robot), 2),
        ("Loading Package (animated)", lambda: test_loading_animation(display, robot), 0),
        ("At Dropoff Point", lambda: display.display_at_dropoff(robot), 2),
        ("Unloading Package (animated)", lambda: test_unloading_animation(display, robot), 0),
        ("Package Delivered", lambda: display.display_package_delivered(robot), 2),

        # Charging screens
        ("Charging 20% (animated)", lambda: test_charging_animation(display, robot, 20, 3), 0),
        ("Charging 50% (animated)", lambda: test_charging_animation(display, robot, 50, 3), 0),
        ("Charging 80% (animated)", lambda: test_charging_animation(display, robot, 80, 3), 0),
        ("Charging 95% Ready (animated)", lambda: test_charging_animation(display, robot, 95, 3), 0),

        # Warning screens
        ("Low Battery Warning (blinking)", lambda: test_animated_robot(display.display_low_battery_warning, robot, 4), 0),

        # Error screens
        ("Error: GPS Lost", lambda: display.display_error("GPS signal lost"), 2),
        ("Error: Server Timeout", lambda: display.display_error("Server timeout error"), 2),
        ("Maintenance Mode", lambda: display.display_maintenance(robot), 2),

        # Custom message
        ("Custom Message", lambda: display.display_custom_message(
            "Line 1: Hello!",
            "Line 2: Testing",
            "Line 3: LCD2004",
            "Line 4: Display"
        ), 2),
    ]

    print("\n4. Running display tests...")
    print("   Total tests: {}\n".format(len(tests)))

    for i, (name, test_func, delay) in enumerate(tests, 1):
        print("   [{}/ {}] {}...".format(i, len(tests), name))
        try:
            test_func()
            if delay > 0:
                sleep(delay)
        except Exception as e:
            print("      ERROR: {}".format(e))
            import sys
            sys.print_exception(e)

    print("\n5. Test sequence complete!")

    # Final screen
    display.clear()
    display.write_line("TEST COMPLETE!", 1, center=True)
    display.write_line("All {} screens OK".format(len(tests)), 2, center=True)
    sleep(3)

    # Shutdown
    print("\n6. Shutting down display...")
    display.shutdown()

    print("\n" + "=" * 50)
    print("Test finished successfully!")
    print("=" * 50)


def test_animated(func, arg, iterations):
    """Test animated screen with multiple frames"""
    for _ in range(iterations):
        if arg:
            func(arg)
        else:
            func()
        sleep(0.5)


def test_animated_robot(func, robot, iterations, distance=None):
    """Test animated screen with robot data"""
    for _ in range(iterations):
        if distance is not None:
            func(robot, distance)
            distance -= 10  # Simulate approaching
        else:
            func(robot)
        sleep(0.5)


def test_loading_animation(display, robot):
    """Test loading animation with timer"""
    for elapsed in range(6):
        display.display_loading(robot, elapsed)
        sleep(0.8)


def test_unloading_animation(display, robot):
    """Test unloading animation with timer"""
    for elapsed in range(11):
        display.display_unloading(robot, elapsed)
        sleep(0.6)


def test_charging_animation(display, robot, battery_level, iterations):
    """Test charging animation at specific battery level"""
    original_battery = robot.battery_level
    robot.battery_level = battery_level
    for _ in range(iterations):
        display.display_charging(robot)
        sleep(0.7)
    robot.battery_level = original_battery


def quick_demo():
    """
    Quick demo - just show a few key screens
    Use this for faster testing
    """
    print("Quick LCD Demo Starting...")

    display = DisplayManager()
    robot = MockRobot()

    # Boot
    display.display_boot()
    sleep(2)

    # WiFi
    display.display_wifi_connected("TestWiFi", "192.168.1.100")
    sleep(2)

    # Idle with animation
    for _ in range(5):
        display.display_idle(robot)
        sleep(0.5)

    # Flight with animation
    for distance in range(200, 0, -20):
        display.display_flight_to_pickup(robot, distance)
        sleep(0.3)

    # Charging
    for battery in range(20, 100, 10):
        robot.battery_level = battery
        display.display_charging(robot)
        sleep(0.5)

    # Done
    display.display_custom_message(
        "Demo Complete!",
        "",
        "LCD working OK",
        ""
    )
    sleep(2)

    print("Quick demo complete!")


# Main execution
if __name__ == '__main__':
    # Choose which test to run:

    # Full test (all screens)
    test_all_screens()

    # Or quick demo (uncomment to use):
    # quick_demo()
