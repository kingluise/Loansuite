namespace LoanSuite.Core.Entities
{
    public enum RepaymentStatus
    {
        Pending,
        Paid
    }

    public class RepaymentSchedule
    {
        public int Id { get; set; }

        public int LoanId { get; set; }
        public Loan Loan { get; set; }

        public int InstallmentNo { get; set; }
        public DateTime DueDate { get; set; }

        public decimal PrincipalPortion { get; set; }
        public decimal InterestPortion { get; set; }
        public decimal TotalAmount { get; set; }

        public RepaymentStatus Status { get; set; } = RepaymentStatus.Pending;

        public DateTime? PaymentDate { get; set; }

        // ✅ One-to-one relationship: each installment can have only one Payment
        public Payment? Payment { get; set; }
    }
}
