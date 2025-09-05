using LoanSuite.Api.Models.Payments;
using LoanSuite.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LoanSuite.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly PaymentService _paymentService;

        public PaymentController(PaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        // ==================== INITIATE PAYMENT ====================
        [Authorize(Roles = "Admin,Operator")]
        [HttpPost("initiate")]
        public async Task<IActionResult> InitiatePayment([FromBody] InitiatePaymentRequest request)
        {
            var loggedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(loggedBy))
            {
                return Unauthorized("User ID claim not found in token.");
            }

            var result = await _paymentService.InitiatePaymentAsync(request, loggedBy);

            if (result.StartsWith("Payment initiated"))
            {
                return Ok(new { message = result });
            }
            else
            {
                return BadRequest(new { message = result });
            }
        }

        // ==================== APPROVE PAYMENT ====================
        [Authorize(Roles = "Admin")]
        [HttpPost("approve/{paymentId}")]
        public async Task<IActionResult> ApprovePayment(int paymentId)
        {
            var reviewedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(reviewedBy))
            {
                return Unauthorized("User ID claim not found in token.");
            }

            var payment = await _paymentService.ApprovePaymentAsync(paymentId, reviewedBy);
            if (payment == null)
            {
                return NotFound(new { message = "Payment not found or cannot be approved." });
            }

            return Ok(new
            {
                payment.Id,
                payment.LoanId,
                payment.RepaymentScheduleId,
                payment.Amount,
                Status = payment.Status.ToString(),
                payment.ReviewedAt,
                payment.ReviewedBy
            });
        }

        // ==================== REJECT PAYMENT ====================
        [Authorize(Roles = "Admin")]
        [HttpPost("reject/{paymentId}")]
        public async Task<IActionResult> RejectPayment(int paymentId)
        {
            var reviewedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(reviewedBy))
            {
                return Unauthorized("User ID claim not found in token.");
            }
            var payment = await _paymentService.RejectPaymentAsync(paymentId, reviewedBy);
            if (payment == null)
            {
                return NotFound(new { message = "Payment not found or cannot be rejected." });
            }

            return Ok(new
            {
                payment.Id,
                payment.LoanId,
                payment.RepaymentScheduleId,
                payment.Amount,
                Status = payment.Status.ToString(),
                payment.ReviewedAt,
                payment.ReviewedBy
            });
        }

        // ==================== GET PAYMENTS (WITH PAGINATION) ====================
        [Authorize(Roles = "Admin,Operator")]
        [HttpGet("all")]
        public async Task<IActionResult> GetPayments(
            [FromQuery] string? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var (payments, totalCount) = await _paymentService.GetPaymentsAsync(status, page, pageSize);

            return Ok(new
            {
                totalCount,
                page,
                pageSize,
                data = payments.Select(p => new
                {
                    p.Id,
                    p.LoanId,
                    p.RepaymentScheduleId,
                    p.Amount,
                    Status = p.Status.ToString(),
                    p.ReviewedAt,
                    p.ReviewedBy
                })
            });
        }
    }
}
