using LoanSuite.Core.Entities;
using LoanSuite.Infrastructure.Data;
using LoanSuite.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LoanSuite.Api.Services
{
    public class CustomerService
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public CustomerService(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // ==================== CREATE CUSTOMER ====================
        public async Task<Customer> CreateCustomerAsync(CreateCustomerRequest request)
        {
            // Prevent duplicate emails
            if (await _context.Customers.AnyAsync(c => c.Email == request.Email))
                throw new InvalidOperationException("Email already exists.");

            string idPhotoPath = string.Empty;
            string passportPhotoPath = string.Empty;

            if (request.IdentificationDocument != null)
                idPhotoPath = await SaveFileAsync(request.IdentificationDocument);

            if (request.PassportPhoto != null)
                passportPhotoPath = await SaveFileAsync(request.PassportPhoto);

            var customer = new Customer
            {
                FullName = request.FullName,
                DateOfBirth = request.DateOfBirth,
                Gender = request.Gender,
                MaritalStatus = request.MaritalStatus,
                ResidentialAddress = request.ResidentialAddress,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                EmploymentStatus = request.EmploymentStatus,
                MonthlyIncome = request.MonthlyIncome,
                IdType = request.IdType,
                IdNumber = request.IdNumber,
                IdPhotoUrl = idPhotoPath,
                PassportPhotoUrl = passportPhotoPath,
                NIN = request.NIN,
                BVN = request.BVN,
                GuarantorFullName = request.Guarantor.FullName,
                GuarantorRelationship = request.Guarantor.RelationshipToBorrower,
                GuarantorAddress = request.Guarantor.ResidentialAddress,
                GuarantorPhone = request.Guarantor.PhoneNumber,
                GuarantorEmail = request.Guarantor.Email
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            return customer;
        }

        // ==================== UPDATE CUSTOMER (JSON-ONLY) ====================
        public async Task<Customer?> UpdateCustomerAsync(int customerId, UpdateCustomerRequest request)
        {
            var existingCustomer = await _context.Customers.FindAsync(customerId);
            if (existingCustomer == null) return null;

            // Prevent email duplicates
            if (!string.IsNullOrEmpty(request.Email) &&
                await _context.Customers.AnyAsync(c => c.Email == request.Email && c.Id != customerId))
            {
                throw new InvalidOperationException("Email already exists.");
            }

            // Update only provided fields
            if (!string.IsNullOrEmpty(request.FullName))
                existingCustomer.FullName = request.FullName;

            if (request.DateOfBirth.HasValue)
                existingCustomer.DateOfBirth = request.DateOfBirth.Value;

            if (!string.IsNullOrEmpty(request.Gender))
                existingCustomer.Gender = request.Gender;

            if (!string.IsNullOrEmpty(request.MaritalStatus))
                existingCustomer.MaritalStatus = request.MaritalStatus;

            if (!string.IsNullOrEmpty(request.ResidentialAddress))
                existingCustomer.ResidentialAddress = request.ResidentialAddress;

            if (!string.IsNullOrEmpty(request.Email))
                existingCustomer.Email = request.Email;

            if (!string.IsNullOrEmpty(request.PhoneNumber))
                existingCustomer.PhoneNumber = request.PhoneNumber;

            if (!string.IsNullOrEmpty(request.EmploymentStatus))
                existingCustomer.EmploymentStatus = request.EmploymentStatus;

            if (request.MonthlyIncome.HasValue)
                existingCustomer.MonthlyIncome = request.MonthlyIncome.Value;

            if (!string.IsNullOrEmpty(request.IdType))
                existingCustomer.IdType = request.IdType;

            if (!string.IsNullOrEmpty(request.IdNumber))
                existingCustomer.IdNumber = request.IdNumber;

            if (!string.IsNullOrEmpty(request.NIN))
                existingCustomer.NIN = request.NIN;

            if (!string.IsNullOrEmpty(request.BVN))
                existingCustomer.BVN = request.BVN;

            // Update guarantor info if provided
            if (request.Guarantor != null)
            {
                if (!string.IsNullOrEmpty(request.Guarantor.FullName))
                    existingCustomer.GuarantorFullName = request.Guarantor.FullName;

                if (!string.IsNullOrEmpty(request.Guarantor.RelationshipToBorrower))
                    existingCustomer.GuarantorRelationship = request.Guarantor.RelationshipToBorrower;

                if (!string.IsNullOrEmpty(request.Guarantor.ResidentialAddress))
                    existingCustomer.GuarantorAddress = request.Guarantor.ResidentialAddress;

                if (!string.IsNullOrEmpty(request.Guarantor.PhoneNumber))
                    existingCustomer.GuarantorPhone = request.Guarantor.PhoneNumber;

                if (!string.IsNullOrEmpty(request.Guarantor.Email))
                    existingCustomer.GuarantorEmail = request.Guarantor.Email;
            }

            await _context.SaveChangesAsync();
            return existingCustomer;
        }

        // ==================== GET CUSTOMER BY ID ====================
        public async Task<Customer?> GetCustomerByIdAsync(int customerId)
        {
            return await _context.Customers.FirstOrDefaultAsync(c => c.Id == customerId);
        }

        // ==================== GET ALL CUSTOMERS ====================
        public async Task<List<Customer>> GetAllCustomersAsync()
        {
            return await _context.Customers.ToListAsync();
        }

        // ==================== GET CUSTOMERS WITH PAGINATION & SEARCH ====================
        public async Task<(List<Customer> Customers, int TotalCount)> GetCustomersPagedAsync(
            int pageNumber, int pageSize, string? searchTerm = null)
        {
            var query = _context.Customers.AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
                query = query.Where(c => c.FullName.Contains(searchTerm) || c.Id.ToString() == searchTerm);

            int totalCount = await query.CountAsync();

            var customers = await query
                .OrderBy(c => c.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (customers, totalCount);
        }

        // ==================== DELETE CUSTOMER ====================
        public async Task<bool> DeleteCustomerAsync(int customerId)
        {
            var customer = await _context.Customers.FindAsync(customerId);
            if (customer == null) return false;

            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();
            return true;
        }

        // ==================== PRIVATE FILE SAVE METHOD (USED ONLY FOR CREATE) ====================
        private async Task<string> SaveFileAsync(Microsoft.AspNetCore.Http.IFormFile file)
        {
            if (file == null || file.Length == 0)
                return string.Empty;

            var webRoot = _env.WebRootPath ?? Path.Combine(AppContext.BaseDirectory, "wwwroot");

            var uploadsFolder = Path.Combine(webRoot, "uploads", "customers");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            return Path.Combine("uploads", "customers", uniqueFileName).Replace("\\", "/");
        }
    }
}
