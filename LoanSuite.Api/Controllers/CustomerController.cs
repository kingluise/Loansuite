using LoanSuite.Api.Models;
using LoanSuite.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoanSuite.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CustomerController : ControllerBase
    {
        private readonly CustomerService _customerService;

        public CustomerController(CustomerService customerService)
        {
            _customerService = customerService;
        }

        // ==================== CREATE CUSTOMER ====================
        [Authorize(Roles = "Admin,Operator")]
        [HttpPost("create")]
        public async Task<IActionResult> CreateCustomer([FromForm] CreateCustomerRequest request)
        {
            var customer = await _customerService.CreateCustomerAsync(request);
            return Ok(customer);
        }

        // ==================== GET CUSTOMER BY ID ====================
        [Authorize(Roles = "Admin,Operator")]
        [HttpGet("{customerId}")]
        public async Task<IActionResult> GetCustomerById(int customerId)
        {
            var customer = await _customerService.GetCustomerByIdAsync(customerId);
            if (customer == null)
                return NotFound(new { message = "Customer not found" });

            return Ok(customer);
        }

        // ==================== GET CUSTOMERS WITH PAGINATION & SEARCH ====================
        [Authorize(Roles = "Admin,Operator")]
        [HttpGet("list")]
        public async Task<IActionResult> GetCustomersPaged(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null)
        {
            var (customers, totalCount) = await _customerService.GetCustomersPagedAsync(pageNumber, pageSize, searchTerm);
            return Ok(new { customers, totalCount, pageNumber, pageSize });
        }

        // ==================== UPDATE CUSTOMER ====================
        [Authorize(Roles = "Admin")] // Restrict update to Admin
        [HttpPut("update/{customerId}")]
        public async Task<IActionResult> UpdateCustomer(int customerId, [FromBody] UpdateCustomerRequest request)
        {
            if (customerId != request.Id)
                return BadRequest(new { message = "Customer ID mismatch" });

            var updatedCustomer = await _customerService.UpdateCustomerAsync(customerId, request);
            if (updatedCustomer == null)
                return NotFound(new { message = "Customer not found" });

            return Ok(updatedCustomer);
        }

        // ==================== DELETE CUSTOMER ====================
        [Authorize(Roles = "Admin")]
        [HttpDelete("delete/{customerId}")]
        public async Task<IActionResult> DeleteCustomer(int customerId)
        {
            var deleted = await _customerService.DeleteCustomerAsync(customerId);
            if (!deleted)
                return NotFound(new { message = "Customer not found" });

            return Ok(new { message = "Customer deleted successfully" });
        }
    }
}
