namespace LoanSuite.Api.Models
{
    public class UpdateUserRequest
    {
        public string? FullName { get; set; }   // ✅ allow updating full name
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? Role { get; set; }       // "Admin" or "Operator"
        public bool? IsActive { get; set; }     // ✅ allow enabling/disabling user
    }
}
