using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.RobotDTOs
{
    public class RobotStatusUpdateDTO
    {
        [Required]
        public string Status { get; set; } // Idle, Delivering, Charging, Maintenance

        [Required]
        [Range(0, 100)]
        public double BatteryLevel { get; set; }

        public int? CurrentNodeId { get; set; } // Null if robot is in transit

        public double? CurrentLatitude { get; set; } // Current GPS coordinates if not at node
        public double? CurrentLongitude { get; set; }

        public int? TargetNodeId { get; set; } // Where robot is heading (null if idle)
    }
}
