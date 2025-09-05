namespace LoanSuite.Api.Models
{
    public class CreateUserRequest
    {
        public string FullName { get; set; } = string.Empty;  // ✅ Added FullName
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = "Operator"; // default role
    }
}
