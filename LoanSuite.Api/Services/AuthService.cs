using LoanSuite.Api.Models;
using LoanSuite.Core.Entities;
using LoanSuite.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace LoanSuite.Api.Services
{
    public class AuthService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // ==================== LOGIN ====================
        public async Task<LoginResponse?> LoginAsync(LoginRequest request)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);

            if (user == null)
                return null;

            // Verify password (plain for now, replace with hashing in prod)
            if (user.PasswordHash != request.Password)
                return null;

            var token = GenerateJwtToken(user);

            return new LoginResponse
            {
                Token = token,
                Role = NormalizeRole(user.Role),
                Email = user.Email
            };
        }

        private string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // ✅ Normalize role (Admin / Operator)
            var normalizedRole = char.ToUpper(user.Role[0]) + user.Role.Substring(1).ToLower();

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(ClaimTypes.Role, normalizedRole)
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["ExpiryMinutes"]!)),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // ==================== CREATE USER ====================
        public async Task<User?> CreateUserAsync(CreateUserRequest request)
        {
            // Ensure email is unique
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                return null; // Email already exists

            var newUser = new User
            {
                FullName = request.FullName,  // ✅ Added
                Email = request.Email,
                PasswordHash = request.Password,
                Role = NormalizeRole(request.Role),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return newUser;
        }

        // ==================== RESET PASSWORD ====================
        public async Task<bool> ResetPasswordAsync(int userId, string newPassword)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            user.PasswordHash = newPassword;
            await _context.SaveChangesAsync();
            return true;
        }

        // ==================== DELETE USER ====================
        public async Task<bool> DeleteUserAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }

        // ==================== UPDATE USER ====================
        public async Task<User?> UpdateUserAsync(int userId, UpdateUserRequest request)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return null;

            user.FullName = request.FullName ?? user.FullName; // ✅ Added
            user.Email = request.Email ?? user.Email;
            user.PasswordHash = request.Password ?? user.PasswordHash;

            if (!string.IsNullOrEmpty(request.Role))
            {
                user.Role = NormalizeRole(request.Role);
            }

            await _context.SaveChangesAsync();
            return user;
        }

        // ==================== GET ALL USERS ====================
        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _context.Users
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();
        }

        // ==================== HELPER ====================
        private string NormalizeRole(string role)
        {
            if (string.IsNullOrWhiteSpace(role)) return "Operator"; // default fallback
            role = role.Trim().ToLower();
            return role switch
            {
                "admin" => "Admin",
                "operator" => "Operator",
                _ => "Operator" // fallback
            };
        }
    }
}
