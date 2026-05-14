using Application.DTOs.MapDTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace RobDeliveryAPI.Hubs
{
    [Authorize]
    public class MapHub : Hub
    {
        public async Task SendRobotPositionUpdate(RobotMapPositionDTO robotPosition)
        {
            // Broadcast robot position update to all connected clients
            await Clients.All.SendAsync("ReceiveRobotUpdate", robotPosition);
        }

        public async Task SendNodeUpdate(NodeMapPositionDTO nodeUpdate)
        {
            // Broadcast node update to all connected clients
            await Clients.All.SendAsync("ReceiveNodeUpdate", nodeUpdate);
        }

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
            Console.WriteLine($"Client connected to MapHub: {Context.ConnectionId}");
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
            Console.WriteLine($"Client disconnected from MapHub: {Context.ConnectionId}");
        }
    }
}
