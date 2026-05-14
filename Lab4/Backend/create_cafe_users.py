import sqlite3
import hashlib
import requests
import sys

API_URL = "http://localhost:5102/api"
DB_PATH = "e:/Dodikuser/ХРЮНЕШНИК/Atark/ark-pzpi-23-10-khodus-danylo/Task2/arkpz-pzpi-23-10-khodus-danylo-task2/Infrastructure/DB_Storage/RobDelivery.db"

def get_hash(password):
    return hashlib.sha256(password.encode('utf-8')).hexdigest()

def login_admin():
    login_data = {"email": "admin@test.com", "password": "admin123"}
    r = requests.post(f"{API_URL}/Auth/login", json=login_data)
    if r.status_code == 200:
        return r.json().get("token")
    return None

def create_users_for_cafes():
    token = login_admin()
    if not token:
        print("Failed to login as admin.")
        return
    
    headers = {"Authorization": f"Bearer {token}"}
    
    # 1. Get all nodes
    try:
        r = requests.get(f"{API_URL}/Node", headers=headers)
        nodes = r.json()
    except Exception as e:
        print(f"Error fetching nodes: {e}")
        return

    # Filter cafes (UserNode = 0)
    cafes = [n for n in nodes if n.get('type') == 0 or n.get('typeName') == 'UserNode']
    print(f"Found {len(cafes)} cafes.")

    conn = sqlite3.connect(DB_PATH)
    cursor = conn.cursor()

    password_hash = get_hash("Cafe123!")
    
    for cafe in cafes:
        name = cafe['name']
        node_id = cafe['id']
        email = f"cafe_{node_id}@example.com"
        
        # Check if user already exists
        cursor.execute("SELECT id FROM Users WHERE Email = ?", (email,))
        if cursor.fetchone():
            print(f"User for {name} already exists. Updating node link...")
            cursor.execute("UPDATE Users SET PersonalNodeId = ? WHERE Email = ?", (node_id, email))
            continue

        print(f"Creating user for {name}...")
        # Insert into Users table
        # Columns: UserName, Email, PasswordHash, Role, PersonalNodeId
        # We need to find the actual column order or use named columns
        try:
            cursor.execute("""
                INSERT INTO Users (UserName, Email, PasswordHash, Role, PersonalNodeId)
                VALUES (?, ?, ?, ?, ?)
            """, (name, email, password_hash, 0, node_id))
        except Exception as e:
            print(f"Error inserting user {name}: {e}")

    conn.commit()
    conn.close()
    print("Done!")

if __name__ == "__main__":
    create_users_for_cafes()
