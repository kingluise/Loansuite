namespace LoanSuite.Core.Entities
{
    public enum PaymentStatus
    {
        Pending,
        Approved,
        Rejected,
        Defaulted,
        Completed
    }

    public class Payment
    {
        public int Id { get; set; }

        public int LoanId { get; set; }
        public Loan Loan { get; set; } = null!;

        // ✅ Each Payment corresponds to exactly one RepaymentSchedule
        public int RepaymentScheduleId { get; set; }
        public RepaymentSchedule RepaymentSchedule { get; set; } = null!;

        public decimal Amount { get; set; }

        public DateTime LoggedAt { get; set; } = DateTime.UtcNow;
        public string LoggedBy { get; set; } = string.Empty;

        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
        public string? ReviewedBy { get; set; }
        public DateTime? ReviewedAt { get; set; }

        public string Reference { get; set; } = string.Empty;
    }
}
