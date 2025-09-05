namespace LoanSuite.Core.Entities
{
    public class User
    {
        public int Id { get; set; }

        // Full name of the user
        public string FullName { get; set; } = string.Empty;

        // Use email for login
        public string Email { get; set; } = string.Empty;

        // Hashed password
        public string PasswordHash { get; set; } = string.Empty;

        // Role can be "Admin" or "Operator"
        public string Role { get; set; } = "Operator";

        // If false, user cannot login
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
