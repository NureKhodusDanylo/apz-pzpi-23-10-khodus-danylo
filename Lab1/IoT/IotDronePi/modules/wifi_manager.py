import network
import time
import sys
import gc
import machine

sys.path.append('/config')
sys.path.append('/utils')

from config import WIFI_CONFIG, DEBUG
from helpers import log_message

class WiFiManager:
    """
    Manages WiFi connection for ESP32 with robust error handling
    """

    def __init__(self):
        # Run garbage collection before allocating WiFi resources
        gc.collect()

        self.wlan = None
        self.ssid = "Wokwi-GUEST"
        self.password = ""
        self.connected = False
        self.initialized = False

        # DO NOT initialize WLAN here - wait until connect() is called
        # This prevents premature WiFi initialization before system is ready
        log_message("WiFiManager created (WLAN initialization deferred)", "INFO")

    def _init_wlan(self):
        """Internal method to initialize WLAN interface with safe reset logic"""
        try:
            # Create WLAN object (singleton on ESP32)
            self.wlan = network.WLAN(network.STA_IF)
            
            # If already active and connected, don't touch it
            if self.wlan.active() and self.wlan.isconnected():
                log_message("WiFi already active and connected", "INFO")
                return True

            # If active but not connected, maybe it's fine
            if self.wlan.active():
                log_message("WiFi interface already active", "INFO")
                return True

            log_message("Activating WiFi Interface...", "INFO")
            gc.collect() # Crucial to free memory for buffers
            
            # Activate the interface
            self.wlan.active(True)
            time.sleep(1.0)

            # Verify it activated successfully
            if not self.wlan.active():
                log_message("WLAN failed to activate", "ERROR")
                return False

            log_message("WiFi interface initialized", "INFO")
            return True

        except Exception as e:
            log_message("WiFi init error: {}".format(str(e)), "ERROR")
            self.wlan = None
            return False

    @property
    def wifi_ssid(self):
        return self.ssid

    def connect(self, max_retries=20, retry_delay=1):
        """
        Connect to WiFi network
        """
        # Initialize WLAN on first connect attempt
        if not self.initialized:
            log_message("First WiFi connection - initializing WLAN...", "INFO")
            gc.collect()  # Free memory before WiFi init
            if not self._init_wlan():
                log_message("Failed to initialize WLAN interface", "ERROR")
                return False
            self.initialized = True

        # Ensure wlan is initialized
        if self.wlan is None:
            log_message("WLAN interface missing after init - critical error", "ERROR")
            return False

        if self.is_connected():
            log_message("Already connected to WiFi")
            return True

        log_message("Connecting to WiFi: {}".format(self.ssid))

        try:
            # Ensure interface is active before connecting
            if not self.wlan.active():
                log_message("Activating WLAN interface...", "INFO")
                self.wlan.active(True)
                time.sleep(0.5)

            # Disconnect if already in connecting state
            try:
                if self.wlan.status() != network.STAT_IDLE:
                    self.wlan.disconnect()
                    time.sleep(0.5)
            except:
                pass

            self.wlan.connect(self.ssid, self.password)
            log_message("WiFi connection initiated...", "INFO")
        except Exception as e:
            log_message("WiFi connect trigger error: {}".format(str(e)), "ERROR")
            return False

        retry_count = 0
        while not self.wlan.isconnected() and retry_count < max_retries:
            if DEBUG:
                print(".", end="")
            time.sleep(retry_delay)
            retry_count += 1

        if DEBUG:
            print("")

        if self.wlan.isconnected():
            self.connected = True
            try:
                ip_address = self.wlan.ifconfig()[0]
                log_message("WiFi connected! IP: {}".format(ip_address))
            except:
                log_message("WiFi connected but failed to get IP")
            return True
        else:
            self.connected = False
            log_message("Failed to connect to WiFi after {} retries".format(retry_count), "ERROR")
            return False

    def disconnect(self):
        try:
            if self.wlan:
                if self.wlan.isconnected():
                    self.wlan.disconnect()
                self.wlan.active(False)
                self.connected = False
                log_message("Disconnected from WiFi")
        except Exception as e:
            log_message(f"Error disconnecting: {e}", "ERROR")

    def is_connected(self):
        try:
            if self.wlan and self.wlan.active():
                return self.wlan.isconnected()
            return False
        except:
            return False
            
    def reconnect_if_needed(self):
        if not self.is_connected():
            return self.connect()
        return True