using Microsoft.AspNetCore.Http;

namespace LoanSuite.Api.Models
{
    public class CreateCustomerRequest
    {
        // ---------------- Customer Information ----------------
        public string FullName { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; } = string.Empty; // Male, Female
        public string MaritalStatus { get; set; } = string.Empty; // Single, Married, Others
        public string ResidentialAddress { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string EmploymentStatus { get; set; } = string.Empty; // Employed, Self-Employed, Unemployed

        public decimal MonthlyIncome { get; set; }
        public string IdType { get; set; } = string.Empty;
        public string IdNumber { get; set; } = string.Empty;

        // New fields
        public IFormFile IdentificationDocument { get; set; }  // ID picture
        public IFormFile PassportPhoto { get; set; }          // Passport photo
        public string NIN { get; set; } = string.Empty;
        public string BVN { get; set; } = string.Empty;

        // ---------------- Guarantor Information ----------------
        public GuarantorInfo Guarantor { get; set; } = new GuarantorInfo();
    }

    public class GuarantorInfo
    {
        public string FullName { get; set; } = string.Empty;
        public string RelationshipToBorrower { get; set; } = string.Empty;
        public string ResidentialAddress { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
