namespace LoanSuite.Api.Models
{
    public class LoanDto
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public decimal Principal { get; set; }
        public double InterestRate { get; set; }
        public string? Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
