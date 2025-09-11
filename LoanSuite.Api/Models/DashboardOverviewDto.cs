namespace LoanSuite.Api.Models
{
    public class DashboardOverviewDto
    {
        public int TotalLoans { get; set; }
        public int PendingApprovals { get; set; }
        public int ActiveLoans { get; set; }
        public int OverdueLoans { get; set; }
        public int TotalCustomers { get; set; }
        public decimal DuePaymentsToday { get; set; }
        public decimal TotalPrincipal { get; set; }
        public decimal TotalInterest { get; set; }

        // Chart data (loans grouped by month)
        public List<LoanAnalyticsDto> LoanAnalytics { get; set; } = new();

        // Recent transactions
        public List<TransactionDto> RecentTransactions { get; set; } = new();
    }

    public class LoanAnalyticsDto
    {
        public string Month { get; set; }
        public int LoanCount { get; set; }
    }

    public class TransactionDto
    {
        public string CustomerName { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
        public DateTime Date { get; set; } // or string if frontend prefers formatted
    }
}
