"""
Display Manager for LCD2004 Screen
Manages display output for robot status and logs
"""

import time
import sys

sys.path.append('/libs')
sys.path.append('/utils')

from helpers import log_message

try:
    from machine import I2C, Pin
    from i2c_lcd import I2cLcd
    HARDWARE_AVAILABLE = True
except ImportError:
    HARDWARE_AVAILABLE = False
    log_message("LCD hardware not available - display disabled", "WARNING")


# Helper functions for string formatting
def str_ljust(s, width, fillchar=' '):
    """Left-justify string"""
    s = str(s)
    if len(s) >= width:
        return s[:width]
    return s + (fillchar * (width - len(s)))


def str_center(s, width, fillchar=' '):
    """Center string"""
    s = str(s)
    if len(s) >= width:
        return s[:width]
    padding = width - len(s)
    return (fillchar * (padding // 2)) + s + (fillchar * (padding - (padding // 2)))


class DisplayManager:
    """
    Manages LCD2004 display for robot status
    4 rows x 20 columns
    """

    # Custom character indices
    CHAR_BATTERY = 0
    CHAR_BOX = 1
    CHAR_LOCATION = 2
    CHAR_DRONE = 3
    CHAR_CHARGING = 4
    CHAR_WIFI = 5
    CHAR_OK = 6
    CHAR_ERROR = 7

    def __init__(self, i2c_addr=0x27, scl_pin=22, sda_pin=21):
        """
        Initialize display manager

        Args:
            i2c_addr: I2C address of LCD (default 0x27)
            scl_pin: SCL pin number (default 22)
            sda_pin: SDA pin number (default 21)
        """
        self.hardware_available = HARDWARE_AVAILABLE
        self.lcd = None
        self.animation_frame = 0

        if self.hardware_available:
            try:
                # Initialize I2C
                i2c = I2C(0, scl=Pin(scl_pin), sda=Pin(sda_pin), freq=400000)

                # Scan for devices
                devices = i2c.scan()
                if not devices:
                    log_message("No I2C devices found for LCD", "WARNING")
                    self.hardware_available = False
                    return

                # Use first device or specified address
                if i2c_addr in devices:
                    lcd_addr = i2c_addr
                else:
                    lcd_addr = devices[0]
                    log_message("LCD address 0x{:02x} not found, using 0x{:02x}".format(
                        i2c_addr, lcd_addr), "WARNING")

                # Initialize LCD (4 rows, 20 columns)
                self.lcd = I2cLcd(i2c, lcd_addr, 4, 20)

                # Create custom characters
                self._create_custom_chars()

                log_message("Display initialized at I2C 0x{:02x}".format(lcd_addr))

            except Exception as e:
                log_message("Failed to initialize LCD: {}".format(str(e)), "ERROR")
                self.hardware_available = False
        else:
            log_message("Display manager in simulation mode")

    def _create_custom_chars(self):
        """Create custom characters for icons"""
        if not self.lcd:
            return

        # Battery icon (full)
        battery_full = [
            0b01110,
            0b11111,
            0b11111,
            0b11111,
            0b11111,
            0b11111,
            0b11111,
            0b11111
        ]

        # Delivery box icon
        delivery_box = [
            0b11111,
            0b10001,
            0b11111,
            0b10001,
            0b10001,
            0b10001,
            0b10001,
            0b11111
        ]

        # Location pin icon
        location_pin = [
            0b00100,
            0b01110,
            0b11111,
            0b11111,
            0b01110,
            0b00100,
            0b00000,
            0b00000
        ]

        # Drone/aircraft icon
        drone_icon = [
            0b00100,
            0b01110,
            0b11111,
            0b00100,
            0b01010,
            0b10001,
            0b00000,
            0b00000
        ]

        # Charging icon
        charging_icon = [
            0b00100,
            0b00110,
            0b01111,
            0b00110,
            0b00100,
            0b01100,
            0b11100,
            0b01100
        ]

        # WiFi icon
        wifi_icon = [
            0b00000,
            0b01110,
            0b10001,
            0b00100,
            0b01010,
            0b00000,
            0b00100,
            0b00000
        ]

        # OK/checkmark icon
        ok_icon = [
            0b00000,
            0b00001,
            0b00011,
            0b10110,
            0b11100,
            0b01000,
            0b00000,
            0b00000
        ]

        # Error/X icon
        error_icon = [
            0b00000,
            0b10001,
            0b01010,
            0b00100,
            0b01010,
            0b10001,
            0b00000,
            0b00000
        ]

        # Load custom characters
        self.lcd.custom_char(self.CHAR_BATTERY, bytearray(battery_full))
        self.lcd.custom_char(self.CHAR_BOX, bytearray(delivery_box))
        self.lcd.custom_char(self.CHAR_LOCATION, bytearray(location_pin))
        self.lcd.custom_char(self.CHAR_DRONE, bytearray(drone_icon))
        self.lcd.custom_char(self.CHAR_CHARGING, bytearray(charging_icon))
        self.lcd.custom_char(self.CHAR_WIFI, bytearray(wifi_icon))
        self.lcd.custom_char(self.CHAR_OK, bytearray(ok_icon))
        self.lcd.custom_char(self.CHAR_ERROR, bytearray(error_icon))

    def write_line(self, text, row, center=False):
        """
        Write text to a specific row

        Args:
            text: Text to display
            row: Row number (0-3)
            center: Center the text (default False)
        """
        if not self.lcd:
            return

        text = str(text) if text is not None else ""

        if center:
            text = str_center(text[:20], 20)
        else:
            text = str_ljust(text[:20], 20)

        self.lcd.move_to(0, row)
        self.lcd.putstr(text)

    def clear(self):
        """Clear display"""
        if self.lcd:
            self.lcd.clear()

    # ==================== SYSTEM SCREENS ====================

    def display_boot(self):
        """Boot screen"""
        self.clear()
        self.write_line("====================", 0)
        self.write_line("RobDelivery System", 1, center=True)
        self.write_line("IoT Robot v2.0", 2, center=True)
        self.write_line("====================", 3)

    def display_system_check(self):
        """System check screen"""
        self.clear()
        self.write_line("System Check...", 0, center=True)
        self.write_line("", 1)
        self.write_line("Initializing...", 2, center=True)
        self.write_line("Please wait", 3, center=True)

    # ==================== WIFI SCREENS ====================

    def display_wifi_connecting(self, ssid):
        """WiFi connecting screen"""
        self.clear()
        icon = chr(self.CHAR_WIFI)
        self.write_line(icon + " Connecting WiFi...", 0)
        self.write_line(ssid[:20], 1)
        self.write_line("", 2)
        # Animated dots
        dots = "." * ((self.animation_frame % 3) + 1)
        self.write_line(dots, 3, center=True)
        self.animation_frame += 1

    def display_wifi_connected(self, ssid, ip):
        """WiFi connected screen"""
        self.clear()
        icon_wifi = chr(self.CHAR_WIFI)
        icon_ok = chr(self.CHAR_OK)
        self.write_line(icon_wifi + icon_ok + " WiFi Connected", 0)
        self.write_line("SSID: " + ssid[:14], 1)
        self.write_line("IP: " + ip[:17], 2)
        self.write_line("", 3)

    def display_wifi_error(self):
        """WiFi connection error"""
        self.clear()
        icon_wifi = chr(self.CHAR_WIFI)
        icon_error = chr(self.CHAR_ERROR)
        self.write_line(icon_wifi + icon_error + " WiFi Failed!", 0)
        self.write_line("", 1)
        self.write_line("Check credentials", 2, center=True)
        self.write_line("Retrying...", 3, center=True)

    # ==================== AUTHENTICATION SCREENS ====================

    def display_authenticating(self):
        """Authentication in progress"""
        self.clear()
        self.write_line("Authenticating...", 0, center=True)
        self.write_line("", 1)
        self.write_line("Server login", 2, center=True)
        # Animated dots
        dots = "." * ((self.animation_frame % 3) + 1)
        self.write_line(dots, 3, center=True)
        self.animation_frame += 1

    def display_auth_success(self, robot_id):
        """Authentication successful"""
        self.clear()
        icon = chr(self.CHAR_OK)
        self.write_line(icon + " Auth Success!", 0)
        self.write_line("", 1)
        self.write_line("Robot ID: " + str(robot_id), 2, center=True)
        self.write_line("", 3)

    def display_auth_error(self):
        """Authentication failed"""
        self.clear()
        icon = chr(self.CHAR_ERROR)
        self.write_line(icon + " Auth Failed!", 0)
        self.write_line("", 1)
        self.write_line("Check credentials", 2, center=True)
        self.write_line("Retrying...", 3, center=True)

    # ==================== MAIN STATUS SCREENS ====================

    def display_idle(self, robot):
        """Idle status - waiting for orders"""
        self.clear()
        # Row 0: Robot ID and status
        self.write_line("ID:{} IDLE".format(robot.robot_id), 0)
        # Row 1: Battery and location
        bat_icon = chr(self.CHAR_BATTERY)
        self.write_line("{}{}% GPS Ready".format(bat_icon, int(robot.battery_level)), 1)
        # Row 2: Ready message
        self.write_line("READY TO SERVE", 2, center=True)
        # Row 3: Waiting animation
        anim = ["|", "/", "-", "\\"]
        symbol = anim[self.animation_frame % 4]
        self.write_line("Waiting {}".format(symbol), 3, center=True)
        self.animation_frame += 1

    def display_checking_orders(self, robot):
        """Checking for orders"""
        self.clear()
        self.write_line("ID:{} Checking...".format(robot.robot_id), 0)
        bat_icon = chr(self.CHAR_BATTERY)
        self.write_line("{}{}%".format(bat_icon, int(robot.battery_level)), 1)
        self.write_line("Fetching orders", 2, center=True)
        # Animated dots
        dots = "." * ((self.animation_frame % 3) + 1)
        self.write_line(dots, 3, center=True)
        self.animation_frame += 1

    def display_order_assigned(self, robot, order_id):
        """Order assigned screen"""
        self.clear()
        box_icon = chr(self.CHAR_BOX)
        ok_icon = chr(self.CHAR_OK)
        self.write_line(box_icon + ok_icon + " Order Assigned!", 0)
        self.write_line("Order: #{}".format(order_id), 1)
        bat_icon = chr(self.CHAR_BATTERY)
        self.write_line("{}{}% Preparing...".format(bat_icon, int(robot.battery_level)), 2)
        self.write_line("Starting motors", 3, center=True)

    # ==================== FLIGHT SCREENS ====================

    def display_flight_to_pickup(self, robot, distance=None):
        """Flying to pickup location"""
        self.clear()
        drone_icon = chr(self.CHAR_DRONE)
        loc_icon = chr(self.CHAR_LOCATION)
        self.write_line(drone_icon + " TO PICKUP " + drone_icon, 0, center=True)

        if distance:
            self.write_line("Dist: {:.0f}m".format(distance), 1)
        else:
            self.write_line("En route...", 1)

        bat_icon = chr(self.CHAR_BATTERY)
        self.write_line("{}{}% {}Lat:{:.4f}".format(
            bat_icon, int(robot.battery_level), loc_icon, robot.current_latitude), 2)

        # Animated flight
        anim = ["   >", "  > ", " >  ", ">   "]
        self.write_line(anim[self.animation_frame % 4], 3, center=True)
        self.animation_frame += 1

    def display_flight_to_dropoff(self, robot, distance=None):
        """Flying to dropoff location"""
        self.clear()
        drone_icon = chr(self.CHAR_DRONE)
        box_icon = chr(self.CHAR_BOX)
        self.write_line(drone_icon + box_icon + " TO DROPOFF " + box_icon + drone_icon, 0)

        if distance:
            self.write_line("Dist: {:.0f}m".format(distance), 1)
        else:
            self.write_line("Delivering...", 1)

        bat_icon = chr(self.CHAR_BATTERY)
        self.write_line("{}{}% Speed: 5m/s".format(bat_icon, int(robot.battery_level)), 2)

        # Animated flight
        anim = [">>>", " >>", "  >", "   "]
        self.write_line(anim[self.animation_frame % 4], 3, center=True)
        self.animation_frame += 1

    def display_flight_to_charging(self, robot, distance=None):
        """Flying to charging station"""
        self.clear()
        drone_icon = chr(self.CHAR_DRONE)
        charge_icon = chr(self.CHAR_CHARGING)
        self.write_line(drone_icon + " TO CHARGING " + charge_icon, 0)

        if distance:
            self.write_line("Dist: {:.0f}m".format(distance), 1)
        else:
            self.write_line("Returning home...", 1)

        bat_icon = chr(self.CHAR_BATTERY)
        self.write_line("{}{}% RTH Mode".format(bat_icon, int(robot.battery_level)), 2)

        # Animated flight
        anim = ["<---", "-<--", "--<-", "---<"]
        self.write_line(anim[self.animation_frame % 4], 3, center=True)
        self.animation_frame += 1

    # ==================== PICKUP/DROPOFF SCREENS ====================

    def display_at_pickup(self, robot):
        """At pickup location"""
        self.clear()
        loc_icon = chr(self.CHAR_LOCATION)
        self.write_line(loc_icon + " AT PICKUP POINT", 0)
        self.write_line("Motors stopped", 1)
        bat_icon = chr(self.CHAR_BATTERY)
        self.write_line("{}{}% Landed".format(bat_icon, int(robot.battery_level)), 2)
        self.write_line("Opening hatch...", 3, center=True)

    def display_loading(self, robot, elapsed_time=0):
        """Loading package"""
        self.clear()
        box_icon = chr(self.CHAR_BOX)
        self.write_line(box_icon + " LOADING PACKAGE", 0)
        self.write_line("Hatch OPEN", 1)
        self.write_line("Wait for sender", 2, center=True)

    def display_at_dropoff(self, robot):
        """At dropoff location"""
        self.clear()
        loc_icon = chr(self.CHAR_LOCATION)
        box_icon = chr(self.CHAR_BOX)
        self.write_line(loc_icon + " AT DROPOFF " + box_icon, 0)
        self.write_line("Delivery complete", 1)
        bat_icon = chr(self.CHAR_BATTERY)
        self.write_line("{}{}% Landed".format(bat_icon, int(robot.battery_level)), 2)
        self.write_line("Opening hatch...", 3, center=True)

    def display_unloading(self, robot, elapsed_time=0):
        """Waiting for package pickup"""
        self.clear()
        box_icon = chr(self.CHAR_BOX)
        self.write_line(box_icon + " UNLOADING", 0)
        self.write_line("Hatch OPEN", 1)
        self.write_line("Wait for recipient", 2, center=True)
        # Timer
        self.write_line("Time: {}s / 10s".format(int(elapsed_time)), 3)

    def display_package_delivered(self, robot):
        """Package delivered successfully"""
        self.clear()
        ok_icon = chr(self.CHAR_OK)
        box_icon = chr(self.CHAR_BOX)
        self.write_line(ok_icon + box_icon + " DELIVERED! " + box_icon + ok_icon, 0)
        self.write_line("Package received", 1, center=True)
        bat_icon = chr(self.CHAR_BATTERY)
        self.write_line("{}{}%".format(bat_icon, int(robot.battery_level)), 2)
        self.write_line("Closing hatch...", 3, center=True)

    # ==================== CHARGING SCREENS ====================

    def display_charging(self, robot):
        """Charging battery"""
        self.clear()
        charge_icon = chr(self.CHAR_CHARGING)
        bat_icon = chr(self.CHAR_BATTERY)

        # Animated charging icon
        anim = ["CHARGING.", "CHARGING..", "CHARGING..."]
        self.write_line(charge_icon + " " + anim[self.animation_frame % 3], 0)

        # Battery level with bar
        battery_bars = int(robot.battery_level / 10)
        bar = "[" + ("=" * battery_bars) + (" " * (10 - battery_bars)) + "]"
        self.write_line("{} {}%".format(bar, int(robot.battery_level)), 1)

        # Status message
        if robot.battery_level >= 95:
            self.write_line("Ready for orders", 2, center=True)
        elif robot.battery_level >= 75:
            self.write_line("Charging in progress", 2, center=True)
        else:
            self.write_line("Low battery", 2, center=True)

        self.write_line("At charging station", 3, center=True)
        self.animation_frame += 1

    def display_low_battery_warning(self, robot):
        """Low battery warning"""
        self.clear()
        bat_icon = chr(self.CHAR_BATTERY)
        error_icon = chr(self.CHAR_ERROR)

        # Blinking warning
        if self.animation_frame % 2 == 0:
            self.write_line(error_icon + bat_icon + " LOW BATTERY! " + bat_icon + error_icon, 0)
        else:
            self.write_line("!!!!!!!!!!!!!!!!!!!!!", 0)

        self.write_line("Level: {}%".format(int(robot.battery_level)), 1, center=True)
        self.write_line("Emergency charging", 2, center=True)
        self.write_line("needed!", 3, center=True)
        self.animation_frame += 1

    # ==================== ERROR SCREENS ====================

    def display_error(self, error_message):
        """Display error"""
        self.clear()
        error_icon = chr(self.CHAR_ERROR)
        self.write_line(error_icon + " ERROR " + error_icon, 0, center=True)
        self.write_line("", 1)
        # Split error message across rows 2-3
        if len(error_message) > 20:
            self.write_line(error_message[:20], 2)
            self.write_line(error_message[20:40], 3)
        else:
            self.write_line(error_message, 2, center=True)
            self.write_line("", 3)

    def display_maintenance(self, robot):
        """Maintenance mode"""
        self.clear()
        self.write_line("MAINTENANCE MODE", 0, center=True)
        self.write_line("ID: {}".format(robot.robot_id), 1, center=True)
        bat_icon = chr(self.CHAR_BATTERY)
        self.write_line("{}{}%".format(bat_icon, int(robot.battery_level)), 2, center=True)
        self.write_line("System offline", 3, center=True)

    # ==================== UTILITY METHODS ====================

    def display_custom_message(self, line1="", line2="", line3="", line4=""):
        """Display custom message on 4 lines"""
        self.clear()
        self.write_line(line1, 0)
        self.write_line(line2, 1)
        self.write_line(line3, 2)
        self.write_line(line4, 3)

    def shutdown(self):
        """Shutdown display"""
        if self.lcd:
            self.clear()
            self.write_line("System shutdown", 1, center=True)
            self.write_line("Goodbye!", 2, center=True)
            time.sleep(2)
            self.lcd.backlight = False
