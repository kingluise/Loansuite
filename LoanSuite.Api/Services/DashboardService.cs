using LoanSuite.Api.Models;
using LoanSuite.Core.Entities;
using LoanSuite.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LoanSuite.Api.Services
{
    public class DashboardService
    {
        private readonly AppDbContext _context;

        public DashboardService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardOverviewDto> GetDashboardOverviewAsync()
        {
            var totalLoans = await _context.Loans.CountAsync();

            var pendingApprovals = await _context.Loans
                .CountAsync(l => l.Status == LoanStatus.Pending);

            var activeLoans = await _context.Loans
                .CountAsync(l => l.Status == LoanStatus.Approved);

            // ✅ Overdue comes from RepaymentSchedules, not Loan
            var overdueLoans = await _context.RepaymentSchedules
                .CountAsync(r => r.Status == RepaymentStatus.Pending && r.DueDate < DateTime.UtcNow);

            var totalCustomers = await _context.Customers.CountAsync();

            // ✅ Due payments today → use TotalAmount
            var duePaymentsToday = await _context.RepaymentSchedules
                .Where(r => r.DueDate.Date == DateTime.Today && r.Status == RepaymentStatus.Pending)
                .SumAsync(r => (decimal?)r.TotalAmount) ?? 0;

            // ✅ Principal and Interest portions come from RepaymentSchedule
            var totalPrincipal = await _context.RepaymentSchedules
                .SumAsync(r => (decimal?)r.PrincipalPortion) ?? 0;

            var totalInterest = await _context.RepaymentSchedules
                .SumAsync(r => (decimal?)r.InterestPortion) ?? 0;

            // 📊 Loan Analytics (group loans by month/year, format in memory)
            var loanAnalyticsRaw = await _context.Loans
                .GroupBy(l => new { l.CreatedAt.Year, l.CreatedAt.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    LoanCount = g.Count()
                })
                .OrderBy(g => g.Year)
                .ThenBy(g => g.Month)
                .ToListAsync();

            var loanAnalytics = loanAnalyticsRaw
                .Select(x => new LoanAnalyticsDto
                {
                    Month = new DateTime(x.Year, x.Month, 1).ToString("MMM yyyy"), // ✅ formatting done in memory
                    LoanCount = x.LoanCount
                })
                .ToList();

            // 📋 Recent Transactions (latest 5 approved loans)
            var recentTransactions = await _context.Loans
                .Where(l => l.Status == LoanStatus.Approved)
                .OrderByDescending(l => l.CreatedAt) // ✅ use CreatedAt
                .Take(5)
                .Select(l => new TransactionDto
                {
                    CustomerName = l.Customer.FullName,
                    Amount = l.Principal,
                    Status = l.Status.ToString(),
                    Date = l.CreatedAt
                })
                .ToListAsync();

            return new DashboardOverviewDto
            {
                TotalLoans = totalLoans,
                PendingApprovals = pendingApprovals,
                ActiveLoans = activeLoans,
                OverdueLoans = overdueLoans,
                TotalCustomers = totalCustomers,
                DuePaymentsToday = duePaymentsToday,
                TotalPrincipal = totalPrincipal,
                TotalInterest = totalInterest,
                LoanAnalytics = loanAnalytics,
                RecentTransactions = recentTransactions
            };
        }
    }
}
