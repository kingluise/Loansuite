using System.ComponentModel.DataAnnotations;

namespace LoanSuite.Api.Models.Payments
{
    public class InitiatePaymentRequest
    {
        [Required]
        public int LoanId { get; set; }

        [Required]
        public List<int> InstallmentIds { get; set; } = new List<int>();

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Payment amount must be greater than zero.")]
        public decimal AmountPaid { get; set; }
    }
}
