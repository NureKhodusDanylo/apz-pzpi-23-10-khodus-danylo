using Application.DTOs.RobotDTOs;

namespace Application.Abstractions.Interfaces
{
    /// <summary>
    /// Service for communicating with Arduino-based drones via HTTP
    /// </summary>
    public interface IDroneConnectionService
    {
        /// <summary>
        /// Send delivery command to drone with route information
        /// </summary>
        /// <param name="ipAddress">Drone IP address</param>
        /// <param name="port">Drone HTTP port</param>
        /// <param name="command">Command data with order and route</param>
        /// <returns>Response from drone</returns>
        Task<DroneResponseDTO> SendDeliveryCommandAsync(string ipAddress, int port, DroneCommandDTO command);

        /// <summary>
        /// Check if drone is reachable and responsive
        /// </summary>
        Task<bool> PingDroneAsync(string ipAddress, int port);

        /// <summary>
        /// Get current status from drone
        /// </summary>
        Task<DroneResponseDTO> GetDroneStatusAsync(string ipAddress, int port);

        /// <summary>
        /// Emergency stop command
        /// </summary>
        Task<bool> SendEmergencyStopAsync(string ipAddress, int port);
    }
}
