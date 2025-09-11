namespace LoanSuite.Api.Models.Payments
{
    public class PaymentDto
    {
        public int Id { get; set; }
        public int LoanId { get; set; }
        public int RepaymentScheduleId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? ReviewedAt { get; set; }
        public string? ReviewedBy { get; set; }
        public string FullName { get; set; } = string.Empty; // ✅ Borrower full name
    }
}
