using LoanSuite.Api.Models;
using LoanSuite.Core.Entities;
using LoanSuite.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LoanSuite.Api.Services
{
    public class LoanService
    {
        private readonly AppDbContext _context;

        public LoanService(AppDbContext context)
        {
            _context = context;
        }

        // ==================== APPLY LOAN ====================
        public async Task<Loan> ApplyLoanAsync(CreateLoanRequest request, string createdBy)
        {
            request.ValidateDuration();

            var customer = await _context.Customers.FindAsync(request.CustomerId);
            if (customer == null)
                throw new Exception("Customer not found");

            if (!Enum.TryParse<LoanTermType>(request.TermType, true, out var termType))
                throw new Exception("Invalid term type");

            decimal totalInterest = request.Principal * (decimal)(request.InterestRate / 100);
            decimal totalRepayment = request.Principal + totalInterest;

            int installmentCount = request.DurationValue;
            decimal principalPortion = request.Principal / installmentCount;
            decimal interestPortion = totalInterest / installmentCount;
            decimal installmentAmount = principalPortion + interestPortion;

            var loan = new Loan
            {
                CustomerId = customer.Id,
                Customer = customer,
                Principal = request.Principal,
                InterestRate = request.InterestRate,
                TermType = termType,
                DurationValue = request.DurationValue,
                Status = LoanStatus.Pending,
                CreatedBy = createdBy,
                StartDate = DateTime.UtcNow,
                TotalInterest = totalInterest,
                TotalRepayment = totalRepayment,
                InstallmentAmount = installmentAmount
            };

            loan.CalculateEndDateAndFirstInstallment();

            var date = loan.FirstInstallmentDate ?? DateTime.UtcNow.AddDays(termType == LoanTermType.Weekly ? 7 : 30);

            for (int i = 1; i <= installmentCount; i++)
            {
                loan.RepaymentSchedules.Add(new RepaymentSchedule
                {
                    InstallmentNo = i,
                    DueDate = date,
                    PrincipalPortion = principalPortion,
                    InterestPortion = interestPortion,
                    TotalAmount = installmentAmount,
                    Status = RepaymentStatus.Pending
                });

                date = termType == LoanTermType.Weekly ? date.AddDays(7) : date.AddMonths(1);
            }

            _context.Loans.Add(loan);
            await _context.SaveChangesAsync();

            return loan;
        }

        // ==================== GET LOAN BY ID ====================
        public async Task<Loan?> GetLoanByIdAsync(int loanId)
        {
            return await _context.Loans
                .Include(l => l.Customer)
                .Include(l => l.RepaymentSchedules)
                .FirstOrDefaultAsync(l => l.Id == loanId);
        }

        // ==================== GET LOANS PAGED ====================
        public async Task<(List<Loan> Loans, int TotalCount)> GetLoansPagedAsync(int pageNumber, int pageSize, string? status = null)
        {
            var query = _context.Loans.Include(l => l.Customer).AsQueryable();

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<LoanStatus>(status, out var loanStatus))
            {
                query = query.Where(l => l.Status == loanStatus);
            }

            int totalCount = await query.CountAsync();

            var loans = await query
                .OrderBy(l => l.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (loans, totalCount);
        }

        // ==================== APPROVE LOAN ====================
        public async Task<Loan?> ApproveLoanAsync(int loanId, string adminId)
        {
            var loan = await _context.Loans.FindAsync(loanId);
            if (loan == null || loan.Status != LoanStatus.Pending) return null;

            loan.Status = LoanStatus.Approved;
            loan.ReviewedBy = adminId;
            loan.ReviewedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return loan;
        }

        // ==================== REJECT LOAN ====================
        public async Task<Loan?> RejectLoanAsync(int loanId, string adminId)
        {
            var loan = await _context.Loans.FindAsync(loanId);
            if (loan == null || loan.Status != LoanStatus.Pending) return null;

            loan.Status = LoanStatus.Rejected;
            loan.ReviewedBy = adminId;
            loan.ReviewedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return loan;
        }

        // ==================== COMPLETE LOAN ====================
        public async Task<Loan?> CompleteLoanAsync(int loanId, string adminId)
        {
            var loan = await _context.Loans
                .Include(l => l.RepaymentSchedules)
                .FirstOrDefaultAsync(l => l.Id == loanId);

            if (loan == null || loan.Status != LoanStatus.Approved) return null;

            bool allPaid = loan.RepaymentSchedules.All(s => s.Status == RepaymentStatus.Paid);
            if (!allPaid)
                throw new InvalidOperationException("Loan cannot be marked as Completed until all installments are paid.");

            loan.Status = LoanStatus.Completed;
            loan.ReviewedBy = adminId;
            loan.ReviewedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return loan;
        }

        // ==================== DEFAULT LOAN ====================
        public async Task<Loan?> DefaultLoanAsync(int loanId, string adminId)
        {
            var loan = await _context.Loans.FindAsync(loanId);
            if (loan == null || loan.Status != LoanStatus.Approved) return null;

            loan.Status = LoanStatus.Defaulted;
            loan.ReviewedBy = adminId;
            loan.ReviewedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return loan;
        }

        // ==================== GET DUE REPAYMENTS ====================
        public async Task<List<RepaymentSchedule>> GetDueRepaymentsAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.RepaymentSchedules
                .Include(r => r.Loan)
                .ThenInclude(l => l.Customer)
                .Where(r => r.Status == RepaymentStatus.Pending && r.DueDate >= startDate && r.DueDate <= endDate)
                .OrderBy(r => r.DueDate)
                .ToListAsync();
        }

        // ==================== GET PROFIT ANALYTICS ====================
        public async Task<object> GetProfitAnalyticsAsync(
            string filterType, int? year, int? month, int? quarter,
            DateTime? startDate, DateTime? endDate)
        {
            DateTime rangeStart = DateTime.MinValue;
            DateTime rangeEnd = DateTime.MaxValue;

            switch (filterType.ToLower())
            {
                case "monthly":
                    if (year == null || month == null) throw new ArgumentException("Year and month are required for monthly filter.");
                    rangeStart = new DateTime(year.Value, month.Value, 1);
                    rangeEnd = rangeStart.AddMonths(1).AddDays(-1);
                    break;

                case "quarterly":
                    if (year == null || quarter == null) throw new ArgumentException("Year and quarter are required for quarterly filter.");
                    int startMonth = (quarter.Value - 1) * 3 + 1;
                    rangeStart = new DateTime(year.Value, startMonth, 1);
                    rangeEnd = rangeStart.AddMonths(3).AddDays(-1);
                    break;

                case "yearly":
                    if (year == null) throw new ArgumentException("Year is required for yearly filter.");
                    rangeStart = new DateTime(year.Value, 1, 1);
                    rangeEnd = new DateTime(year.Value, 12, 31);
                    break;

                case "custom":
                    if (startDate == null || endDate == null) throw new ArgumentException("Start and end dates are required for custom filter.");
                    rangeStart = startDate.Value;
                    rangeEnd = endDate.Value;
                    break;

                default:
                    throw new ArgumentException("Invalid filter type. Use monthly, quarterly, yearly, or custom.");
            }

            // ✅ Loans disbursed in the period
            var disbursedLoans = await _context.Loans
                .Where(l => (l.Status == LoanStatus.Approved || l.Status == LoanStatus.Completed)
                         && l.StartDate >= rangeStart && l.StartDate <= rangeEnd)
                .ToListAsync();

            decimal totalDisbursement = disbursedLoans.Sum(l => l.Principal);
            decimal totalExpectedInterest = disbursedLoans.Sum(l => l.TotalInterest);
            decimal totalRepayment = disbursedLoans.Sum(l => l.TotalRepayment); // obligation, not just paid

            // ✅ Profit earned = only paid interest portions
            var paidRepayments = await _context.RepaymentSchedules
                .Where(r => r.Status == RepaymentStatus.Paid
                         && r.PaymentDate >= rangeStart && r.PaymentDate <= rangeEnd)
                .ToListAsync();
            decimal profitEarned = paidRepayments.Sum(r => r.InterestPortion);

            // ✅ Defaulted loans (loans marked as Defaulted in this period)
            var defaultedLoans = await _context.Loans
                .Include(l => l.RepaymentSchedules)
                .Where(l => l.Status == LoanStatus.Defaulted
                         && l.StartDate >= rangeStart && l.StartDate <= rangeEnd)
                .ToListAsync();

            int defaultedCount = defaultedLoans.Count;
            decimal totalDefaultedAmount = defaultedLoans
                .SelectMany(l => l.RepaymentSchedules)
                .Where(r => r.Status == RepaymentStatus.Pending)
                .Sum(r => r.TotalAmount);

            return new
            {
                Period = $"{rangeStart:yyyy-MM-dd} to {rangeEnd:yyyy-MM-dd}",
                TotalDisbursement = totalDisbursement,
                TotalRepayment = totalRepayment,
                TotalExpectedInterest = totalExpectedInterest,
                DefaultedLoans = defaultedCount,
                TotalDefaultedAmount = totalDefaultedAmount,
                ProfitEarned = profitEarned
            };
        }
    }
}
