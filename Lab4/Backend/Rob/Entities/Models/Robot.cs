using Entities.Interfaces;

namespace Entities.Models
{
    public class Robot : IDbEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Model { get; set; }
        public RobotType Type { get; set; }
        public RobotStatus Status { get; set; }
        public double BatteryLevel { get; set; }

        // Battery characteristics (for energy-based calculations)
        public double BatteryCapacityJoules { get; set; } = 360000; // Default 100Wh = 360000J
        public double EnergyConsumptionPerMeterJoules { get; set; } = 36; // Default 36J per meter (10km range)
        public double HoverConsumptionPerSecondJoules { get; set; } = 100; // Default 100J per second (hovering)

        // Legacy field for backward compatibility (calculated from battery)
        public double MaxFlightRangeMeters
        {
            get => BatteryCapacityJoules / EnergyConsumptionPerMeterJoules;

        }

        // IoT Connection
        public string? IpAddress { get; set; } // IP address for Arduino connection (e.g., "192.168.1.100")
        public int? Port { get; set; } = 80; // Default HTTP port for Arduino web server

        // Authentication fields for IoT devices
        public string? SerialNumber { get; set; } // Unique identifier
        public string? AccessKeyHash { get; set; } // Hashed access key for authentication

        // Location tracking
        public int? CurrentNodeId { get; set; }
        public virtual Node? CurrentNode { get; set; }

        public double? CurrentLatitude { get; set; } // GPS coordinates when not at a node
        public double? CurrentLongitude { get; set; }

        public int? TargetNodeId { get; set; } // Destination node (null if idle)
        public virtual Node? TargetNode { get; set; }

        public virtual ICollection<Order> ActiveOrders { get; set; }
    }
}