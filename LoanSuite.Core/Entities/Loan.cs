using System;
using System.Collections.Generic;

namespace LoanSuite.Core.Entities
{
    public enum LoanTermType
    {
        Weekly,
        Monthly
    }

    public enum LoanStatus
    {
        Pending,
        Approved,
        Rejected,
        Active,
        Overdue,
        Completed,
        Defaulted
    }

    public class Loan
    {
        public int Id { get; set; }

        // ---------------- Customer Info ----------------
        public int CustomerId { get; set; }
        public Customer Customer { get; set; } = null!;
        public string CustomerName => Customer?.FullName ?? string.Empty; // convenience property

        // ---------------- Loan Details ----------------
        public decimal Principal { get; set; }
        public double InterestRate { get; set; } = 26.6;

        public LoanTermType TermType { get; set; } = LoanTermType.Monthly;
        public int DurationValue { get; set; } // Number of weeks or months

        public DateTime? StartDate { get; set; } = DateTime.UtcNow;
        public DateTime? EndDate { get; set; }
        public DateTime? FirstInstallmentDate { get; set; }

        public LoanStatus Status { get; set; } = LoanStatus.Pending;

        // ---------------- Audit ----------------
        // FIX: Change data type from int to string to match the user ID from the JWT
        public string CreatedBy { get; set; }
        // FIX: Change data type from int? to string?
        public string? ReviewedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReviewedAt { get; set; }

        // ---------------- Derived Values ----------------
        public decimal TotalInterest { get; set; }
        public decimal TotalRepayment { get; set; }
        public decimal InstallmentAmount { get; set; }

        // ---------------- Navigation ----------------
        public ICollection<RepaymentSchedule> RepaymentSchedules { get; set; } = new List<RepaymentSchedule>();
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();

        // ---------------- Methods ----------------
        public void CalculateEndDateAndFirstInstallment()
        {
            if (!StartDate.HasValue) return;

            // Calculate EndDate based on term type and duration
            EndDate = TermType switch
            {
                LoanTermType.Monthly => StartDate.Value.AddMonths(DurationValue),
                LoanTermType.Weekly => StartDate.Value.AddDays(DurationValue * 7),
                _ => StartDate
            };

            // Calculate first installment date
            FirstInstallmentDate = TermType switch
            {
                LoanTermType.Monthly => StartDate.Value.AddDays(30),
                LoanTermType.Weekly => StartDate.Value.AddDays(7),
                _ => StartDate
            };
        }
    }
}
