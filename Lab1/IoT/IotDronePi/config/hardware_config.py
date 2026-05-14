"""
Hardware Configuration for ESP32 Drone
GPIO pin mappings and hardware settings
"""

# GPIO Pin Configuration
GPIO_CONFIG = {
    # Motor control (motors on/off)
    "MOTOR_PIN": 25,          # GPIO25 - Motor power (HIGH=flying, LOW=stopped)

    # Compartment control (servo for opening/closing)
    "COMPARTMENT_PIN": 26,    # GPIO26 - Compartment servo (HIGH=open, LOW=closed)

    # Button for package pickup confirmation
    "BUTTON_PIN": 12,         # GPIO12 - Button input (pullup, active LOW)

    # LED indicators
    "LED_STATUS": 32,         # GPIO32 - Status LED (blinking patterns)
    "LED_BATTERY": 33,        # GPIO33 - Battery warning LED (RED)
}

# Hardware timing settings (in seconds)
HARDWARE_TIMINGS = {
    "COMPARTMENT_OPEN_TIME": 2,      # Time to fully open compartment
    "COMPARTMENT_CLOSE_TIME": 2,     # Time to fully close compartment
    "LOADING_WAIT_TIME": 5,          # Time to wait for loading at pickup
    "BUTTON_DEBOUNCE_TIME": 0.5,     # Button debounce time
    "LED_BLINK_INTERVAL": 0.5,       # LED blink interval for status
}

# Motor settings
MOTOR_CONFIG = {
    "STARTUP_DELAY": 1,              # Delay after motor start (seconds)
    "SHUTDOWN_DELAY": 1,             # Delay before motor stop (seconds)
}

# Servo settings (for compartment)
SERVO_CONFIG = {
    "OPEN_ANGLE": 90,                # Servo angle for open position
    "CLOSED_ANGLE": 0,               # Servo angle for closed position
    "PWM_FREQUENCY": 50,             # PWM frequency for servo (Hz)
}
