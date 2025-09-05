using LoanSuite.Api.Models;
using LoanSuite.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoanSuite.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        // ==================== LOGIN ====================
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var response = await _authService.LoginAsync(request);

            if (response == null)
                return Unauthorized(new { message = "Invalid email or password" });

            return Ok(response);
        }

        // ==================== CREATE USER ====================
        [Authorize(Roles = "Admin")]
        [HttpPost("create-user")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            var user = await _authService.CreateUserAsync(request);
            if (user == null)
                return BadRequest(new { message = "Email already exists" });

            return Ok(new
            {
                user.Id,
                user.FullName,
                user.Email,
                user.Role,
                user.IsActive,
                user.CreatedAt
            });
        }

        // ==================== UPDATE USER ====================
        [Authorize(Roles = "Admin")]
        [HttpPut("update-user/{userId}")]
        public async Task<IActionResult> UpdateUser(int userId, [FromBody] UpdateUserRequest request)
        {
            var updatedUser = await _authService.UpdateUserAsync(userId, request);

            if (updatedUser == null)
                return NotFound(new { message = "User not found" });

            return Ok(new
            {
                updatedUser.Id,
                updatedUser.FullName,
                updatedUser.Email,
                updatedUser.Role,
                updatedUser.IsActive,
                updatedUser.CreatedAt
            });
        }

        // ==================== RESET PASSWORD ====================
        [Authorize(Roles = "Admin")]
        [HttpPut("reset-password/{userId}")]
        public async Task<IActionResult> ResetPassword(int userId, [FromBody] string newPassword)
        {
            var result = await _authService.ResetPasswordAsync(userId, newPassword);
            if (!result)
                return NotFound(new { message = "User not found" });

            return Ok(new { message = "Password reset successfully" });
        }

        // ==================== DELETE USER ====================
        [Authorize(Roles = "Admin")]
        [HttpDelete("delete-user/{userId}")]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            var result = await _authService.DeleteUserAsync(userId);
            if (!result)
                return NotFound(new { message = "User not found" });

            return Ok(new { message = "User deleted successfully" });
        }

        // ==================== GET ALL USERS ====================
        [Authorize(Roles = "Admin")]
        [HttpGet("all-users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _authService.GetAllUsersAsync();

            var response = users.Select(user => new
            {
                user.Id,
                user.FullName,
                user.Email,
                user.Role,
                user.IsActive,
                user.CreatedAt
            });

            return Ok(response);
        }
    }
}
