using System;

namespace LoanSuite.Api.Models
{
    public class UpdateCustomerRequest
    {
        public int Id { get; set; }

        // ---------------- Customer Information ----------------
        public string? FullName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? MaritalStatus { get; set; }
        public string? ResidentialAddress { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? EmploymentStatus { get; set; }
        public decimal? MonthlyIncome { get; set; }
        public string? IdType { get; set; }
        public string? IdNumber { get; set; }
        public string? NIN { get; set; }
        public string? BVN { get; set; }

        // ---------------- Guarantor Information ----------------
        public UpdateGuarantorInfo? Guarantor { get; set; }
    }

    public class UpdateGuarantorInfo
    {
        public string? FullName { get; set; }
        public string? RelationshipToBorrower { get; set; }
        public string? ResidentialAddress { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
    }
}
