#!/bin/bash
set -e

# Ensure directories exist in the persistent storage (/data)
# These might be missing if the bucket is empty
mkdir -p /data/Uploads
mkdir -p /data/Backups
chown -R 1000:1000 /data

# If the database doesn't exist in persistent storage, restore it from the seed
if [ ! -f /data/RobDelivery.db ]; then
    echo "Restoring database from seed..."
    base64 -d /app/db_seed.txt > /data/RobDelivery.db
    chown 1000:1000 /data/RobDelivery.db
    echo "Database restored successfully."
fi

# Execute the application
echo "Starting application..."
exec dotnet RobDeliveryAPI.dll
