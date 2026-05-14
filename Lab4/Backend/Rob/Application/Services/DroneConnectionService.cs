using Application.Abstractions.Interfaces;
using Application.DTOs.RobotDTOs;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace Application.Services
{
    /// <summary>
    /// Service for HTTP communication with Arduino-based drones
    /// Arduino should have a web server listening on specified port
    /// </summary>
    public class DroneConnectionService : IDroneConnectionService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<DroneConnectionService> _logger;

        public DroneConnectionService(IHttpClientFactory httpClientFactory, ILogger<DroneConnectionService> logger)
        {
            _httpClient = httpClientFactory.CreateClient("DroneClient");
            _httpClient.Timeout = TimeSpan.FromSeconds(10); // Arduino response timeout
            _logger = logger;
        }

        public async Task<DroneResponseDTO> SendDeliveryCommandAsync(string ipAddress, int port, DroneCommandDTO command)
        {
            try
            {
                var url = $"http://{ipAddress}:{port}/api/delivery";

                var jsonContent = JsonSerializer.Serialize(command, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _logger.LogInformation($"Sending delivery command to drone at {ipAddress}:{port}");

                var response = await _httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var droneResponse = JsonSerializer.Deserialize<DroneResponseDTO>(responseJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    _logger.LogInformation($"Drone responded: {droneResponse?.Message}");
                    return droneResponse ?? new DroneResponseDTO
                    {
                        Success = true,
                        Message = "Command sent successfully",
                        OrderId = command.OrderId
                    };
                }
                else
                {
                    _logger.LogWarning($"Drone returned error: {response.StatusCode}");
                    return new DroneResponseDTO
                    {
                        Success = false,
                        Message = $"HTTP {response.StatusCode}: {response.ReasonPhrase}",
                        OrderId = command.OrderId
                    };
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, $"Failed to connect to drone at {ipAddress}:{port}");
                return new DroneResponseDTO
                {
                    Success = false,
                    Message = $"Connection failed: {ex.Message}",
                    OrderId = command.OrderId
                };
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, $"Request to drone timed out: {ipAddress}:{port}");
                return new DroneResponseDTO
                {
                    Success = false,
                    Message = "Request timeout - drone not responding",
                    OrderId = command.OrderId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error communicating with drone: {ipAddress}:{port}");
                return new DroneResponseDTO
                {
                    Success = false,
                    Message = $"Error: {ex.Message}",
                    OrderId = command.OrderId
                };
            }
        }

        public async Task<bool> PingDroneAsync(string ipAddress, int port)
        {
            try
            {
                var url = $"http://{ipAddress}:{port}/api/ping";
                var response = await _httpClient.GetAsync(url);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<DroneResponseDTO> GetDroneStatusAsync(string ipAddress, int port)
        {
            try
            {
                var url = $"http://{ipAddress}:{port}/api/status";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var droneResponse = JsonSerializer.Deserialize<DroneResponseDTO>(responseJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return droneResponse ?? new DroneResponseDTO
                    {
                        Success = true,
                        Message = "Status retrieved"
                    };
                }

                return new DroneResponseDTO
                {
                    Success = false,
                    Message = $"Failed to get status: {response.StatusCode}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting drone status from {ipAddress}:{port}");
                return new DroneResponseDTO
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }

        public async Task<bool> SendEmergencyStopAsync(string ipAddress, int port)
        {
            try
            {
                var url = $"http://{ipAddress}:{port}/api/emergency-stop";
                var response = await _httpClient.PostAsync(url, null);

                _logger.LogWarning($"Emergency stop sent to drone at {ipAddress}:{port}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send emergency stop to {ipAddress}:{port}");
                return false;
            }
        }
    }
}
