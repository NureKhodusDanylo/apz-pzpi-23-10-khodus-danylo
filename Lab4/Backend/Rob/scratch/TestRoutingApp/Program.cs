
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Entities.Models;
using Entities.Enums;
using Entities.Interfaces;
using Application.Services;
using Application.DTOs.OrderDTOs;
using Application.Abstractions.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

class Program {
    static async Task Main() {
        var loggerMock = new Mock<ILogger<OrderService>>();
        var nodeRepoMock = new Mock<INodeRepository>();
        var robotRepoMock = new Mock<IRobotRepository>();
        var userRepoMock = new Mock<IUserRepository>();
        var orderRepoMock = new Mock<IOrderRepository>();
        var droneConnMock = new Mock<IDroneConnectionService>();
        var settingsMock = new Mock<ISettingsService>();

        // Setup nodes
        var node17 = new Node { Id = 17, Name = "Center", Latitude = 43.210084, Longitude = 27.9208, Type = NodeType.UserNode };
        var home = new Node { Id = 654, Name = "Home", Latitude = 43.231161, Longitude = 27.998216, Type = NodeType.UserNode };
        var soFresh = new Node { Id = 632, Name = "So Fresh", Latitude = 43.2326213, Longitude = 28.0122609, Type = NodeType.UserNode };
        var balkan = new Node { Id = 612, Name = "Balkan", Latitude = 43.228046, Longitude = 28.0008781, Type = NodeType.ChargingStation };
        var trakata = new Node { Id = 574, Name = "Trakata", Latitude = 43.2226114, Longitude = 27.9784979, Type = NodeType.ChargingStation };

        nodeRepoMock.Setup(r => r.GetByIdAsync(654)).ReturnsAsync(home);
        nodeRepoMock.Setup(r => r.GetByIdAsync(632)).ReturnsAsync(soFresh);
        nodeRepoMock.Setup(r => r.GetByTypeAsync(NodeType.ChargingStation)).ReturnsAsync(new List<Node> { balkan, trakata });
        nodeRepoMock.Setup(r => r.FindNearestNodeAsync(It.IsAny<double>(), It.IsAny<double>(), NodeType.ChargingStation))
            .ReturnsAsync((double lat, double lon, NodeType? t) => {
                var list = new List<Node> { balkan, trakata };
                return list.OrderBy(n => Math.Pow(n.Latitude - lat, 2) + Math.Pow(n.Longitude - lon, 2)).First();
            });

        // Setup robot
        var rob1 = new Robot { 
            Id = 1, 
            Name = "Rob1", 
            Type = RobotType.Drone, 
            Status = RobotStatus.Charging, 
            BatteryLevel = 100,
            BatteryCapacityJoules = 360000,
            EnergyConsumptionPerMeterJoules = 66,
            CurrentNodeId = 17,
            CurrentNode = node17
        };
        robotRepoMock.Setup(r => r.GetAvailableRobotsAsync()).ReturnsAsync(new List<Robot> { rob1 });

        var service = new OrderService(
            orderRepoMock.Object,
            userRepoMock.Object,
            robotRepoMock.Object,
            nodeRepoMock.Object,
            droneConnMock.Object,
            settingsMock.Object,
            loggerMock.Object
        );

        // Reflection to call private method for testing
        var method = typeof(OrderService).GetMethod("CalculateDroneRoute", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var task = (Task<(bool canComplete, List<RouteSegmentDTO> route, double totalDistance, double batteryUsage)>)method.Invoke(service, new object[] { rob1, node17, home, soFresh, 1.0 });
        var result = await task;

        Console.WriteLine($"Can Complete: {result.canComplete}");
        
        // Print all logs
        foreach (var invocation in loggerMock.Invocations.Where(i => i.Method.Name == "Log")) {
            var state = invocation.Arguments[2];
            Console.WriteLine($"LOG: {state}");
        }

        if (result.canComplete) {
            Console.WriteLine($"Total Distance: {result.totalDistance}");
            foreach (var seg in result.route) {
                Console.WriteLine($"{seg.SegmentNumber}: {seg.FromNodeName} -> {seg.ToNodeName} ({seg.Action})");
            }
        }
    }
}
