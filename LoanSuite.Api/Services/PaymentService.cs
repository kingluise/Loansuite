using LoanSuite.Api.Models.Payments;
using LoanSuite.Core.Entities;
using LoanSuite.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LoanSuite.Api.Services
{
    public class PaymentService
    {
        private readonly AppDbContext _context;

        public PaymentService(AppDbContext context)
        {
            _context = context;
        }

        // ==================== INITIATE PAYMENT ====================
        public async Task<string> InitiatePaymentAsync(InitiatePaymentRequest request, string loggedBy)
        {
            var loan = await _context.Loans
                .Include(l => l.RepaymentSchedules)
                .FirstOrDefaultAsync(l => l.Id == request.LoanId);

            if (loan == null)
            {
                return "Loan not found.";
            }

            var installmentsToPay = loan.RepaymentSchedules
                .Where(s => request.InstallmentIds.Contains(s.Id) && s.Status == RepaymentStatus.Pending)
                .ToList();

            if (!installmentsToPay.Any())
            {
                return "No valid, pending installments found for the given IDs.";
            }

            var totalDue = installmentsToPay.Sum(s => s.TotalAmount);
            if (request.AmountPaid < totalDue)
            {
                return "Insufficient payment. Please pay the full amount due for the selected installments.";
            }

            foreach (var installment in installmentsToPay)
            {
                var payment = new Payment
                {
                    LoanId = request.LoanId,
                    RepaymentScheduleId = installment.Id,
                    Amount = installment.TotalAmount,
                    LoggedBy = loggedBy,
                    Status = PaymentStatus.Pending,
                    Reference = $"PAY-{Guid.NewGuid().ToString().Substring(0, 8)}"
                };

                _context.Payments.Add(payment);

                // ❌ Remove schedule.Paid update here
                // installment.Status = RepaymentStatus.Paid;
                // installment.PaymentDate = DateTime.UtcNow;
            }

            try
            {
                await _context.SaveChangesAsync();
                return "Payment initiated successfully. Awaiting approval.";
            }
            catch (DbUpdateException ex)
            {
                return $"An error occurred while saving the payment. Details: {ex.InnerException?.Message ?? ex.Message}";
            }
        }

        // ==================== APPROVE PAYMENT ====================
        public async Task<Payment?> ApprovePaymentAsync(int paymentId, string reviewedBy)
        {
            var payment = await _context.Payments
                .Include(p => p.RepaymentSchedule)
                .FirstOrDefaultAsync(p => p.Id == paymentId && p.Status == PaymentStatus.Pending);

            if (payment == null) return null;

            payment.Status = PaymentStatus.Approved;
            payment.ReviewedBy = reviewedBy;
            payment.ReviewedAt = DateTime.UtcNow;

            // ✅ Only mark schedule as paid now
            if (payment.RepaymentSchedule != null)
            {
                payment.RepaymentSchedule.Status = RepaymentStatus.Paid;
                payment.RepaymentSchedule.PaymentDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return payment;
        }

        // ==================== REJECT PAYMENT ====================
        public async Task<Payment?> RejectPaymentAsync(int paymentId, string reviewedBy)
        {
            var payment = await _context.Payments
                .Include(p => p.RepaymentSchedule)
                .FirstOrDefaultAsync(p => p.Id == paymentId && p.Status == PaymentStatus.Pending);

            if (payment == null) return null;

            payment.Status = PaymentStatus.Rejected;
            payment.ReviewedBy = reviewedBy;
            payment.ReviewedAt = DateTime.UtcNow;

            // ✅ Repayment schedule stays Pending since payment is rejected
            if (payment.RepaymentSchedule != null)
            {
                payment.RepaymentSchedule.Status = RepaymentStatus.Pending;
                payment.RepaymentSchedule.PaymentDate = null;
            }

            await _context.SaveChangesAsync();
            return payment;
        }

        // ==================== GET PAYMENTS WITH PAGINATION ====================
        public async Task<(List<PaymentDto>, int)> GetPaymentsAsync(string? status, int page, int pageSize)
        {
            var query = _context.Payments
                .Include(p => p.Loan)
                .ThenInclude(l => l.Customer)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                if (Enum.TryParse<PaymentStatus>(status, true, out var parsedStatus))
                {
                    query = query.Where(p => p.Status == parsedStatus);
                }
            }

            var totalCount = await query.CountAsync();

            var payments = await query
                .OrderByDescending(p => p.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new PaymentDto
                {
                    Id = p.Id,
                    LoanId = p.LoanId,
                    RepaymentScheduleId = p.RepaymentScheduleId,
                    Amount = p.Amount,
                    Status = p.Status.ToString(),
                    ReviewedAt = p.ReviewedAt,
                    ReviewedBy = p.ReviewedBy,
                    FullName = p.Loan.Customer.FullName
                })
                .ToListAsync();

            return (payments, totalCount);
        }

    }
}
