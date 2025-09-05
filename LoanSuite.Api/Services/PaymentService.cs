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

                installment.Status = RepaymentStatus.Paid;
                installment.PaymentDate = DateTime.UtcNow;
            }

            try
            {
                await _context.SaveChangesAsync();
                return "Payment initiated successfully and installments marked as paid.";
            }
            catch (DbUpdateException ex)
            {
                return $"An error occurred while saving the payment. Details: {ex.InnerException?.Message ?? ex.Message}";
            }
        }

        public async Task<Payment?> ApprovePaymentAsync(int paymentId, string reviewedBy)
        {
            var payment = await _context.Payments
                .Include(p => p.RepaymentSchedule)
                .FirstOrDefaultAsync(p => p.Id == paymentId && p.Status == PaymentStatus.Pending);

            if (payment == null) return null;

            payment.Status = PaymentStatus.Approved;
            payment.ReviewedBy = reviewedBy;
            payment.ReviewedAt = DateTime.UtcNow;

            if (payment.RepaymentSchedule != null)
            {
                payment.RepaymentSchedule.Status = RepaymentStatus.Paid;
                payment.RepaymentSchedule.PaymentDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return payment;
        }

        public async Task<Payment?> RejectPaymentAsync(int paymentId, string reviewedBy)
        {
            var payment = await _context.Payments.FindAsync(paymentId);
            if (payment == null || payment.Status != PaymentStatus.Pending) return null;

            payment.Status = PaymentStatus.Rejected;
            payment.ReviewedBy = reviewedBy;
            payment.ReviewedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return payment;
        }

        // ==================== GET PAYMENTS WITH PAGINATION ====================
        public async Task<(IEnumerable<Payment> Payments, int TotalCount)> GetPaymentsAsync(
            string? status, int page, int pageSize)
        {
            var query = _context.Payments.AsQueryable();

            if (!string.IsNullOrEmpty(status) &&
                Enum.TryParse<PaymentStatus>(status, true, out var parsedStatus))
            {
                query = query.Where(p => p.Status == parsedStatus);
            }

            var totalCount = await query.CountAsync();

            var payments = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (payments, totalCount);
        }
    }
}
