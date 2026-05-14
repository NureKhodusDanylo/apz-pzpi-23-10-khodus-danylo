using Application.Abstractions.Interfaces;
using Application.DTOs.OrderDTOs;
using Entities.Enums;
using Entities.Interfaces;
using Entities.Models;
using Microsoft.Extensions.Logging;

namespace Application.Services
{
    public class RoutingService : IRoutingService
    {
        private readonly INodeRepository _nodeRepository;
        private readonly ILogger<RoutingService> _logger;

        public RoutingService(INodeRepository nodeRepository, ILogger<RoutingService> logger)
        {
            _nodeRepository = nodeRepository;
            _logger = logger;
        }

        public async Task<(bool CanComplete, List<RouteSegmentDTO> Route, double TotalDistance, double BatteryUsage, List<string> Logs)> CalculateDroneRouteAsync(
            Robot drone,
            Node currentNode,
            Node pickupNode,
            Node dropoffNode,
            double packageWeight)
        {
            var logs = new List<string>();
            void Log(string message)
            {
                logs.Add(message);
                _logger.LogInformation(message);
            }

            Log($"Routing calculation started for drone {drone.Id} ({drone.Name})");
            Log($"Origin: {currentNode.Name}, Pickup: {pickupNode.Name}, Dropoff: {dropoffNode.Name}");
            Log($"Package Weight: {packageWeight}kg");

            var route = new List<RouteSegmentDTO>();
            double totalDistance = 0;
            int segmentNumber = 1;

            // Calculate battery metrics
            double maxRange = drone.MaxFlightRangeMeters;
            double metersPerBatteryPercentEmpty = maxRange / 100.0;
            double percentPerSecondHover = (drone.HoverConsumptionPerSecondJoules / drone.BatteryCapacityJoules) * 100.0;
            double actionDurationSeconds = 30.0; // Assume 30 seconds for loading/unloading
            double batteryUsagePerAction = percentPerSecondHover * actionDurationSeconds;
            
            Log($"Drone {drone.Id} specs: Battery={drone.BatteryLevel}%, MaxRange={Math.Round(maxRange, 0)}m, HoverUsage={Math.Round(percentPerSecondHover, 4)}%/s");
            Log($"Action Cost: {Math.Round(batteryUsagePerAction, 2)}% per pickup/delivery");

            // Step 1: Calculate route from current position to pickup (without package)
            Log("--- PHASE 1: TRAVEL TO PICKUP ---");
            var (canReachPickup, routeToPickup, distanceToPickup, batteryToPickup) = await CalculateRouteSegmentWithCharging(
                currentNode,
                pickupNode,
                drone.BatteryLevel,
                metersPerBatteryPercentEmpty,
                segmentNumber,
                "Travel to Pickup",
                logs
            );

            if (!canReachPickup)
            {
                Log("FAILURE: Could not establish path to pickup node.");
                return (false, null, 0, 0, logs);
            }

            route.AddRange(routeToPickup);
            totalDistance += distanceToPickup;
            segmentNumber += routeToPickup.Count;

            // Pickup package action
            route.Add(new RouteSegmentDTO
            {
                SegmentNumber = segmentNumber++,
                FromNodeName = pickupNode.Name,
                ToNodeName = pickupNode.Name,
                FromLatitude = pickupNode.Latitude,
                FromLongitude = pickupNode.Longitude,
                ToLatitude = pickupNode.Latitude,
                ToLongitude = pickupNode.Longitude,
                DistanceMeters = 0,
                Action = "Pickup"
            });

            // Calculate remaining battery after reaching pickup
            double remainingBatteryAfterPickup = drone.BatteryLevel - batteryToPickup;
            // If we charged during route, battery is 100% minus last segment usage
            if (routeToPickup.Any(r => r.Action == "Charge"))
            {
                var lastChargeIndex = routeToPickup.FindLastIndex(r => r.Action == "Charge");
                double distanceAfterLastCharge = routeToPickup.Skip(lastChargeIndex + 1).Sum(r => r.DistanceMeters);
                remainingBatteryAfterPickup = 100 - (distanceAfterLastCharge / metersPerBatteryPercentEmpty);
            }

            Log($"Arrival at pickup. Battery remaining before loading: {Math.Round(remainingBatteryAfterPickup, 1)}%");
            remainingBatteryAfterPickup -= batteryUsagePerAction;
            Log($"Battery after loading: {Math.Round(remainingBatteryAfterPickup, 1)}%");

            // Step 2: Calculate route from pickup to dropoff WITH package weight
            Log("--- PHASE 2: DELIVERY TO DROPOFF ---");
            double weightFactor = 1.0 / (1.0 + (packageWeight * 0.1));
            weightFactor = Math.Max(0.2, weightFactor);
            double metersPerBatteryPercentWithLoad = metersPerBatteryPercentEmpty * weightFactor;
            
            Log($"Applied weight factor {Math.Round(weightFactor, 2)}x for {packageWeight}kg load. New efficiency: {Math.Round(metersPerBatteryPercentWithLoad, 1)}m/%");

            var (canReachDropoff, routeToDropoff, distanceToDropoff, batteryToDropoff) = await CalculateRouteSegmentWithCharging(
                pickupNode,
                dropoffNode,
                remainingBatteryAfterPickup,
                metersPerBatteryPercentWithLoad,
                segmentNumber,
                "Travel",
                logs
            );

            if (!canReachDropoff)
            {
                Log("FAILURE: Could not establish path to dropoff node with current payload.");
                return (false, null, 0, 0, logs);
            }

            route.AddRange(routeToDropoff);
            totalDistance += distanceToDropoff;
            segmentNumber += routeToDropoff.Count;

            // Delivery action
            route.Add(new RouteSegmentDTO
            {
                SegmentNumber = segmentNumber++,
                FromNodeName = dropoffNode.Name,
                ToNodeName = dropoffNode.Name,
                FromLatitude = dropoffNode.Latitude,
                FromLongitude = dropoffNode.Longitude,
                ToLatitude = dropoffNode.Latitude,
                ToLongitude = dropoffNode.Longitude,
                DistanceMeters = 0,
                Action = "Deliver"
            });

            // Step 3: Calculate return route to nearest charging station after delivery
            Log("--- PHASE 3: RETURN TO BASE ---");
            double batteryAfterDropoff = remainingBatteryAfterPickup - batteryToDropoff - batteryUsagePerAction;
            
            // Search for nearest charging station from dropoff
            var nearestStation = await _nodeRepository.FindNearestNodeAsync(
                dropoffNode.Latitude,
                dropoffNode.Longitude,
                NodeType.ChargingStation
            );

            if (nearestStation != null)
            {
                var (canReturn, returnRoute, returnDistance, returnBattery) = await CalculateRouteSegmentWithCharging(
                    dropoffNode,
                    nearestStation,
                    batteryAfterDropoff,
                    metersPerBatteryPercentEmpty, // Return empty
                    segmentNumber,
                    "Return to Base",
                    logs
                );

                if (canReturn)
                {
                    route.AddRange(returnRoute);
                    totalDistance += returnDistance;
                    
                    // Final charge action at the end
                    route.Add(new RouteSegmentDTO
                    {
                        SegmentNumber = segmentNumber + returnRoute.Count,
                        FromNodeName = nearestStation.Name,
                        ToNodeName = nearestStation.Name,
                        FromLatitude = nearestStation.Latitude,
                        FromLongitude = nearestStation.Longitude,
                        ToLatitude = nearestStation.Latitude,
                        ToLongitude = nearestStation.Longitude,
                        DistanceMeters = 0,
                        Action = "Charge"
                    });
                    
                    Log($"Added return trip to {nearestStation.Name}. Final Battery: {Math.Round(batteryAfterDropoff - returnBattery, 1)}%");
                }
                else
                {
                    Log("WARNING: Cannot guarantee safe return to charging station after delivery!");
                }
            }

            double totalBatteryUsage = batteryToPickup + batteryToDropoff + (batteryUsagePerAction * 2); // Pickup + Deliver
            Log($"SUCCESS: Full route planned. Total Distance: {Math.Round(totalDistance, 0)}m. Estimated battery usage: {Math.Round(totalBatteryUsage, 1)}%");
            return (true, route, totalDistance, totalBatteryUsage, logs);
        }

