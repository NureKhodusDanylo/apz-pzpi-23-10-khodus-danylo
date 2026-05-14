namespace Application.DTOs.AdminDTOs
{
    public class SystemStatsDTO
    {
        public int TotalUsers { get; set; }
        public int TotalOrders { get; set; }
        public int TotalRobots { get; set; }
        public int TotalNodes { get; set; }

        public int ActiveOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int CancelledOrders { get; set; }

        public int AvailableRobots { get; set; }
        public int BusyRobots { get; set; }
        public int ChargingRobots { get; set; }

        public double AverageBatteryLevel { get; set; }
        public double TotalRevenue { get; set; }
    }
}
