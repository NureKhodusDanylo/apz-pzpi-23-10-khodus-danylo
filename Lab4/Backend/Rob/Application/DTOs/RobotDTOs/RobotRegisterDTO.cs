using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.RobotDTOs
{
    public class RobotRegisterDTO
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public string Model { get; set; }

        [Required]
        public string Type { get; set; } // "GroundCourier" or "Drone"

        [Required]
        public string SerialNumber { get; set; } // Unique identifier for the robot

        [Required]
        public string AccessKey { get; set; } // Secret key for robot authentication

        // Battery characteristics
        [Range(0, double.MaxValue)]
        public double? BatteryCapacityJoules { get; set; } // Battery capacity in Joules (e.g., 360000J = 100Wh)

        [Range(0, double.MaxValue)]
        public double? EnergyConsumptionPerMeterJoules { get; set; } // Energy consumption per meter (e.g., 36J/m for 10km range)

        [Range(0, double.MaxValue)]
        public double? HoverConsumptionPerSecondJoules { get; set; } // Energy consumption per second stationary (e.g., 100J/s)

        // IoT Connection
        public string? IpAddress { get; set; } // IP address for Arduino connection (e.g., "192.168.1.100")

        [Range(1, 65535)]
        public int? Port { get; set; } // HTTP port for Arduino web server (default 80)

        // Initial location
        public int? CurrentNodeId { get; set; } // Starting node location (optional)
    }
}
