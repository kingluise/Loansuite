using LoanSuite.Api.Models;
using LoanSuite.Api.Services;
using LoanSuite.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LoanSuite.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LoanController : ControllerBase
    {
        private readonly LoanService _loanService;

        public LoanController(LoanService loanService)
        {
            _loanService = loanService;
        }

        // ==================== APPLY LOAN ====================
        [Authorize(Roles = "Admin,Operator")]
        [HttpPost("apply")]
        public async Task<IActionResult> ApplyLoan([FromBody] CreateLoanRequest request)
        {
            var createdByString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(createdByString))
            {
                return Unauthorized("User ID claim not found in token.");
            }

            var createdBy = createdByString;

            var loan = await _loanService.ApplyLoanAsync(request, createdBy);

            var loanDto = new LoanDto
            {
                Id = loan.Id,
                CustomerId = loan.CustomerId,
                CustomerName = loan.Customer?.FullName,
                Principal = loan.Principal,
                InterestRate = loan.InterestRate,
                Status = loan.Status.ToString(),
                CreatedAt = loan.CreatedAt
            };

            return Ok(loanDto);
        }

        // ==================== GET LOAN BY ID (REPAYMENT SCHEDULE) ====================
        // This endpoint remains unchanged to preserve its original function.
        [Authorize(Roles = "Admin,Operator")]
        [HttpGet("{loanId}")]
        public async Task<IActionResult> GetLoanById(int loanId)
        {
            var loan = await _loanService.GetLoanByIdAsync(loanId);
            if (loan == null) return NotFound(new { message = "Loan not found" });

            return Ok(new
            {
                LoanId = loan.Id,
                LoanAmount = loan.Principal,
                LoanType = loan.TermType.ToString(),
                CustomerFullName = loan.Customer?.FullName,
                RepaymentSchedules = loan.RepaymentSchedules
                    .OrderBy(s => s.InstallmentNo)
                    .Select(s => new
                    {
                        s.Id,
                        s.InstallmentNo,
                        s.DueDate,
                        s.TotalAmount,
                        s.Status
                    })
            });
        }

        // ==================== NEW: GET LOAN DETAILS ====================
        // This new endpoint returns all the required loan properties for the "View" modal.
        [Authorize(Roles = "Admin,Operator")]
        [HttpGet("details/{loanId}")]
        public async Task<IActionResult> GetLoanDetails(int loanId)
        {
            var loan = await _loanService.GetLoanByIdAsync(loanId);
            if (loan == null) return NotFound(new { message = "Loan not found" });

            // Returning the full loan object with the correct property names
            return Ok(new
            {
                loan.CustomerId,
                loan.Principal,
                loan.InterestRate,
                TermType = loan.TermType.ToString(),
                DurationValue = loan.DurationValue,
                loan.StartDate,
                loan.EndDate,
                loan.FirstInstallmentDate,
                loan.CreatedAt,
                loan.ReviewedAt,
                loan.TotalInterest,
                loan.TotalRepayment,
                loan.InstallmentAmount,
            });
        }

        // ==================== GET LOANS LIST (simplified) ====================
        [Authorize(Roles = "Admin,Operator")]
        [HttpGet("list")]
        public async Task<IActionResult> GetLoansPaged(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? status = null)
        {
            var (loans, totalCount) = await _loanService.GetLoansPagedAsync(pageNumber, pageSize, status);

            var result = loans.Select(loan => new LoanDto
            {
                Id = loan.Id,
                CustomerId = loan.CustomerId,
                CustomerName = loan.Customer?.FullName,
                Principal = loan.Principal,
                InterestRate = loan.InterestRate,
                Status = loan.Status.ToString(),
                CreatedAt = loan.CreatedAt
            }).ToList();

            return Ok(new { loans = result, totalCount, pageNumber, pageSize });
        }

        // ==================== APPROVE LOAN ====================
        [Authorize(Roles = "Admin")]
        [HttpPost("approve/{loanId}")]
        public async Task<IActionResult> ApproveLoan(int loanId)
        {
            var adminIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(adminIdString))
            {
                return Unauthorized("User ID claim not found in token.");
            }

            var adminId = adminIdString;

            var loan = await _loanService.ApproveLoanAsync(loanId, adminId);
            if (loan == null) return NotFound(new { message = "Loan not found or cannot be approved" });

            var loanDto = new LoanDto
            {
                Id = loan.Id,
                CustomerId = loan.CustomerId,
                CustomerName = loan.Customer?.FullName,
                Principal = loan.Principal,
                InterestRate = loan.InterestRate,
                Status = loan.Status.ToString(),
                CreatedAt = loan.CreatedAt
            };

            return Ok(loanDto);
        }

        // ==================== REJECT LOAN ====================
        [Authorize(Roles = "Admin")]
        [HttpPost("reject/{loanId}")]
        public async Task<IActionResult> RejectLoan(int loanId)
        {
            var adminIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(adminIdString))
            {
                return Unauthorized("User ID claim not found in token.");
            }

            var adminId = adminIdString;

            var loan = await _loanService.RejectLoanAsync(loanId, adminId);
            if (loan == null) return NotFound(new { message = "Loan not found or cannot be rejected" });

            var loanDto = new LoanDto
            {
                Id = loan.Id,
                CustomerId = loan.CustomerId,
                CustomerName = loan.Customer?.FullName,
                Principal = loan.Principal,
                InterestRate = loan.InterestRate,
                Status = loan.Status.ToString(),
                CreatedAt = loan.CreatedAt
            };

            return Ok(loanDto);
        }

        // ==================== COMPLETE LOAN ====================
        [Authorize(Roles = "Admin")]
        [HttpPost("complete/{loanId}")]
        public async Task<IActionResult> CompleteLoan(int loanId)
        {
            var adminIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(adminIdString))
            {
                return Unauthorized("User ID claim not found in token.");
            }

            var adminId = adminIdString;

            var loan = await _loanService.CompleteLoanAsync(loanId, adminId);
            if (loan == null) return NotFound(new { message = "Loan not found or cannot be completed" });

            return Ok(new
            {
                loan.Id,
                loan.CustomerId,
                CustomerName = loan.Customer?.FullName,
                loan.Principal,
                loan.InterestRate,
                Status = loan.Status.ToString(),
                loan.CreatedAt
            });
        }

        // ==================== DEFAULT LOAN ====================
        [Authorize(Roles = "Admin")]
        [HttpPost("default/{loanId}")]
        public async Task<IActionResult> DefaultLoan(int loanId)
        {
            var adminIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(adminIdString))
            {
                return Unauthorized("User ID claim not found in token.");
            }

            var adminId = adminIdString;

            var loan = await _loanService.DefaultLoanAsync(loanId, adminId);
            if (loan == null) return NotFound(new { message = "Loan not found or cannot be defaulted" });

            return Ok(new
            {
                loan.Id,
                loan.CustomerId,
                CustomerName = loan.Customer?.FullName,
                loan.Principal,
                loan.InterestRate,
                Status = loan.Status.ToString(),
                loan.CreatedAt
            });
        }


        [Authorize(Roles = "Admin,Operator")]
        [HttpGet("due-repayments")]
        public async Task<IActionResult> GetDueRepayments(
     [FromQuery] DateTime startDate,
     [FromQuery] DateTime endDate)
        {
            // Adjust endDate to include the whole day
            endDate = endDate.Date.AddDays(1).AddTicks(-1);

            var repayments = await _loanService.GetDueRepaymentsAsync(startDate, endDate);

            var result = repayments.Select(r => new
            {
                r.Id,
                r.LoanId,
                CustomerName = r.Loan.Customer.FullName,
                r.DueDate,
                r.TotalAmount,
                r.Status
            });

            return Ok(result);
        }


        // ==================== PROFIT ANALYTICS ====================
        [Authorize(Roles = "Admin,Operator")]
        [HttpGet("profit-analytics")]
        public async Task<IActionResult> GetProfitAnalytics(
            [FromQuery] string filterType, // monthly, quarterly, yearly, custom
            [FromQuery] int? year,
            [FromQuery] int? month,
            [FromQuery] int? quarter,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            var analytics = await _loanService.GetProfitAnalyticsAsync(
                filterType, year, month, quarter, startDate, endDate);

            return Ok(analytics);
        }

        // ==================== DETAILED PROFIT ANALYTICS ====================
        [Authorize(Roles = "Admin,Operator")]
        [HttpGet("profit-analytics/detailed")]
        public async Task<IActionResult> GetDetailedProfitAnalytics(
            [FromQuery] string filterType, // monthly, quarterly, yearly, custom
            [FromQuery] int? year,
            [FromQuery] int? month,
            [FromQuery] int? quarter,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            var analytics = await _loanService.GetDetailedProfitAnalyticsAsync(
                filterType, year, month, quarter, startDate, endDate);

            return Ok(analytics);
        }

    }
}