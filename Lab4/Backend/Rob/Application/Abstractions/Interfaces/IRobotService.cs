using Application.DTOs.RobotDTOs;

namespace Application.Abstractions.Interfaces
{
    public interface IRobotService
    {
        Task<RobotResponseDTO> CreateRobotAsync(CreateRobotDTO robotDto);
        Task<RobotResponseDTO?> GetRobotByIdAsync(int robotId);
        Task<IEnumerable<RobotResponseDTO>> GetAllRobotsAsync();
        Task<IEnumerable<RobotResponseDTO>> GetByStatusAsync(RobotStatus status);
        Task<IEnumerable<RobotResponseDTO>> GetByTypeAsync(RobotType type);
        Task<IEnumerable<RobotResponseDTO>> GetAvailableRobotsAsync();
        Task<bool> UpdateRobotAsync(UpdateRobotDTO robotDto);
        Task<bool> DeleteRobotAsync(int robotId);
        Task<RobotResponseDTO?> UpdateRobotStatusAsync(int robotId, RobotStatusUpdateDTO statusUpdate);

        // IoT Device Authentication
        Task<(bool Success, int? RobotId, string? ErrorMessage)> RegisterRobotAsync(RobotRegisterDTO registerDto);
        Task<(bool Success, int? RobotId, string? ErrorMessage)> AuthenticateRobotAsync(RobotLoginDTO loginDto);

        // IoT Order Management
        Task<List<OrderAssignmentDTO>> GetMyOrdersAsync(int robotId);
        Task<AcceptOrderResponseDTO> AcceptOrderAsync(int robotId, int orderId);
        Task<bool> UpdateOrderPhaseAsync(int robotId, int orderId, OrderPhaseUpdateDTO phaseUpdate);

        // Diagnostic / Testing
        Task<RoutingTestResponseDTO> RoutingTestAsync(RoutingTestDTO testDto);
    }
}
