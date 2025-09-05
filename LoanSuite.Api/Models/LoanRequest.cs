using System.ComponentModel.DataAnnotations;

namespace LoanSuite.Api.Models
{
    public class CreateLoanRequest
    {
        [Required]
        public int CustomerId { get; set; }

        [Required]
        [Range(1000, double.MaxValue, ErrorMessage = "Principal must be at least 1000")]
        public decimal Principal { get; set; }

        [Range(0, 100, ErrorMessage = "Interest rate must be between 0 and 100")]
        public double InterestRate { get; set; } = 26.5;

        [Required]
        [RegularExpression("Weekly|Monthly", ErrorMessage = "TermType must be 'Weekly' or 'Monthly'")]
        public string TermType { get; set; } = "Monthly";

        [Required]
        [Range(1, 23, ErrorMessage = "For weekly loans, DurationValue must be between 1 and 23 weeks")]
        public int DurationValue { get; set; }

        public void ValidateDuration()
        {
            if (TermType == "Weekly" && (DurationValue < 1 || DurationValue > 23))
                throw new ValidationException("Weekly loans must have duration between 1 and 23 weeks");

            if (TermType == "Monthly" && (DurationValue < 1 || DurationValue > 6))
                throw new ValidationException("Monthly loans must have duration between 1 and 6 months");
        }
    }
}
