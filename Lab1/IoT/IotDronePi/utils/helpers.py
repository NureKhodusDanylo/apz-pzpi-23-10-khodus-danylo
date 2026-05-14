import math
import time

def log_message(message, level="INFO"):
    """
    Print formatted log message with timestamp

    Args:
        message: Message to log
        level: Log level (INFO, WARNING, ERROR, DEBUG)
    """
    timestamp = time.localtime()
    time_str = "{:02d}:{:02d}:{:02d}".format(
        timestamp[3], timestamp[4], timestamp[5]
    )
    print("[{}] [{}] {}".format(time_str, level, message))

def calculate_distance(lat1, lon1, lat2, lon2):
    """
    Calculate distance between two GPS coordinates using Haversine formula

    Args:
        lat1, lon1: First point coordinates
        lat2, lon2: Second point coordinates

    Returns:
        Distance in meters
    """
    R = 6371000  # Earth radius in meters

    lat1_rad = math.radians(lat1)
    lat2_rad = math.radians(lat2)
    delta_lat = math.radians(lat2 - lat1)
    delta_lon = math.radians(lon2 - lon1)

    a = (math.sin(delta_lat / 2) ** 2 +
         math.cos(lat1_rad) * math.cos(lat2_rad) *
         math.sin(delta_lon / 2) ** 2)
    c = 2 * math.atan2(math.sqrt(a), math.sqrt(1 - a))

    distance = R * c
    return distance

def calculate_bearing(lat1, lon1, lat2, lon2):
    """
    Calculate bearing between two GPS coordinates

    Args:
        lat1, lon1: Starting point coordinates
        lat2, lon2: Target point coordinates

    Returns:
        Bearing in degrees (0-360)
    """
    lat1_rad = math.radians(lat1)
    lat2_rad = math.radians(lat2)
    delta_lon = math.radians(lon2 - lon1)

    x = math.sin(delta_lon) * math.cos(lat2_rad)
    y = (math.cos(lat1_rad) * math.sin(lat2_rad) -
         math.sin(lat1_rad) * math.cos(lat2_rad) * math.cos(delta_lon))

    bearing = math.atan2(x, y)
    bearing = math.degrees(bearing)
    bearing = (bearing + 360) % 360

    return bearing

def move_coordinates(lat, lon, bearing, distance):
    """
    Calculate new coordinates after moving from a point

    Args:
        lat, lon: Starting coordinates
        bearing: Direction in degrees
        distance: Distance in meters

    Returns:
        Tuple of (new_latitude, new_longitude)
    """
    R = 6371000  # Earth radius in meters

    lat_rad = math.radians(lat)
    lon_rad = math.radians(lon)
    bearing_rad = math.radians(bearing)

    new_lat_rad = math.asin(
        math.sin(lat_rad) * math.cos(distance / R) +
        math.cos(lat_rad) * math.sin(distance / R) * math.cos(bearing_rad)
    )

    new_lon_rad = lon_rad + math.atan2(
        math.sin(bearing_rad) * math.sin(distance / R) * math.cos(lat_rad),
        math.cos(distance / R) - math.sin(lat_rad) * math.sin(new_lat_rad)
    )

    new_lat = math.degrees(new_lat_rad)
    new_lon = math.degrees(new_lon_rad)

    return new_lat, new_lon

def format_status(status):
    """
    Format robot status string to match API enum

    Args:
        status: Status string

    Returns:
        Formatted status string
    """
    status_map = {
        "idle": "Idle",
        "delivering": "Delivering",
        "returning": "Returning",
        "charging": "Charging",
        "maintenance": "Maintenance"
    }
    return status_map.get(status.lower(), "Idle")

def clamp(value, min_value, max_value):
    """
    Clamp value between min and max

    Args:
        value: Value to clamp
        min_value: Minimum value
        max_value: Maximum value

    Returns:
        Clamped value
    """
    return max(min_value, min(value, max_value))

def resolve_dns(hostname):
    """
    Forced DNS resolution to fix error -202 in Wokwi
    """
    try:
        import usocket
        addr_info = usocket.getaddrinfo(hostname, 80)
        return addr_info[0][-1][0]
    except Exception as e:
        log_message("DNS Error for {}: {}".format(hostname, str(e)), "ERROR")
        return None