        private async Task<(bool canComplete, List<RouteSegmentDTO> route, double distance, double batteryUsed)> CalculateRouteSegmentWithCharging(
            Node fromNode,
            Node toNode,
            double currentBattery,
            double metersPerBatteryPercent,
            int startSegmentNumber,
            string travelAction,
            List<string> logs)
        {
            void Log(string message) => logs.Add(message);

            var route = new List<RouteSegmentDTO>();
            double totalDistance = 0;
            double totalBatteryUsed = 0;
            int segmentNumber = startSegmentNumber;

            // If we're starting at a charging station, we can top up to 100% first
            if (fromNode.Type == NodeType.ChargingStation && currentBattery < 99)
            {
                Log($"Station {fromNode.Name} is a charging hub. Topping up from {Math.Round(currentBattery, 1)}% to 100%.");
                route.Add(new RouteSegmentDTO
                {
                    SegmentNumber = segmentNumber++,
                    FromNodeName = fromNode.Name,
                    ToNodeName = fromNode.Name,
                    FromLatitude = fromNode.Latitude,
                    FromLongitude = fromNode.Longitude,
                    ToLatitude = fromNode.Latitude,
                    ToLongitude = fromNode.Longitude,
                    DistanceMeters = 0,
                    Action = "Charge"
                });
                currentBattery = 100;
            }

            double distanceToDestination = CalculateDistance(fromNode, toNode);
            double requiredBattery = distanceToDestination / metersPerBatteryPercent;
            Log($"Segment Planning: {fromNode.Name} -> {toNode.Name}. Distance: {Math.Round(distanceToDestination, 0)}m, Battery needed: {Math.Round(requiredBattery, 1)}%, Current: {Math.Round(currentBattery, 1)}%");

            // Check if can reach destination directly with safety margin
            if (requiredBattery <= currentBattery)
            {
                double remainingBatteryAtDestination = currentBattery - requiredBattery;
                
                // Safety check: Can the drone reach the nearest charging station FROM the destination?
                var nearestChargeFromDestination = await _nodeRepository.FindNearestNodeAsync(
                    toNode.Latitude,
                    toNode.Longitude,
                    NodeType.ChargingStation,
                    new[] { toNode.Id }
                );

                double distanceToNearestCharge = nearestChargeFromDestination != null 
                    ? CalculateDistance(toNode, nearestChargeFromDestination) 
                    : 10000; 

                double batteryNeededForSafety = distanceToNearestCharge / metersPerBatteryPercent;
                
                Log($"Safety Check at {toNode.Name}: Battery remaining {Math.Round(remainingBatteryAtDestination, 1)}%. Nearest charge is {Math.Round(distanceToNearestCharge, 0)}m away (needs {Math.Round(batteryNeededForSafety, 1)}% to reach).");

                if (remainingBatteryAtDestination >= batteryNeededForSafety)
                {
                    Log($"Direct path confirmed for {travelAction}. Safe to proceed.");
                    route.Add(new RouteSegmentDTO
                    {
                        SegmentNumber = segmentNumber++,
                        FromNodeName = fromNode.Name,
                        ToNodeName = toNode.Name,
                        FromLatitude = fromNode.Latitude,
                        FromLongitude = fromNode.Longitude,
                        ToLatitude = toNode.Latitude,
                        ToLongitude = toNode.Longitude,
                        DistanceMeters = distanceToDestination,
                        Action = travelAction
                    });

                    return (true, route, distanceToDestination, requiredBattery);
                }
                
                Log($"Insufficient safety margin! Remaining {Math.Round(remainingBatteryAtDestination, 1)}% < Needed {Math.Round(batteryNeededForSafety, 1)}%. Searching for intermediate charging station...");
            }
            else
            {
                Log($"Cannot reach {toNode.Name} directly. Battery deficit of {Math.Round(requiredBattery - currentBattery, 1)}%. Searching for intermediate charging station...");
            }

            // Get all charging stations
            var allChargingStations = await _nodeRepository.GetByTypeAsync(NodeType.ChargingStation);
            var chargingStationsList = allChargingStations.Where(s => s.Id != fromNode.Id && s.Id != toNode.Id).ToList();

            if (!chargingStationsList.Any())
            {
                Log("CRITICAL: No intermediate charging stations found in database!");
                return (false, new List<RouteSegmentDTO>(), 0, 0);
            }

            Node? optimalStation = null;
            double minTotalRouteDistance = double.MaxValue;

            Log($"Evaluating {chargingStationsList.Count} charging hubs...");
            foreach (var station in chargingStationsList)
            {
                double distanceToStation = CalculateDistance(fromNode, station);
                double batteryToStation = distanceToStation / metersPerBatteryPercent;

                if (batteryToStation <= currentBattery)
                {
                    double distanceFromStationToDestination = CalculateDistance(station, toNode);
                    double batteryFromStationToDestination = distanceFromStationToDestination / metersPerBatteryPercent;

                    if (batteryFromStationToDestination <= 100)
                    {
                        var chargeFromDestination = await _nodeRepository.FindNearestNodeAsync(
                            toNode.Latitude,
                            toNode.Longitude,
                            NodeType.ChargingStation,
                            new[] { toNode.Id, station.Id }
                        );

                        if (chargeFromDestination != null)
                        {
                            double distanceDestToCharge = CalculateDistance(toNode, chargeFromDestination);
                            double batteryDestToCharge = distanceDestToCharge / metersPerBatteryPercent;
                            double remainingBatteryAtDest = 100 - batteryFromStationToDestination;

                            if (remainingBatteryAtDest >= batteryDestToCharge)
                            {
                                double totalRouteDistance = distanceToStation + distanceFromStationToDestination;
                                if (totalRouteDistance < minTotalRouteDistance)
                                {
                                    minTotalRouteDistance = totalRouteDistance;
                                    optimalStation = station;
                                }
                            }
                        }
                    }
                }
            }

            if (optimalStation == null)
            {
                Log($"FAIL: No reachable charging stations found that allow reaching {toNode.Name} with safety margin.");
                return (false, new List<RouteSegmentDTO>(), 0, 0);
            }

            Log($"Found hub: {optimalStation.Name} (+{Math.Round(CalculateDistance(fromNode, optimalStation), 0)}m). Rerouting...");
            
            double distanceToOptimalStation = CalculateDistance(fromNode, optimalStation);
            double batteryToOptimalStation = distanceToOptimalStation / metersPerBatteryPercent;

            route.Add(new RouteSegmentDTO
            {
                SegmentNumber = segmentNumber++,
                FromNodeName = fromNode.Name,
                ToNodeName = optimalStation.Name,
                FromLatitude = fromNode.Latitude,
                FromLongitude = fromNode.Longitude,
                ToLatitude = optimalStation.Latitude,
                ToLongitude = optimalStation.Longitude,
                DistanceMeters = distanceToOptimalStation,
                Action = travelAction
            });
            totalDistance += distanceToOptimalStation;
            totalBatteryUsed += batteryToOptimalStation;

            route.Add(new RouteSegmentDTO
            {
                SegmentNumber = segmentNumber++,
                FromNodeName = optimalStation.Name,
                ToNodeName = optimalStation.Name,
                FromLatitude = optimalStation.Latitude,
                FromLongitude = optimalStation.Longitude,
                ToLatitude = optimalStation.Latitude,
                ToLongitude = optimalStation.Longitude,
                DistanceMeters = 0,
                Action = "Charge"
            });

            var (canCompleteSub, remainingRoute, remainingDistance, remainingBattery) = await CalculateRouteSegmentWithCharging(
                optimalStation,
                toNode,
                100,
                metersPerBatteryPercent,
                segmentNumber,
                travelAction,
                logs
            );

            if (!canCompleteSub)
            {
                return (false, new List<RouteSegmentDTO>(), 0, 0);
            }

            route.AddRange(remainingRoute);
            totalDistance += remainingDistance;
            totalBatteryUsed += remainingBattery;

            return (true, route, totalDistance, totalBatteryUsed);
        }

        public double CalculateDistance(Node node1, Node node2)
        {
            return CalculateDistance(node1.Latitude, node1.Longitude, node2.Latitude, node2.Longitude);
        }

        public double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            // Haversine formula
            const double R = 6371000; // Earth radius in meters
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private static double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }
    }
}
