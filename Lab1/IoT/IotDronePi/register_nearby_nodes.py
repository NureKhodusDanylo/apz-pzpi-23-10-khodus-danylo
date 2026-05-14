import requests
import json
import time

# Configuration
API_BASE_URL = "http://localhost:5102/api"
ADMIN_EMAIL = "admin@test.com"
ADMIN_PASSWORD = "admin123"

# Default coordinates (Varna, Bulgaria)
DEFAULT_LAT = 43.2141
DEFAULT_LON = 27.9147
DEFAULT_RADIUS = 5000  # meters

import sys
LAT = float(sys.argv[1]) if len(sys.argv) > 1 else DEFAULT_LAT
LON = float(sys.argv[2]) if len(sys.argv) > 2 else DEFAULT_LON
RADIUS = int(sys.argv[3]) if len(sys.argv) > 3 else DEFAULT_RADIUS

OVERPASS_URL = "https://overpass-api.de/api/interpreter"

# NodeType mapping (using strings as expected by JsonSerializer)
NODE_TYPE_CHARGING = "ChargingStation"
NODE_TYPE_ORDINARY = "UserNode"

def get_nearby_locations():
    print(f"Fetching nearby locations from Overpass API (around {LAT}, {LON})...")
    query = f"""
    [out:json];
    (
      node["amenity"="cafe"](around:{RADIUS}, {LAT}, {LON});
      node["amenity"="fuel"](around:{RADIUS}, {LAT}, {LON});
      node["highway"="bus_stop"](around:{RADIUS}, {LAT}, {LON});
    );
    out body;
    """
    headers = {"User-Agent": "RobDeliveryAutoRegister/1.0"}
    response = requests.post(OVERPASS_URL, data={"data": query}, headers=headers)
    if response.status_code == 200:
        return response.json().get("elements", [])
    else:
        print(f"Error fetching from Overpass: {response.status_code}")
        return []

def login_admin():
    print("Logging in as admin...")
    login_data = {
        "email": ADMIN_EMAIL,
        "password": ADMIN_PASSWORD
    }
    # Try to register first just in case
    requests.post(f"{API_BASE_URL}/Auth/register", json={
        "userName": "AdminUser",
        "email": ADMIN_EMAIL,
        "password": ADMIN_PASSWORD
    })
    
    response = requests.post(f"{API_BASE_URL}/Auth/login", json=login_data)
    if response.status_code == 200:
        return response.json().get("token")
    else:
        print(f"Login failed: {response.status_code} - {response.text}")
        return None

def register_node(token, name, lat, lon, node_type):
    headers = {
        "Authorization": f"Bearer {token}",
        "Content-Type": "application/json"
    }
    
    # Check for duplicates first
    response = requests.get(f"{API_BASE_URL}/Node", headers=headers)
    if response.status_code == 200:
        existing_nodes = response.json()
        for node in existing_nodes:
            if node["name"] == name and abs(node["latitude"] - lat) < 0.0001 and abs(node["longitude"] - lon) < 0.0001:
                print(f"Skipping duplicate: {name}")
                return True

    payload = {
        "name": name,
        "latitude": lat,
        "longitude": lon,
        "type": node_type
    }
    response = requests.post(f"{API_BASE_URL}/Node", json=payload, headers=headers)
    if response.status_code in [200, 201]:
        print(f"Successfully registered: {name} ({node_type})")
        return True
    else:
        print(f"Failed to register {name}: {response.status_code} - {response.text}")
        return False

def main():
    token = login_admin()
    if not token:
        return

    locations = get_nearby_locations()
    if not locations:
        print("No locations found.")
        return

    print(f"Found {len(locations)} locations. Registering...")
    
    counts = {"cafe": 0, "fuel": 0, "bus_stop": 0}
    
    for loc in locations:
        tags = loc.get("tags", {})
        name = tags.get("name")
        lat = loc.get("lat")
        lon = loc.get("lon")
        
        # Determine node type and name
        if tags.get("amenity") == "cafe":
            node_type = NODE_TYPE_ORDINARY
            if not name: name = f"Cafe {loc['id']}"
            counts["cafe"] += 1
        elif tags.get("amenity") == "fuel":
            node_type = NODE_TYPE_CHARGING
            if not name: name = f"Gas Station {loc['id']}"
            counts["fuel"] += 1
        elif tags.get("highway") == "bus_stop":
            node_type = NODE_TYPE_CHARGING
            if not name: name = f"Bus Stop {loc['id']}"
            counts["bus_stop"] += 1
        else:
            continue
            
        register_node(token, name, lat, lon, node_type)
        time.sleep(0.1)

    print("\nRegistration complete!")
    print(f"Summary: Cafes: {counts['cafe']}, Gas Stations: {counts['fuel']}, Bus Stops: {counts['bus_stop']}")

if __name__ == "__main__":
    main()
