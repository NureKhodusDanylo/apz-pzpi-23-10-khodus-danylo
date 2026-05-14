# ===================================
# IoT Robot Configuration
# ===================================

# WiFi Configuration
WIFI_CONFIG = {
    "SSID": "Wokwi-GUEST",
    "PASSWORD": ""
}

# API Configuration
API_CONFIG = {
    "DOMAIN": "danildrotik-rob.hf.space",
    "BASE_URL": "https://danildrotik-rob.hf.space",
    "AUTH_ENDPOINT": "/api/Auth/robot/login",
    "ROBOT_STATUS_ENDPOINT": "/api/Robot/status",
    "ROBOT_ME_ENDPOINT": "/api/Robot/me",
    "REQUEST_TIMEOUT": 5,
    "START_NODE": 612
}

# Standard headers for Localtunnel bypass
API_HEADERS = {
    "User-Agent": "Mozilla/5.0",
    "bypass-tunnel-reminder": "true",
    "Content-Type": "application/json",
    "Connection": "close"
}

# Robot Credentials (must be configured for each robot)
ROBOT_CONFIG = {
    "SERIAL_NUMBER": "SN-2026-001",
    "ACCESS_KEY": "key_12345",
    "TYPE": "Drone",  # "Drone" or "GroundCourier"
    "BATTERY_CAPACITY_JOULES": 2000000,  # 2MJ (~555Wh)
    "ENERGY_CONSUMPTION_PER_METER": 50,  # 50 J/m (40km range)
    "HOVER_CONSUMPTION_PER_SECOND": 250  # 250 J/s (approx 133 min hover time)
}


# Robot Characteristics
ROBOT_CHARACTERISTICS = {
    "TYPE": ROBOT_CONFIG["TYPE"],
    "BATTERY_CAPACITY_JOULES": ROBOT_CONFIG["BATTERY_CAPACITY_JOULES"],
    "ENERGY_CONSUMPTION_PER_METER": ROBOT_CONFIG["ENERGY_CONSUMPTION_PER_METER"],
    "HOVER_CONSUMPTION_PER_SECOND": ROBOT_CONFIG["HOVER_CONSUMPTION_PER_SECOND"],
    "MAX_SPEED_MS": 15.0,  # Increased speed to 15m/s (54 km/h)
    "MIN_BATTERY_LEVEL": 15.0  # Allow lower battery for longer missions
}

# GPS Simulation Configuration
GPS_CONFIG = {
    "START_LATITUDE": 50.0,
    "START_LONGITUDE": 36.0,
    "MOVEMENT_STEP": 0.0001,  # degrees per update (approx 11 meters)
    "UPDATE_INTERVAL": 2  # seconds
}

# Telemetry Configuration
TELEMETRY_CONFIG = {
    "UPDATE_INTERVAL": 5,  # seconds
    "BATTERY_DRAIN_RATE": 0.1  # percent per second when moving
}

# Debug Configuration
DEBUG = True
