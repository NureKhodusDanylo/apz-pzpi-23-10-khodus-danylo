namespace Application.DTOs.RobotDTOs
{
    /// <summary>
    /// DTO for updating order delivery phase from robot/drone
    /// </summary>
    public class OrderPhaseUpdateDTO
    {
        /// <summary>
        /// Current phase of delivery
        /// Possible values: FLIGHT_TO_PICKUP, AT_PICKUP, LOADING, FLIGHT_TO_DROPOFF,
        /// AT_DROPOFF, UNLOADING, PACKAGE_DELIVERED, FLIGHT_TO_CHARGING
        /// </summary>
        public string Phase { get; set; } = string.Empty;

        /// <summary>
        /// Current latitude of robot
        /// </summary>
        public double? Latitude { get; set; }

        /// <summary>
        /// Current longitude of robot
        /// </summary>
        public double? Longitude { get; set; }

        /// <summary>
        /// Timestamp of phase update
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Optional message/details about the phase
        /// </summary>
        public string? Message { get; set; }
    }
}
