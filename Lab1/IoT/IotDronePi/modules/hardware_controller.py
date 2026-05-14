"""
Hardware Controller for ESP32 Drone
Manages GPIO pins for motors, compartment, buttons, and LEDs
"""

import time
import sys

sys.path.append('/config')
sys.path.append('/utils')

from hardware_config import GPIO_CONFIG, HARDWARE_TIMINGS, MOTOR_CONFIG
from helpers import log_message

try:
    from machine import Pin, PWM
    HARDWARE_AVAILABLE = True
except ImportError:
    # Running on PC (for testing)
    HARDWARE_AVAILABLE = False
    log_message("Hardware not available - running in simulation mode", "WARNING")


class HardwareController:
    """
    Controls all hardware components of the drone
    Motors, compartment servo, button, LEDs
    """

    def __init__(self):
        """
        Initialize hardware controller
        """
        self.hardware_available = HARDWARE_AVAILABLE

        # Initialize pins
        if self.hardware_available:
            self._init_hardware()
        else:
            self._init_simulation()

        # State tracking
        self.motors_running = False
        self.compartment_open = False
        self.last_button_time = 0

        log_message("Hardware controller initialized (hardware={})".format(self.hardware_available))

    def _init_hardware(self):
        """
        Initialize real hardware pins
        """
        # Motor pin (digital output)
        self.motor_pin = Pin(GPIO_CONFIG["MOTOR_PIN"], Pin.OUT)
        self.motor_pin.off()

        # Compartment pin (PWM for servo)
        self.compartment_pin = PWM(Pin(GPIO_CONFIG["COMPARTMENT_PIN"]))
        self.compartment_pin.freq(50)  # 50Hz for servo
        self._close_compartment_servo()

        # Button pin (input with pullup)
        self.button_pin = Pin(GPIO_CONFIG["BUTTON_PIN"], Pin.IN, Pin.PULL_UP)

        # LED pins (digital output)
        self.led_status = Pin(GPIO_CONFIG["LED_STATUS"], Pin.OUT)
        self.led_battery = Pin(GPIO_CONFIG["LED_BATTERY"], Pin.OUT)
        self.led_status.off()
        self.led_battery.off()

        log_message("Hardware pins initialized")

    def _init_simulation(self):
        """
        Initialize simulation mode (no real hardware)
        """
        self.motor_pin = None
        self.compartment_pin = None
        self.button_pin = None
        self.led_status = None
        self.led_battery = None

        log_message("Simulation mode initialized")

    # Motor control

    def start_motors(self):
        """
        Start drone motors (begin flight)
        """
        if self.motors_running:
            log_message("Motors already running", "WARNING")
            return

        log_message("Starting motors...")

        if self.hardware_available and self.motor_pin:
            self.motor_pin.on()

        self.motors_running = True
        time.sleep(MOTOR_CONFIG["STARTUP_DELAY"])

        log_message("Motors started")

    def stop_motors(self):
        """
        Stop drone motors (land)
        """
        if not self.motors_running:
            return

        log_message("Stopping motors...")

        time.sleep(MOTOR_CONFIG["SHUTDOWN_DELAY"])

        if self.hardware_available and self.motor_pin:
            self.motor_pin.off()

        self.motors_running = False

        log_message("Motors stopped")

    def are_motors_running(self):
        """
        Check if motors are running

        Returns:
            bool: True if motors running
        """
        return self.motors_running

    # Compartment control

    def open_compartment(self):
        """
        Open compartment (for loading/unloading)
        """
        if self.compartment_open:
            log_message("Compartment already open", "WARNING")
            return

        log_message("Opening compartment...")

        if self.hardware_available and self.compartment_pin:
            self._open_compartment_servo()
        else:
            # Simulation mode
            time.sleep(HARDWARE_TIMINGS["COMPARTMENT_OPEN_TIME"])

        self.compartment_open = True
        self.blink_status_led(times=2)

        log_message("Compartment opened")

    def close_compartment(self):
        """
        Close compartment (secure package)
        """
        if not self.compartment_open:
            return

        log_message("Closing compartment...")

        if self.hardware_available and self.compartment_pin:
            self._close_compartment_servo()
        else:
            # Simulation mode
            time.sleep(HARDWARE_TIMINGS["COMPARTMENT_CLOSE_TIME"])

        self.compartment_open = False
        self.blink_status_led(times=1)

        log_message("Compartment closed")

    def is_compartment_open(self):
        """
        Check if compartment is open

        Returns:
            bool: True if compartment open
        """
        return self.compartment_open

    def _open_compartment_servo(self):
        """
        Open compartment using servo (internal)
        """
        # Set servo to open position (90 degrees = ~77 duty cycle on ESP32)
        duty = self._angle_to_duty(90)
        self.compartment_pin.duty(duty)
        time.sleep(HARDWARE_TIMINGS["COMPARTMENT_OPEN_TIME"])

    def _close_compartment_servo(self):
        """
        Close compartment using servo (internal)
        """
        # Set servo to closed position (0 degrees = ~26 duty cycle on ESP32)
        duty = self._angle_to_duty(0)
        self.compartment_pin.duty(duty)
        time.sleep(HARDWARE_TIMINGS["COMPARTMENT_CLOSE_TIME"])

    def _angle_to_duty(self, angle):
        """
        Convert servo angle to PWM duty cycle

        Args:
            angle: Angle in degrees (0-180)

        Returns:
            int: Duty cycle (0-1023 for ESP32)
        """
        # Servo PWM: 0.5ms (0°) to 2.5ms (180°) pulse width at 50Hz
        # For ESP32: duty = (pulse_width_ms / 20ms) * 1023
        pulse_width_ms = 0.5 + (angle / 180.0) * 2.0
        duty = int((pulse_width_ms / 20.0) * 1023)
        return duty

    # Button control

    def is_button_pressed(self):
        """
        Check if button is pressed (with debouncing)

        Returns:
            bool: True if button pressed
        """
        current_time = time.time()

        # Check debounce
        if current_time - self.last_button_time < HARDWARE_TIMINGS["BUTTON_DEBOUNCE_TIME"]:
            return False

        if self.hardware_available and self.button_pin:
            # Button is active LOW (pressed = 0)
            pressed = self.button_pin.value() == 0
        else:
            # Simulation mode - always return False (manual trigger needed)
            pressed = False

        if pressed:
            self.last_button_time = current_time
            log_message("Button pressed!")

        return pressed

    def wait_for_button_press(self, timeout=None):
        """
        Wait for button press (blocking)

        Args:
            timeout: Maximum wait time in seconds (None = wait forever)

        Returns:
            bool: True if button pressed, False if timeout
        """
        log_message("Waiting for button press{}...".format(
            " (timeout={}s)".format(timeout) if timeout else ""
        ))

        start_time = time.time()

        while True:
            if self.is_button_pressed():
                return True

            if timeout and (time.time() - start_time) >= timeout:
                log_message("Button press timeout", "WARNING")
                return False

            time.sleep(0.1)

    # LED control

    def set_status_led(self, state):
        """
        Set status LED state

        Args:
            state: True=on, False=off
        """
        if self.hardware_available and self.led_status:
            if state:
                self.led_status.on()
            else:
                self.led_status.off()

    def set_battery_led(self, state):
        """
        Set battery warning LED state

        Args:
            state: True=on, False=off
        """
        if self.hardware_available and self.led_battery:
            if state:
                self.led_battery.on()
            else:
                self.led_battery.off()

    def blink_status_led(self, times=3, interval=None):
        """
        Blink status LED

        Args:
            times: Number of blinks
            interval: Blink interval (seconds)
        """
        if interval is None:
            interval = HARDWARE_TIMINGS["LED_BLINK_INTERVAL"]

        for _ in range(times):
            self.set_status_led(True)
            time.sleep(interval)
            self.set_status_led(False)
            time.sleep(interval)

    def shutdown(self):
        """
        Shutdown hardware controller (cleanup)
        """
        log_message("Shutting down hardware controller...")

        # Stop motors
        if self.motors_running:
            self.stop_motors()

        # Close compartment
        if self.compartment_open:
            self.close_compartment()

        # Turn off LEDs
        self.set_status_led(False)
        self.set_battery_led(False)

        log_message("Hardware controller shutdown complete")
