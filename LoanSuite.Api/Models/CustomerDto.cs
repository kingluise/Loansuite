namespace LoanSuite.Api.Models
{
    public class CustomerDto
    {
        public int Id { get; set; }              // ✅ CustomerId
        public string FullName { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string MaritalStatus { get; set; } = string.Empty;
        public string ResidentialAddress { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string EmploymentStatus { get; set; } = string.Empty;
        public decimal MonthlyIncome { get; set; }
        public string IdType { get; set; } = string.Empty;
        public string IdNumber { get; set; } = string.Empty;
        public string IdPhotoUrl { get; set; } = string.Empty;
        public string PassportPhotoUrl { get; set; } = string.Empty;
        public string NIN { get; set; } = string.Empty;
        public string BVN { get; set; } = string.Empty;

        // Guarantor
        public string GuarantorFullName { get; set; } = string.Empty;
        public string GuarantorRelationship { get; set; } = string.Empty;
        public string GuarantorAddress { get; set; } = string.Empty;
        public string GuarantorPhone { get; set; } = string.Empty;
        public string GuarantorEmail { get; set; } = string.Empty;

        // ✅ Add CreatedAt so you can track registration date
        public DateTime CreatedAt { get; set; }
    }

}
