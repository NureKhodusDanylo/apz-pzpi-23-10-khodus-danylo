using Application.Abstractions.Interfaces;
using Application.DTOs.RobotDTOs;
using Application.DTOs.UserDTOs;
using Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using IAuthService = Application.Abstractions.Interfaces.IAuthorizationService;

namespace RobDeliveryAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ITokenService _tokenService;
        private readonly IAuthService _authorizationService;
        private readonly IRobotService _robotService;

        public AuthController(
            ITokenService tokenService,
            IAuthService authorizationService,
            IRobotService robotService)
        {
            _tokenService = tokenService;
            _authorizationService = authorizationService;
            _robotService = robotService;
        }

        /// <summary>
        /// Register a new user. If account already exists — auto-login.
        /// Google registration returns NeedsAdditionalInfo if user is new (redirect to complete-profile).
        /// </summary>
        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register(UserRegisterDTO registerData)
        {
            var result = await _authorizationService.RegisterOrLoginAsync(registerData);

            if (result.Status == "Error")
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Login user. Google login auto-registers if user doesn't exist (returns NeedsAdditionalInfo).
        /// </summary>
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login(UserLoginDTO loginData)
        {
            var result = await _authorizationService.LoginOrRegisterAsync(loginData);

            if (result.Status == "Error")
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Complete Google registration with additional info (phone number, map location).
        /// Called after Google auth returns NeedsAdditionalInfo.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("complete-google-registration")]
        public async Task<IActionResult> CompleteGoogleRegistration(CompleteGoogleRegDTO data)
        {
            var result = await _authorizationService.CompleteGoogleRegistrationAsync(data);

            if (result.Status == "Error")
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        [AllowAnonymous]
        [HttpPost("robot/register")]
        public async Task<IActionResult> RegisterRobot(RobotRegisterDTO registerData)
        {
            // Use RobotService to register robot (follows Clean Architecture)
            var (success, robotId, errorMessage) = await _robotService.RegisterRobotAsync(registerData);

            if (!success)
            {
                return BadRequest(new { error = errorMessage });
            }

            // Generate token for the robot
            var token = await _tokenService.GenerateRobotToken(robotId!.Value);

            // Get robot details for response
            var robot = await _robotService.GetRobotByIdAsync(robotId.Value);

            return Ok(new
            {
                message = "Robot registered successfully",
                robotId = robotId.Value,
                serialNumber = registerData.SerialNumber,
                token
            });
        }

        [AllowAnonymous]
        [HttpPost("robot/login")]
        public async Task<IActionResult> LoginRobot(RobotLoginDTO loginData)
        {
            // Use RobotService to authenticate robot (follows Clean Architecture)
            var (success, robotId, errorMessage) = await _robotService.AuthenticateRobotAsync(loginData);

            if (!success)
            {
                return Unauthorized(new { error = errorMessage });
            }

            // Generate token for the authenticated robot
            var token = await _tokenService.GenerateRobotToken(robotId!.Value);

            // Get robot details for response
            var robot = await _robotService.GetRobotByIdAsync(robotId.Value);

            return Ok(new
            {
                message = "Robot login successful",
                robotId = robotId.Value,
                serialNumber = robot?.SerialNumber,
                robotName = robot?.Name,
                token
            });
        }
    }
}
